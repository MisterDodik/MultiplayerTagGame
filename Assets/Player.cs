using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class Player : MonoBehaviour
{
    private string Username { get; set; }
    public string Id {get; private set; }

    private PlayerLobby playerManager;

    [HideInInspector] public Vector2 TargetPosition;
    private bool gameStarted = false;
    private bool isOtherPlayer = false;
    public void InitPlayer(string username, string id, bool _isOtherPlayer, PlayerLobby _playerManager)
    {
        gameStarted = false;
        Username = username;
        Id = id;
        playerManager = _playerManager;
        isOtherPlayer = _isOtherPlayer;
    }
    public void SpawnInGame()
    {
        transform.localScale = new Vector3(0.25f, 0.25f, 1);
        gameStarted = true;
    }

    private void FixedUpdate()
    {
        if (!gameStarted && isOtherPlayer)
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

        transform.localPosition = Vector2.Lerp(
            transform.position,
            TargetPosition,
            Time.deltaTime * 10f
        );
    }

    private void UpdatePositionsHandler(object o)
    {
        var playerInfoList = o as List<PositionUpdateServer>;
        foreach(PositionUpdateServer item in playerInfoList)
        {
            playerManager.players[item.id].SetTargetPosition(new Vector2(item.x, item.y));
        }
    }
    public void SetTargetPosition(Vector2 pos)
    {
        TargetPosition = pos;
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