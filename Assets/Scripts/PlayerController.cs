using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Spawning;
using MLAPI.NetworkVariable;
using UnityEngine.Networking;
using TMPro;
using System.Collections.Generic;
using System;

public class PlayerController : NetworkBehaviour
{
    public Color color;
    [SerializeField]
    float moveSpeed = 5f;
    [SerializeField]
    Transform movePoint;
    [SerializeField]
    Transform lookPoint;

    [SerializeField]
    LayerMask whatStopsMovement;
    TextMeshProUGUI txtGameTime;
    TextMeshProUGUI txtWaitingForPlayers;
    [SerializeField]
    GameObject[] plantPrefabs;

    private float timer;
    int roundDuration = 15;
    int downtimeDuration = 5;
    NetworkVariableInt players;
    bool gameStarted = false;

    void Start()
    {
        movePoint.parent = null;
        lookPoint.parent = null;

        timer = downtimeDuration;
        txtGameTime = GameObject.Find("GameTimerText").GetComponentInChildren<TextMeshProUGUI>();
        txtWaitingForPlayers = GameObject.Find("WaitingForPlayers").GetComponentInChildren<TextMeshProUGUI>();

        txtGameTime.text = ((int)timer).ToString();
        txtWaitingForPlayers.text = "Waiting for players...";
    }

    void Update()
    {
        GetPlayersServerRPC();
        if (players.Value >= 2) {
            txtWaitingForPlayers.gameObject.SetActive(false);
            // Start countdown
            if(timer > 0)
            {
                timer -= Time.deltaTime;
                txtGameTime.SetText(((int)timer).ToString());
            }
            else
            {
                if (gameStarted)
                {
                    // Calculate winner and reset round
                    if (IsHost) CalculateWinnerServerRPC();
                    // Todo : Reset round
                    // Todo : Check if game over
                    gameStarted = false;
                    timer = downtimeDuration;
                }
                else
                {
                    //Start game
                    gameStarted = true;
                    timer = roundDuration;
                }
            }
        }
        

        if(gameStarted && IsLocalPlayer)
        {
            transform.position = Vector3.MoveTowards(transform.position, movePoint.position, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, movePoint.position) <= 0.05f) 
            {
                if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f)
                {
                    Vector3 desiredMovement = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, 0f);
                    if (!Physics2D.OverlapCircle(movePoint.position + desiredMovement, .2f, whatStopsMovement)) 
                    {
                        movePoint.position += desiredMovement;
                        lookPoint.position = movePoint.position + desiredMovement;              
                    }
                    else 
                    {
                        lookPoint.position = transform.position + desiredMovement;        
                    }
                } 
                else if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f)
                {
                    Vector3 desiredMovement = new Vector3(0f, Input.GetAxisRaw("Vertical"), 0f);
                    if (!Physics2D.OverlapCircle(movePoint.position + desiredMovement, .2f, whatStopsMovement)) 
                    {
                        movePoint.position += new Vector3(0f, Input.GetAxisRaw("Vertical"), 0f);
                        lookPoint.position = movePoint.position + desiredMovement;           
                    }
                    else 
                    {
                        lookPoint.position = transform.position + desiredMovement;       
                    }
                }

                if (Input.GetKey(KeyCode.Space)) 
                {
                    SpawnPlant();
                }

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    CutPlant();
                }
            }
        }
    }

    [ServerRpc]
    private void CalculateWinnerServerRPC()
    {     
        Debug.Log("Calculating winner...");
        var spawnedObjects = NetworkSpawnManager.SpawnedObjects;
        Dictionary<ulong, int> playerPlants = new Dictionary<ulong, int>();
        foreach (NetworkObject obj in spawnedObjects.Values)
        {
            var playerId = obj.OwnerClientId;
            if (playerPlants.ContainsKey(playerId))
            {
                playerPlants[playerId] += 1;
            }
            else
            {
                playerPlants[playerId] = 0;
            }
        }

        var currentBest = -1;
        List<ulong?> currentWinnersIds = new List<ulong?>();
        foreach (KeyValuePair<ulong, int> entry in playerPlants)
        {
            if (entry.Value > currentBest)
            {
                currentBest = entry.Value;
                currentWinnersIds.Clear();
                currentWinnersIds.Add(entry.Key);
            }
            else if (entry.Value == currentBest)
            {
                currentWinnersIds.Add(entry.Key);
            }
        }

        Debug.Log("The winners are...");
        foreach (var xd in currentWinnersIds)
        {
            Debug.Log(xd);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CutServerRPC()
    {
        Collider2D intersectingCollider = Physics2D.OverlapBox(lookPoint.position, new Vector2(0.5f, 0.5f), 0);
        if (intersectingCollider != null) {
            if (intersectingCollider.tag == "Plant")
            {
                ulong itemNetID = intersectingCollider.gameObject.GetComponent<NetworkObject>().NetworkObjectId;
                NetworkObject no = NetworkSpawnManager.SpawnedObjects[itemNetID];
                NetworkManager.Destroy(no.gameObject);
            }
        }
    }

    [ServerRpc]
    private void SpawnServerRPC(ulong netID)
    {
        Collider2D intersectingCollider = Physics2D.OverlapBox(transform.position, new Vector2(0.5f, 0.5f), 0);
        if (intersectingCollider == null) {
            int prefabIndex = UnityEngine.Random.Range(0, plantPrefabs.Length - 1);
            GameObject go = Instantiate(plantPrefabs[prefabIndex], transform.position, Quaternion.identity);
            go.GetComponent<NetworkObject>().SpawnWithOwnership(netID); 
            ulong itemNetID = go.GetComponent<NetworkObject>().NetworkObjectId;
            SpawnClientRPC(itemNetID);
        }
    }

    [ServerRpc]
    private void GetPlayersServerRPC()
    {
        players.Value = NetworkManager.ConnectedClientsList.Count;
    }

    [ClientRpc]
    private void SpawnClientRPC(ulong itemNetID)
    {
        NetworkObject netObj = NetworkSpawnManager.SpawnedObjects[itemNetID];
    }
    
    void SpawnPlant()
    {
        SpawnServerRPC(NetworkManager.Singleton.LocalClientId);
    }

    void CutPlant()
    {
        CutServerRPC();
    }

}
