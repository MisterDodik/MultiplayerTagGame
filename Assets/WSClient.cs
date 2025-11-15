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
        };

        ws.Connect();
    }

    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        var (type, payload) = JsonParser.Parse(e.Data);
        print(type);
        EventSystem.Emit(type, payload);
    }

    private void SendMessage(NetworkMessage msg)
    {
        string json = JsonUtility.ToJson(msg);
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
        EventSystem.Subscribe("SendNetworkMessage", OnSendNetworkMessage);
        EventSystem.Subscribe("connect", Connect);
    }

    private void OnDisable()
    {
        EventSystem.Unsubscribe("SendNetworkMessage", OnSendNetworkMessage);
        EventSystem.Unsubscribe("connect", Connect);
    }

    private void OnDestroy()
    {
        ws.Close();
    }
}
