using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;
using System.Security.Policy;
using Unity.VisualScripting;

public class WSClient : MonoBehaviour
{
    private WebSocket ws;
    public void Connect(object data)
    {
        LoginResponse loginResponse = data as LoginResponse;

        ws = new WebSocket("ws://localhost:8080/ws?otp=" + loginResponse.otp);

        ws.OnMessage += OnMessageReceived;

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("Connected to server");
        };

        ws.OnClose += (sender, e) =>
        {
            Debug.Log("Connection closed");
            EventSystem.Emit(MessageType.CloseConnection, null);
        };

        ws.Connect();
    }

    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        var (type, payload) = JsonParser.Parse(e.Data);
        if(type != MessageType.UpdatePositionFromServer && type != MessageType.RemoveObstacle)
            print("received payload with type: " + type);
        EventSystem.Emit(type, payload);
    }

    private void SendMessage(NetworkMessage msg)
    {
        string json = JsonConvert.SerializeObject(msg);
        ws.Send(json);
    }


    private void OnSendNetworkMessage(object data)
    {   
        var msg = data as NetworkMessage;
        if (msg != null)
            SendMessage(msg);
    }
    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.SendNetworkMessage, OnSendNetworkMessage);
        EventSystem.Subscribe("connect", Connect);
    }

    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.SendNetworkMessage, OnSendNetworkMessage);
        EventSystem.Unsubscribe("connect", Connect);
    }

    private void OnDestroy()
    {
        ws.Close();
    }
}
