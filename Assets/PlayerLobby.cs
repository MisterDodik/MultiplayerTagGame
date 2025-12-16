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
    private OwnerPlayerInput realPlayer;


    private int playerCount = 0;

    [SerializeField] private Transform mainCamera;
    private void NewInLobby(object playerInfo)
    {
        LobbyPlayer data = playerInfo as LobbyPlayer;
        if (data == null)
            return;
        
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            GameObject player = Instantiate(playerPrefab, lobbyPlayerParent);
            player.transform.localScale = new Vector3(0.75f, 1f, 1);
            playerNameTMPro = player.GetComponentInChildren<TextMeshPro>();

            playerNameTMPro.text = data.username;

            if (playerCount == 0)
            {
                realPlayer = player.AddComponent<OwnerPlayerInput>();
            }
            else
            {
                player.AddComponent<EnemyPlayer>();
            }

            player.transform.localPosition = positionOrigin + new Vector2(playerCount%4 * 1.5f, - playerCount/4 * 2);
            playerCount++;

            PlayerGeneral p = player.GetComponent<PlayerGeneral>();
            p.InitPlayer(data.username, data.id, playerCount > 1, this, data.colorHex);
            players[data.id] = p;
        });
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
                playerCount--;
                players.Remove(data.id);
                Destroy(player.gameObject);

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
            foreach (PlayerGeneral p in players.Values)
            {
                p.SpawnInGame();
            }
            realPlayer.StartGame(this, mainCamera);
        });
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
}