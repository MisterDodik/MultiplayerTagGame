using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private string Username { get; set; }
    public string Id {get; private set; }

    public void InitPlayer(string username, string id)
    {
        Username = username;
        Id = id;
    }

}
