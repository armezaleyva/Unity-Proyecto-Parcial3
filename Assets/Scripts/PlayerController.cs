using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.NetworkVariable;

public class PlayerController : NetworkBehaviour
{
    [SerializeField]
    float moveSpeed = 5f;
    [SerializeField]
    Transform movePoint;

    [SerializeField]
    LayerMask whatStopsMovement;

    public override void NetworkStart()
    {
        base.NetworkStart();
        /*foreach(MLAPI.Connection.NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            Debug.Log(client.PlayerObject.name);
        }*/
        /*NetworkManager.Singleton.OnClientConnectedCallback += client =>{
            Debug.Log(client);
        };*/
        //NetworkObject.name = Gamemanager.instance.currentUsername;
        //Debug.Log(NetworkObject.name);
        //NetworkManager.Singleton.LocalClientId;
    }

    void Start()
    {
        movePoint.parent = null;
    }

    void Update()
    {
        if(IsLocalPlayer){
            transform.position = Vector3.MoveTowards(transform.position, movePoint.position, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, movePoint.position) <= 0.05f) 
            {
                if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f)
                {
                    Vector3 desiredMovement = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, 0f);
                    if (!Physics2D.OverlapCircle(movePoint.position + desiredMovement, .2f, whatStopsMovement)) 
                    {
                        movePoint.position += desiredMovement;
                    }
                } 
                else if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f)
                {
                    Vector3 desiredMovement = new Vector3(0f, Input.GetAxisRaw("Vertical"), 0f);
                    if (!Physics2D.OverlapCircle(movePoint.position + desiredMovement, .2f, whatStopsMovement)) 
                    {
                        movePoint.position += new Vector3(0f, Input.GetAxisRaw("Vertical"), 0f);
                    }
                }
            }
        }
    }
}
