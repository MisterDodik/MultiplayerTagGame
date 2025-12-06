using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using UnityEditor;
using UnityEngine;


[System.Serializable]
public class Client
{
    public float width{get; private set;}
    public float height{get; private set;}
    public float posX { get; set; }
    public float posY { get; set; }

    public float speed { get; private set; }

    public Game Game { get; private set; }
    public DummyClient dummyClient { get; private set; }

    public Client(Game _game, DummyClient _dummyClient, float _width = .3f,  float _height = .3f, float _speed = .03f)
    {
        width = _width;
        height = _height;
        speed = _speed;
        Game = _game;
        dummyClient = _dummyClient;
    }

    public void SetPosition(float _posX, float _posY)
    {
        posX = _posX; 
        posY = _posY;
    }
}

[System.Serializable]
public class GridData
{
    public float centerX;
    public float centerY;
    public bool hasObstacle;
}

[System.Serializable]
public class Game
{
    //public Dictionary<GameObject, bool> obstacles { get; private set; }
    public Dictionary<Client, bool> players { get; private set; }

    public GridData[,] grid { get; private set; }
    public int gridRows { get; private set; }
    public int gridCols { get; private set; }
    public float gridCellSize { get; private set; }
    private float gridOriginX;
    private float gridOriginY;
    public Game(int rows = 30, int cols = 60, float cellSize = .5f, float originX = -15f, float originY = -7.5f)
    {
       // obstacles = new();
        players = new();

        gridRows = rows;
        gridCols = cols;
        gridCellSize = cellSize;
        gridOriginX = originX;
        gridOriginY = originY;
    }
    public void initGrid()
    {
        //grid[i][j] = 1        -obstacle na i j
        //grid[i][j] = 0        -slobodno na i j
        grid = new GridData[gridCols, gridRows];
        for (int i = 0; i < gridCols; i++)
        {
            for (int j = 0; j < gridRows; j++)
            {
                bool isEdge = false;
                if (i == gridCols - 1 || i == 0 || j == 0 || j == gridRows - 1)
                    isEdge = true;
                grid[i, j] = new GridData()
                {
                    centerX = gridOriginX + i * gridCellSize,
                    centerY = gridOriginY + j * gridCellSize,
                    hasObstacle = isEdge
                };
            }
        }
    }

    public void initObstacles(int obstacleCoverage = 20)
    {
        HashSet<(int, int)> used = new HashSet<(int, int)>();

        while (used.Count < gridCols * gridRows * obstacleCoverage / 100)
        {
            int x = Random.Range(0, gridCols);
            int y = Random.Range(0, gridRows);

            used.Add((x, y));
        }

        foreach (var (x, y) in used)
        {
            grid[x, y].hasObstacle = true;
        }
   
        for (int i = 0; i < gridCols; i++)
        {
            for (int j = 0; j < gridRows; j++)
            {
                if (!grid[i, j].hasObstacle)
                    continue;
                EventSystem.Emit(MessageType.SpawnObstacle, new Obstacle
                                                            {
                                                                cellSize = gridCellSize,
                                                                posX = grid[i, j].centerX,
                                                                posY = grid[i, j].centerY
                                                            });
            }
        }
    }
}
public class DummyServer : MonoBehaviour
{
    Game game = new Game();
    
    public Client NewClient(DummyClient dc)
    {
        Client c = new Client(_game: game, _dummyClient: dc);
        game.players[c] = true;
        return c;       //ovo stavi tamo da player postane ovaj client, pa kasnije da salje sebe bilo gdje
    }

    private void Start()
    {
        StartGame(game);
    }
    public void StartGame(Game game)
    {
        game.initGrid();
        game.initObstacles();
        
        foreach(Client client in game.players.Keys)
        {

            do
            {
                int i = Random.Range(0, game.gridCols);
                int j = Random.Range(0, game.gridRows);

                if (!game.grid[i, j].hasObstacle)
                {
                    client.SetPosition(game.grid[i,j].centerX, game.grid[i,j].centerY);
                    sendPosUpdate(client, client.dummyClient);
                    break;
                }

            } while (true);
        }
    }


    public void ReceiveInput(Client c, float inputX, float inputY, DummyClient dc)
    {
        if (Mathf.Abs(inputX) > 0 && Mathf.Abs(inputX) > 0)
        {
            inputX /= Mathf.Sqrt(2);
            inputY /= Mathf.Sqrt(2);
        }
        float tempX = c.posX + inputX * c.speed;
        float tempY = c.posY + inputY * c.speed;

        if (!CheckCollision(c, tempX, tempY))
        {
            c.posX = tempX;
            c.posY = tempY;
            sendPosUpdate(c, dc);
            return;
        }

        tempX = c.posX;
        tempY = c.posY + inputY * c.speed;

        if (!CheckCollision(c, tempX, tempY))
        {
            c.posX = tempX;
            c.posY = tempY;
            sendPosUpdate(c, dc);
            return;
        }

        tempX = c.posX + inputX * c.speed;
        tempY = c.posY;

        if (!CheckCollision(c, tempX, tempY))
        {
            c.posX = tempX;
            c.posY = tempY;
            sendPosUpdate(c, dc);
            return;
        }

    }
   private void sendPosUpdate(Client c, DummyClient dc) {
        //TODO broadcast new client pos to everyone, ali returnace se prije vjv pa to rijesi
        dc.UpdatePositionsHandler(new Vector2(c.posX, c.posY));
   }

    private bool CheckCollision(Client client, float potentialX, float potentialY)
    {
        foreach(GridData item in client.Game.grid)
        {
            if (!item.hasObstacle)
                continue;

            bool overlapX = partialAABB(item.centerX, client.Game.gridCellSize / 2, potentialX, client.width / 2);
            bool overlapY = partialAABB(item.centerY, client.Game.gridCellSize / 2, potentialY, client.height / 2);

            if (overlapX && overlapY)
                return true;
        }

        foreach (Client player in client.Game.players.Keys)
        {
            if (player == client)
                continue;

            if (player.posX + player.width / 2 <= potentialX)
                return true;
            if (player.posY + player.height / 2 <= potentialY)
                return true;

            bool overlapX = partialAABB(player.posX, player.width / 2, potentialX, client.width / 2);
            bool overlapY = partialAABB(player.posY, player.height / 2, potentialY, client.height / 2);

            if (overlapX && overlapY)
                return true;
        }

        return false;
    }
    private bool partialAABB(float ax, float ad, float bx, float bd)
    {
        return ax + ad >= bx - bd &&            //nacrtaj sliku kad ne bude jasno :)
               bx + bd >= ax - ad;
    }
}

