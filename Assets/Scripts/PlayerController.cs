using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Spawning;

public class PlayerController : NetworkBehaviour
{
    [SerializeField]
    float moveSpeed = 5f;
    [SerializeField]
    Transform movePoint;

    [SerializeField]
    LayerMask whatStopsMovement;
    [SerializeField]
    GameObject[] plantPrefabs;

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

                if (Input.GetKey(KeyCode.Space)) {
                    SpawnPlant();
                }
            }
        }
    }

    [ServerRpc]
    private void SpawnServerRPC(int prefabIndex)
    {
        Collider2D intersectingCollider = Physics2D.OverlapBox(transform.position, new Vector2(0.5f, 0.5f), 0);
        if (intersectingCollider == null) {
            GameObject go = Instantiate(plantPrefabs[prefabIndex], transform.position, Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn(); 
            ulong itemNetID = go.GetComponent<NetworkObject>().NetworkObjectId;

            SpawnClientRPC(itemNetID);
        }
    }

    [ClientRpc]
    private void SpawnClientRPC(ulong itemNetID)
    {
        NetworkObject netObj = NetworkSpawnManager.SpawnedObjects[itemNetID];

    }

    void SpawnPlant()
    {
            int prefabIndex = Random.Range(0, plantPrefabs.Length - 1);
            Debug.Log(prefabIndex);           
            SpawnServerRPC(prefabIndex);
    }
}
