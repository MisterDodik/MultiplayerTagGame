using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ChatSystem : MonoBehaviour
{
    [SerializeField] private TMP_InputField msgInputField;
    [SerializeField] private TextMeshProUGUI chat;
    [SerializeField] private ScrollRect scrollRect;


    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.ChatroomMsg, OnChatMessageReceived);
    }
    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.ChatroomMsg, OnChatMessageReceived);
    }

    private void OnChatMessageReceived(object data)
    {
        Debug.Log("Chat message received");
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            msgInputField.text = "";
            
            if( data is ChatMessagePayload msg)
            {
                AppendMessage(msg);
            }
        });
    }

    public void OnSendClicked()
    {
        string message = msgInputField.text;
        if (message == "")
        {
            Debug.LogError("cannot send empty message");
            return;
        }

        var msg = new NetworkMessage 
        { 
            type = MessageType.ChatroomMsg, 
            payload = message 
        };
        EventSystem.Emit("SendNetworkMessage", msg); 
    }
    private void AppendMessage(ChatMessagePayload msg)
    {
        string message = "\n" + msg.sentAt + " " + msg.from + ": " + msg.message;
        chat.text += message;

        scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, chat.preferredHeight + 70);
        scrollRect.verticalNormalizedPosition = 0;
    }

    [System.Serializable]
    public class ChatMessagePayload
    {
        public string from;
        public string sentAt;
        public string message;
    }
}
