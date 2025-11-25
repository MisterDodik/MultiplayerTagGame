using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class Player : MonoBehaviour
{
    private string Username { get; set; }
    public string Id {get; private set; }

    private bool gameStarted = false;

    private Vector2 targetPosition;
    //private bool isOtherPlayer = false;
    public void InitPlayer(string username, string id, bool _isOtherPlayer, PlayerLobby _playerManager, string colorHex)
    {
        gameStarted = false;
        Username = username;
        Id = id;

	    Color newColor;
	    if (ColorUtility.TryParseHtmlString(colorHex, out newColor))
	    {
		    GetComponent<SpriteRenderer>().color = newColor;
	    }
	
        //playerManager = _playerManager;
       // isOtherPlayer = _isOtherPlayer;
    }
    public void SpawnInGame()
    {
        transform.localScale = new Vector3(0.25f, 0.25f, 1);
        gameStarted = true;
    }

   
    private void Update()
    {
        if (!gameStarted)
            return;
        transform.localPosition = Vector2.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * 10f
        );
    }
    public void SetTargetPosition(Vector2 pos)
    {
        targetPosition = pos;
    }

   

}

