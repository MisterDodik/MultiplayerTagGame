using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing.Printing;
using static ChatSystem;

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

        switch (type)
        {
            case MessageType.ChatroomMsg:
                return (type, payloadToken.ToObject<ChatMessagePayload>());
            case MessageType.PopulateLobby:
                return (type, payloadToken.ToObject<LobbyPlayer>());
            default:
                return (type, payloadToken.ToString());
        }
    }
}
