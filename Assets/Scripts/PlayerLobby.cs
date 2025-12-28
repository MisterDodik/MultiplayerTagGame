using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Linq;
public class PlayerLobby : MonoBehaviour
{
    [SerializeField] private Transform lobbyPlayerParent;
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private Transform gameElements;

    private TextMeshPro playerNameTMPro;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector2 positionOrigin = new Vector2(0, 2);
    [HideInInspector] public Dictionary<string, PlayerGeneral> players = new();
    private Stack<GameObject> pooledPlayers = new();
    private OwnerPlayerInput realPlayer;

    [SerializeField] private Color defaultUsernameFontColor;
    [SerializeField] private Color ownerPlayerFontColor;

    [SerializeField] private ScoreManager scoreManager;
    public ObstacleManager obstacleManager;

    private int playerCount = 0;
    private void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject playerOB = Instantiate(playerPrefab, lobbyPlayerParent);
            pooledPlayers.Push(playerOB);
            playerOB.SetActive(false);
        }
    }

    [SerializeField] private Transform mainCamera;
    private void NewInLobby(object playerInfo)
    {
        LobbyPlayer data = playerInfo as LobbyPlayer;
        if (data == null)
            return;
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            PlayerGeneral player;
            if (!players.TryGetValue(data.id, out player))
            {
                GameObject playerOB;
                if (pooledPlayers.Count > 0)
                {
                    playerOB = pooledPlayers.Pop();
                    playerOB.SetActive(true);
                }
                else
                {
                    playerOB = Instantiate(playerPrefab, lobbyPlayerParent);
                }
                playerNameTMPro = playerOB.GetComponentInChildren<TextMeshPro>();
                playerNameTMPro.text = data.username;

                if (playerCount == 0)
                {
                    realPlayer = playerOB.AddComponent<OwnerPlayerInput>();
                    playerNameTMPro.color = ownerPlayerFontColor;
                }
                else
                {
                    playerOB.AddComponent<EnemyPlayer>();
                    playerNameTMPro.color = defaultUsernameFontColor;
                }

                player = playerOB.GetComponent<PlayerGeneral>();
                player.InitPlayer(data.username, data.id, playerCount > 1, this, data.colorHex);
                
                playerCount++;
                players[data.id] = player;
            }
            UpdateLobby(data.gameStarted);
        });
    }
    private void UpdateLobby(bool clientInGame)
    {
        if (clientInGame)
        {
            return;
        }
        int index = 0;
        foreach(var p in players.Values)
        {
            p.transform.localScale = new Vector3(0.75f, 1f, 1);
            p.GetComponentInChildren<TextMeshPro>().rectTransform.localScale = new Vector3(1, 1, 1);
            p.transform.localPosition = positionOrigin + new Vector2(index % 4 * 2.5f, -index / 4 * 2);
            index++;
        }
    }
    private void PlayerLeftLobby(object playerInfo)
    {
        LobbyPlayer data = playerInfo as LobbyPlayer;
        if (data == null)
            return;
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            PlayerGeneral player = players[data.id];
            if (player != null)
            {
                RemovePlayer(player, data.gameStarted);

                //if (playerCount == 1)
                //{
                //    realPlayer.EndGame();
                //    print("kraj igre, dodaj neki ui ili sta vec, vrati u lobby...");
                //}
                //mzd ce trebati ovdje svim igracima staviti gamestarted na false al aj vidjecu
            }
        });
    }


    public void OnClickStartGame()
    {
        EventSystem.Emit(MessageType.SendNetworkMessage, 
            new NetworkMessage 
            {
                type = MessageType.StartGame,
                payload = ""
            });
    }
    private void GameStarted(object o)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            lobbyUI.SetActive(false);
            obstacleManager.RemoveAllObstacles();
            foreach (PlayerGeneral p in players.Values)
            {
                p.SpawnInGame();
            }
            realPlayer.StartGame(this, mainCamera);

            scoreManager.ShowScoreUI();
        });
    }
    public void ReloadLobby()
    {
        scoreManager.HideScoreUI();
        lobbyUI.SetActive(true);
        foreach (PlayerGeneral p in players.Values)
        {
            p.EndGame();
            NewInLobby(new LobbyPlayer
            {
                id = p.Id,
                gameStarted = false,

                //--not important
                colorHex = null,
                username = "",
            });
        }
        if(realPlayer != null)
            realPlayer.EndOwnerGame();
    }
    public void ClearOnLobbyExit()
    {
        scoreManager.HideScoreUI();
        if (realPlayer != null)
            realPlayer.EndOwnerGame();
        foreach(PlayerGeneral p in players.Values.ToList())
        {
            RemovePlayer(p, false);
        }
        realPlayer = null;
    }
    private void RemovePlayer(PlayerGeneral p, bool clientInGame)
    {
        p.EndGame();
        p.gameObject.SetActive(false);
        pooledPlayers.Push(p.gameObject);
        if (p.GetComponent<OwnerPlayerInput>() != null)
        {
            Destroy(p.GetComponent<OwnerPlayerInput>());
        }
        if (p.GetComponent<EnemyPlayer>() != null)
        {
            Destroy(p.GetComponent<EnemyPlayer>());
        }
        players.Remove(p.Id);
        playerCount--;

        UpdateLobby(clientInGame);

    }
    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.PopulateLobby, NewInLobby); 
        EventSystem.Subscribe(MessageType.DepopulateLobby, PlayerLeftLobby);
        EventSystem.Subscribe(MessageType.StartGame, GameStarted);
    }
    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.PopulateLobby, NewInLobby);
        EventSystem.Unsubscribe(MessageType.DepopulateLobby, PlayerLeftLobby);
        EventSystem.Unsubscribe(MessageType.StartGame, GameStarted);
    }
}


[System.Serializable]
public class LobbyPlayer
{
    public string username;
    public string id;
	public string colorHex;
    public bool gameStarted;
}