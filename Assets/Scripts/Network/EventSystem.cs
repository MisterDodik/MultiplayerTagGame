using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MessageType
{
    public const string BroadcastToClients = "server_msg";
    public const string SendNetworkMessage = "SendNetworkMessage";

    public const string JoinLobby = "join_lobby";
    public const string PopulateLobby = "populate_lobby";
    public const string DepopulateLobby = "depopulate_lobby";

    public const string ChatroomMsg = "chatroom_msg";

    public const string StartGame = "start_game";
    public const string ExitToLobby = "exit_to_lobby";
    public const string CloseConnection = "close_connection";

    public const string UpdateClientColor = "update_color_from_server";
    public const string UpdatePositionFromClient = "update_position_from_client";
    public const string UpdateRotationFromClient = "update_rotation_from_client";

    public const string UpdatePositionFromServer = "update_position_from_server";
    public const string HunterAttack = "hunter_attack";

    public const string SpawnObstacle = "spawn_grid_obstacle";
    public const string RemoveObstacle = "remove_grid_obstacle";

    public const string InfoEvent = "send_info";

    public const string UpdateScore = "update_single_score";
    public const string EndGameUpdateScore = "end_game";
}

[System.Serializable]
public class NetworkMessage
{
    public string type;
    public object payload;
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