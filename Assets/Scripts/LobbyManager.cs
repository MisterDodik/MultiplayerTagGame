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
    [SerializeField] private PlayerLobby playerLobbyManager;
    [SerializeField] private ObstacleManager obstacleManager;
    [SerializeField] private Button joinLobbyBtn;
    private void Start()
    {
        seedPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }
    public async void OnJoinClicked(Button button)
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

        button.enabled = false;
        var result = await HttpConnection.PostRequest<LoginResponse>("localhost:8080/login", new 
        {
            seed = seedInput,
            username = usernameInput,	
        });
        if (result == null)
        {
            button.enabled = true;
            return;
        } 
        EventSystem.Emit("connect", result);
    }
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

    public void OnLeaveLobbyClicked()
    {
        EventSystem.Emit(MessageType.SendNetworkMessage, new NetworkMessage
        {
            type = MessageType.CloseConnection,
            payload = ""
        });
    }
    private void CloseConnectionHandler(object data)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            obstacleManager.RemoveAllObstacles();
            playerLobbyManager.ClearOnLobbyExit(true);
            seedPanel.SetActive(true);
            lobbyPanel.SetActive(false);
            joinLobbyBtn.enabled = true;
        });
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            EventSystem.Emit(MessageType.SendNetworkMessage, new NetworkMessage
            {
                type = MessageType.ExitToLobby,
                payload = ""
            });
        }
    }
    private void LoadLobbyHandler(object o)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() => { 
            playerLobbyManager.ReloadLobby();
            obstacleManager.RemoveAllObstacles();
        });
    }
    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.JoinLobby, LobbyJoinedHandler);
        EventSystem.Subscribe(MessageType.ExitToLobby, LoadLobbyHandler);
        EventSystem.Subscribe(MessageType.CloseConnection, CloseConnectionHandler);
    }
    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.JoinLobby, LobbyJoinedHandler);
        EventSystem.Unsubscribe(MessageType.ExitToLobby, LoadLobbyHandler);
        EventSystem.Unsubscribe(MessageType.CloseConnection, CloseConnectionHandler);

    }
}

[System.Serializable]
public class LoginResponse
{
    public string otp;
}