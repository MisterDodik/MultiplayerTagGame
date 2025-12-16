using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public abstract class PlayerGeneral : MonoBehaviour
{
    private SpriteRenderer playerModel;
    private string Username { get; set; }
    public string Id {get; private set; }
    public bool IsHunter {get; private set; }

    private bool gameStarted = false;

    private Vector2 targetPosition;

    private float rotationSpeed = 5;
    //private bool isOtherPlayer = false;
    public void InitPlayer(string username, string id, bool _isOtherPlayer, PlayerLobby _playerManager, string colorHex)
    {
        gameStarted = false;
        Username = username;
        Id = id;

        playerModel = GetComponentInChildren<SpriteRenderer>();

        Color newColor;
	    if (ColorUtility.TryParseHtmlString(colorHex, out newColor) && playerModel!=null)
	    {
            playerModel.color = newColor;
	    }

        targetPosition = transform.localPosition;
        //playerManager = _playerManager;
        // isOtherPlayer = _isOtherPlayer;
    }
    public void SpawnInGame()
    {
        transform.localScale = new Vector3(0.3f, 0.3f, 1);
        gameStarted = true;
    }

   
    public virtual void Update()
    {
        if (!gameStarted)
            return;
        transform.localPosition = Vector2.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * 10f
        );

        //Vector2 direction = (targetPosition - (Vector2)transform.localPosition).normalized;
        //Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
        //transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
       // transform.localRotation = Quaternion.Euler(0, 0, targetRotation.eulerAngles.z);
    }
    public void SetTargetPosition(Vector2 pos)
    {
        targetPosition = pos;
    }

    public void UpdateClientColor(Color c, bool _isHunter)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
        
            playerModel.color = c;
            IsHunter = _isHunter;
            print("ishunter state updated to: " + IsHunter);
        });
    }
}

