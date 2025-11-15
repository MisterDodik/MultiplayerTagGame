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

    private TextMeshPro playerNameTMPro;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector2 positionOrigin = new Vector2(0, 2);
    private List<Player> players = new();

    private int playerCount = 0;

    private void NewInLobby(object playerInfo)
    {
        LobbyPlayer data = playerInfo as LobbyPlayer;
        if (data == null)
        {
            Debug.LogError("DATA JE NULL – payload NIJE LobbyPlayer!");
            Debug.Log("playerInfo type = " + playerInfo.GetType());
            return;
        }
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            GameObject player = Instantiate(playerPrefab, lobbyPlayerParent);
            playerNameTMPro = player.GetComponentInChildren<TextMeshPro>();

            playerNameTMPro.text = data.username;

            int rem = playerCount % 4;
            player.transform.localPosition = positionOrigin + new Vector2(playerCount%4 * 1.5f, - playerCount/4 * 2);
            playerCount++;

            Player p = player.GetComponent<Player>();
            p.InitPlayer(data.username, data.id);
            players.Add(p);
        });
    }

    private void PlayerLeftLobby(object playerInfo)
    {
        LobbyPlayer data = playerInfo as LobbyPlayer;
        if (data == null)
            return;    

        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            Player player = players.FirstOrDefault(x => x.Id == data.id);
            if (player != null)
            {
                Destroy(player.gameObject);
                playerCount--;
                players.Remove(player);
            }
        });
    }
    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.PopulateLobby, NewInLobby); 
        EventSystem.Subscribe(MessageType.DepopulateLobby, PlayerLeftLobby);
    }
    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.PopulateLobby, NewInLobby);
        EventSystem.Unsubscribe(MessageType.DepopulateLobby, PlayerLeftLobby);
    }
}


[System.Serializable]
public class LobbyPlayer
{
    public string username;
    public string id;
}