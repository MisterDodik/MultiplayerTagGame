using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using PimDeWitte.UnityMainThreadDispatcher;
public class PlayerLobby : MonoBehaviour
{
    [SerializeField] private Transform lobbyPlayerParent;

    private TextMeshPro playerNameTMPro;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector2 positionOrigin = new Vector2(0, 2);
    private int playerCount = 0;

    private void NewInLobby(object playerInfo)
    {
        print(playerInfo.GetType());
        LobbyPlayer data = playerInfo as LobbyPlayer;
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            GameObject player = Instantiate(playerPrefab, lobbyPlayerParent);
            playerNameTMPro = player.GetComponentInChildren<TextMeshPro>();

            playerNameTMPro.text = data.username;

            int rem = playerCount % 4;
            player.transform.localPosition = positionOrigin + new Vector2(playerCount%4 * 1.5f, - playerCount/4 * 2);
            playerCount++;
        
        });

    }


    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.PopulateLobby, NewInLobby); 
    }
    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.PopulateLobby, NewInLobby);
    }
}


[System.Serializable]
public class LobbyPlayer
{
    public string username;
}