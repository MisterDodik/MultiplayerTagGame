using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MessageType
{
    public const string JoinLobby = "join_lobby";
    public const string PopulateLobby = "populate_lobby";
    public const string StartGame = "start_game";
    public const string ChatroomMsg = "chatroom_msg";
    public const string BroadcastToClients = "server_msg";

}

[System.Serializable]
public class NetworkMessage
{
    public string type;
    public string payload;
}



public class EventSystem: MonoBehaviour
{
    private static readonly Dictionary<string, Action<object>> eventTable = new Dictionary<string, Action<object>>();

    public static void Subscribe(string eventType, Action<object> listener)
    {
        if (!eventTable.ContainsKey(eventType))
            eventTable[eventType] = delegate { };

        eventTable[eventType] += listener;
    }
    public static void Unsubscribe(string eventType, Action<object> listener)
    {
        if (eventTable.ContainsKey(eventType))
            eventTable[eventType] -= listener;
    }

    public static void Emit(string eventType, object data = null)
    {
        if (eventTable.ContainsKey(eventType))
            eventTable[eventType].Invoke(data);
    }
}