using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class InfoEventManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textPopupTMPro;
    [SerializeField] private GameObject textPopupGO;

    private float messageTime = 2f;

    private Queue<string> messages = new Queue<string>();
    private Coroutine showMsgCoroutine;

    private void Start()
    {
        textPopupGO.SetActive(false);
    }
    private void Update()
    {
        if (messages.Count > 0 && showMsgCoroutine == null)
        {
            showMsgCoroutine = StartCoroutine(ShowText());
        }
    }
    private IEnumerator ShowText()
    {
        textPopupGO.SetActive(true);
        string text = messages.Dequeue();
        textPopupTMPro.text = text;

        yield return new WaitForSeconds(messageTime);

        textPopupGO.SetActive(false);
        showMsgCoroutine = null;
    }
    private void InfoEventHandler(object o)
    {
        InfoData data = o as InfoData;
        messages.Enqueue(data.message);
    }

    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.InfoEvent, InfoEventHandler);
    }
    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.InfoEvent, InfoEventHandler);
    }
}
[System.Serializable]
public class InfoData
{
    public string message;
}
