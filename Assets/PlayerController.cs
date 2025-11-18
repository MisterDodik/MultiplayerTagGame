using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool GameStarted { get; private set; }

    private PlayerLobby playerManager;

    public void StartGame(PlayerLobby _playerManager)
    {
        GameStarted = true;
        playerManager = _playerManager;
    }
    public void EndGame()
    {
        GameStarted = false;
    }


    private void FixedUpdate()
    {
        if (!GameStarted)
            return;

        if (Input.GetAxisRaw("horizontal") != 0 || Input.GetAxisRaw("vertical") != 0)
        {
            EventSystem.Emit(MessageType.SendNetworkMessage, new NetworkMessage
            {
                type = MessageType.UpdatePositionFromClient,
                payload = (new PositionUpdateClient
                {
                    inputX = Input.GetAxisRaw("horizontal"),
                    inputY = Input.GetAxisRaw("vertical")
                })
            });

        }
    }



    private void UpdatePositionsHandler(object o)
    {
        var playerInfoList = o as List<PositionUpdateServer>;
        foreach (PositionUpdateServer item in playerInfoList)
        {
            playerManager.players[item.id].SetTargetPosition(new Vector2(item.x, item.y));
        }
    }
    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.UpdatePositionFromServer, UpdatePositionsHandler);
    }
    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.UpdatePositionFromServer, UpdatePositionsHandler);
    }
}



[System.Serializable]
public class PositionUpdateServer
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