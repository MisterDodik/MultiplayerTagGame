using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OwnerPlayerInput : PlayerGeneral
{
    private float currentRotationTimer = 0;
    public bool GameStarted { get; private set; }

    private PlayerLobby playerManager;

    private Transform mainCamera;
    public void StartGame(PlayerLobby _playerManager, Transform _camera)
    {
        GameStarted = true;
        playerManager = _playerManager;
        
        mainCamera = _camera;
        mainCamera.parent = transform;
        mainCamera.localPosition = new Vector3(0, 0, -10);
    }
    public void EndGame()
    {
        GameStarted = false;
        mainCamera.parent = null;
        mainCamera.localPosition = new Vector3(0, 0, -10);
    }


    public override void Update()
    {
        //if (!GameStarted)
        //    return;
        base.Update();

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

        if (IsHunter && Input.GetKeyDown(KeyCode.Space))
        {
            EventSystem.Emit(MessageType.SendNetworkMessage, new NetworkMessage
            {
                type = MessageType.HunterAttack,
                payload = (new PositionUpdateClient{})
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