using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private GameObject seedPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private TMP_InputField seedInputTMPro;
    [SerializeField] private TMP_InputField usernameInputTMPro;
    [SerializeField] private TextMeshProUGUI lobbyNameTMPro;

    private void Start()
    {
        seedPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }
    public async void OnJoinClicked()
    {
        string seedInput = seedInputTMPro.text;
        seedInput = seedInput.Trim();

        if (seedInput.Length < 1)
        {
            print("seed cannot be empty");
            return;
        }

        string usernameInput = usernameInputTMPro.text;
        usernameInput = usernameInput.Trim();

        if (usernameInput.Length < 1)
        {
            print("username cannot be empty");
            return;
        }


        var result = await HttpConnection.PostRequest<LoginResponse>("localhost:8080/login", new 
        {
            seed = seedInput,
            username = usernameInput,	
        });

        EventSystem.Emit("connect", result);
    }
    //private int testCount = 0;
    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        test("user"+testCount.ToString());
    //        testCount++;
    //    }
    //}

    //private async void test(string username)
    //{
    //    var result = await HttpConnection.PostRequest<LoginResponse>("localhost:8080/login", new
    //    {
    //        seed = "123",
    //        username = username,
    //    });

    //    EventSystem.Emit("connect", result);
    //}
    private void LobbyJoinedHandler(object data)
    {
        print("connected to lobby");
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            lobbyNameTMPro.text = data.ToString();
            seedPanel.SetActive(false);
            lobbyPanel.SetActive(true);
        });
    }

    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.JoinLobby, LobbyJoinedHandler);
    }
    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.JoinLobby, LobbyJoinedHandler);
    }
}

[System.Serializable]
public class LoginResponse
{
    public string otp;
}