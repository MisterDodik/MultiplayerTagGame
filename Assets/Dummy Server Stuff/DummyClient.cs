using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyClient : MonoBehaviour
{

    public DummyServer server;
    private Client client;
    private Vector2 targetPosition;
    void Start()
    {
        client = server.NewClient(this);     //ovo ce da bude join game ili sta vec
    }

    private void Update()
    {
        if (Input.GetAxisRaw("horizontal") != 0 || Input.GetAxisRaw("vertical") != 0)
        {
            //EventSystem.Emit(MessageType.SendNetworkMessage, new NetworkMessage
            //{
            //    type = MessageType.UpdatePositionFromClient,
            //    payload = (new PositionUpdateClient
            //    {
            //        inputX = Input.GetAxisRaw("horizontal"),
            //        inputY = Input.GetAxisRaw("vertical")
            //    })
            //});
            server.ReceiveInput(client, Input.GetAxisRaw("horizontal"), Input.GetAxisRaw("vertical"), this);
        }

        transform.localPosition = Vector2.Lerp(
           transform.localPosition,
           targetPosition,
           Time.deltaTime * 10f
       );
    }
    public void UpdatePositionsHandler(Vector2 pos)
    {
        //var playerInfoList = o as List<PositionUpdateServer>;
        //foreach (PositionUpdateServer item in playerInfoList)
        //{
        //    playerManager.players[item.id].SetTargetPosition(new Vector2(item.x, item.y));
        //}
        SetTargetPosition(pos);


    }
    public void SetTargetPosition(Vector2 pos)
    {
        targetPosition = pos;
    }
}