using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing.Printing;
using static ChatSystem;
using System.Linq;

public static class JsonParser
{
    public static (string type, object payload) Parse(string json)
    {
        var jObj = JObject.Parse(json);

        string type = jObj["type"]?.ToString();
        JToken payloadToken = jObj["payload"];

        if (payloadToken.Type == JTokenType.String)
        {
            return (type, payloadToken.ToString());
        }

                //Debug.Log(payloadToken.ToString());
        switch (type)
        {
            case MessageType.ChatroomMsg:
                return (type, payloadToken.ToObject<ChatMessagePayload>());
            case MessageType.PopulateLobby:
            case MessageType.DepopulateLobby:
                return (type, payloadToken.ToObject<LobbyPlayer>());
            case MessageType.UpdatePositionFromServer:
                return (type, JsonConvert.DeserializeObject<List<PositionUpdateServer>>(payloadToken.ToString()));
            case MessageType.SpawnObstacle:
                return (type, JsonConvert.DeserializeObject<List<Obstacle>>(payloadToken.ToString()));
            default:
                return (type, payloadToken.ToString());
        }
    }
}
