using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

struct InputState
{
    public Vector2 input;
    public Vector2 pos;
}
public class OwnerPlayerInput : PlayerGeneral
{
    public bool GameStarted { get; private set; }

    private PlayerLobby playerManager;

    private Transform mainCamera;

    private float inputX;
    private float inputY;

    //client side prediction
    private const int SERVER_TICK_RATE = 20;
    private int currentTick = 0;
    private float tickTimer = 0;
    private float timeBetweenTicks;

    private const int STATE_BUFFER_SIZE = 1024;
    private InputState[] stateBuffer = new InputState[STATE_BUFFER_SIZE];

    private float playerHeight = 0.3f;
    private float playerWidth = 0.3f;
    private float clientSpeed = 3f;
    private void Awake()
    {
        timeBetweenTicks = 1.0f / SERVER_TICK_RATE;
    }

    public void StartGame(PlayerLobby _playerManager, Transform _camera)
    {
        GameStarted = true;
        playerManager = _playerManager;

        currentTick = 0;
        mainCamera = _camera;
        mainCamera.parent = transform;
        mainCamera.localPosition = new Vector3(0, 0, -10);
    }
    public void EndOwnerGame()
    {
        GameStarted = false;
        if (mainCamera != null)
        {
            mainCamera.parent = null;
            mainCamera.localPosition = new Vector3(0, 0, -10);
        }
    }


    public override void Update()
    {
        base.Update();
        if (!GameStarted)
            return;
       // ClientMovementPrediction(new InputState { input = new Vector2(Input.GetAxisRaw("horizontal"), Input.GetAxisRaw("vertical")) });
        tickTimer += Time.deltaTime;

        if (tickTimer >= timeBetweenTicks)
        {
            tickTimer -= timeBetweenTicks;
            HandleTick();
            currentTick++;
            //Debug.Log($"CLIENT tick {currentTick} pos {targetPosition}");

        }

        if (IsHunter && Input.GetKeyDown(KeyCode.Space))
        {
            EventSystem.Emit(MessageType.SendNetworkMessage, new NetworkMessage
            {
                type = MessageType.HunterAttack,
                payload = (new PositionUpdateClient{})
            });
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            targetPosition = new Vector2(targetPosition.x + 2, targetPosition.y);
        }
    }

    private void HandleTick()
    {
        int bufferIndex = currentTick % STATE_BUFFER_SIZE;
        inputX = Input.GetAxisRaw("horizontal");
        inputY = Input.GetAxisRaw("vertical");

        InputState inputState = new InputState();
        inputState.input = new Vector2(inputX, inputY);

        stateBuffer[bufferIndex] = ClientMovementPrediction(inputState);
        EventSystem.Emit(MessageType.SendNetworkMessage, new NetworkMessage
        {
            type = MessageType.UpdatePositionFromClient,
            payload = (new PositionUpdateClient
            {
                inputX = inputX,
                inputY = inputY
            })
        });
    }
    private InputState ClientMovementPrediction(InputState state)
    {
        if (state.input.sqrMagnitude > 0f)
            state.input.Normalize();

        state.input.x = Mathf.Clamp(state.input.x, -1f, 1f);
        state.input.y = Mathf.Clamp(state.input.y, -1f, 1f);

        Vector2 newPos = state.input * clientSpeed * timeBetweenTicks;
        Vector2 tryPos = targetPosition + newPos;
        if (newPos == Vector2.zero)
        {
            state.pos = targetPosition;
            return state;
        }
        if (!checkCollision(tryPos.x, tryPos.y))
        {
            targetPosition = tryPos;
        }
        else if (!checkCollision(tryPos.x, targetPosition.y))
        {
            targetPosition = new Vector2(tryPos.x, targetPosition.y);
        }
        else if(!checkCollision(targetPosition.x, tryPos.y))
        {
            targetPosition = new Vector2(targetPosition.x, tryPos.y);
        }
        state.pos = targetPosition;
        return state;
    }

    private bool checkCollision(float x, float y)
    {
        float cellSize = playerManager.obstacleManager.obstacleSize;
        foreach (var cell in playerManager.obstacleManager.activeObstacles.Values.ToList())
        {
            bool overlapX =
                cell.transform.localPosition.x + cellSize / 2 >= x - playerWidth /2 && 
                x + playerWidth/2 >= cell.transform.localPosition.x - cellSize / 2;

            bool overlapY =
                cell.transform.localPosition.y + cellSize / 2 >= y - playerHeight/2 &&
                y + playerHeight/2 >= cell.transform.localPosition.y - cellSize / 2;

            if (overlapX && overlapY)
                return true;
        }

        return false;
    }
    private void ServerReconciliation(Vector2 serverPos, int serverTick)
    {
        int bufferTick = serverTick % STATE_BUFFER_SIZE;
        if (Vector2.Distance(stateBuffer[bufferTick].pos, serverPos) < 0.01f)
            return;
       // transform.localPosition = serverPos;
        targetPosition = serverPos;
        InputState correctedState = stateBuffer[bufferTick];
        correctedState.pos = serverPos;
        stateBuffer[bufferTick] = correctedState;

        int tickToProcess = serverTick + 1;
        while (tickToProcess < currentTick)
        {
            stateBuffer[tickToProcess % STATE_BUFFER_SIZE] = ClientMovementPrediction(stateBuffer[tickToProcess % STATE_BUFFER_SIZE]);

            tickToProcess++;
        }
    }
    private void UpdatePositionsHandler(object o)
    {
        var playerInfo = o as PositionUpdateServer;
        foreach (ServerPositions item in playerInfo.serverPositions)
        {
            if (playerManager.players[item.id] != this)
                playerManager.players[item.id].SetTargetPosition(new Vector2(item.x, item.y));
            else          
                ServerReconciliation(new Vector2(item.x, item.y), playerInfo.serverTick);
            
            
        }
    }
    private void UpdateClientColor(object o)
    {
        UpdateColor data = o as UpdateColor;
        Color newColor;

        if (ColorUtility.TryParseHtmlString(data.colorHex, out newColor))
        {
            playerManager.players[data.id].UpdateClientColor(newColor, data.isHunter);
            return;
        }
        print("error parsing updateclientcolor data");
    }
  
    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.UpdateClientColor, UpdateClientColor);
        EventSystem.Subscribe(MessageType.UpdatePositionFromServer, UpdatePositionsHandler);
    }
    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.UpdateClientColor, UpdateClientColor);
        EventSystem.Unsubscribe(MessageType.UpdatePositionFromServer, UpdatePositionsHandler);
    }
}



[System.Serializable]
public class PositionUpdateServer
{
    //public string id;
    //public float x;
    //public float y;
    public int serverTick;
    public List<ServerPositions> serverPositions;
}
public struct ServerPositions
{
    public string id;
    public float x;
    public float y;
}

[System.Serializable]
public class PositionUpdateClient
{
    public float inputX;
    public float inputY;
}


[System.Serializable]
public class UpdateColor
{
    public string id;
    public string colorHex;
    public bool isHunter;
}