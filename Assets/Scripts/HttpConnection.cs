using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Security.Policy;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class HttpConnection
{
    public static async Task<T> PostRequest<T>(string url, object data)
    {
        string jsonData = JsonConvert.SerializeObject(data);
        string response = await SendRequest("POST", url, jsonData);
        return JsonUtility.FromJson<T>(response);
    }

    public static async Task PostJson(string url, object payload)
    {
        string json = JsonUtility.ToJson(payload);
        await SendRequest("POST", url, json);
    }

    private static async Task<string> SendRequest(string method, string url, string data)
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);

        UnityWebRequest www = new UnityWebRequest(url, method);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        var requestOp = www.SendWebRequest();

        while (!requestOp.isDone)
        {
            await Task.Yield();
        }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"HTTP {method} ERROR: {www.error}");
            throw new Exception(www.error);
        }
        return www.downloadHandler.text;
    }
}