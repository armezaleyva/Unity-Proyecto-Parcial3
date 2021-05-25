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
    protected Animator anim;
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
    NetworkVariable<GameObject> roundWinsObject;
    bool gameStarted = false;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();        
    }

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
                    if (IsHost) CalculateWinnerAndResetPlantsServerRPC();
                    if (IsHost) CheckGameOverServerRPC();
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
                    if(IsLocalPlayer)
                    {
                        anim.SetTrigger("plant");
                    }
                }

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    CutPlant();
                }
            }
        }
    }

    void LateUpdate()
    {
        if(IsLocalPlayer)
        {
            anim.SetFloat("moveX", Input.GetAxisRaw("Horizontal"));
            anim.SetFloat("moveY", Input.GetAxisRaw("Vertical"));
            if(gameStarted) anim.SetBool("gameStarted", true);
            else anim.SetBool("gameStarted", false);
        }
    }

    [ServerRpc]
    private void CalculateWinnerAndResetPlantsServerRPC()
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

            if (obj.gameObject.CompareTag("Plant"))
            {
                NetworkManager.Destroy(obj.gameObject);
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

    
        Debug.Log("The round winners are...");
        foreach (var winnerId in currentWinnersIds)
        {
            if (roundWinsObject.Value ==  null)
            {
                if (IsHost)
                {
                    GameObject test = new GameObject();
                    test.AddComponent<StupidIdea>();
                    test.AddComponent<NetworkObject>();
                    StupidIdea xd = test.GetComponent<StupidIdea>();
                    test.GetComponent<NetworkObject>().Spawn();
                    test.tag = "DontDestroy";

                    roundWinsObject.Value = test;
                    Debug.Log(roundWinsObject.Value);
                }
            }
            GameObject gameObject = roundWinsObject.Value;
            Dictionary<ulong, int> roundWins = gameObject.GetComponent<StupidIdea>().roundWins;

            if (roundWins.ContainsKey(winnerId.Value))
            {
                roundWins[winnerId.Value] += 1;
            }
            else
            {
                roundWins[winnerId.Value] = 1;
            }
            Debug.Log(winnerId);
        }
    }

    [ServerRpc]
    private void CheckGameOverServerRPC()
    {
        GameObject gameObject = roundWinsObject.Value;
        Dictionary<ulong, int> roundWins = gameObject.GetComponent<StupidIdea>().roundWins;
        List<ulong> winners = new List<ulong>();
        foreach (KeyValuePair<ulong, int> entry in roundWins)
        {
            if (entry.Value >= 3)
            {
                winners.Add(entry.Key);
            }
            Debug.Log("ID and wins");
            Debug.Log(entry.Key);
            Debug.Log(entry.Value);
        }

        if (winners.Count > 0)
        {
            Debug.Log("The MATCH winners are...");
            foreach (ulong winnerId in winners)
            {
                Debug.Log(winnerId);
            }
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
    private void SpawnServerRPC(ulong netID, Color color)
    {
        Collider2D intersectingCollider = Physics2D.OverlapBox(transform.position, new Vector2(0.5f, 0.5f), 0);
        if (intersectingCollider == null) {
            int prefabIndex = UnityEngine.Random.Range(0, plantPrefabs.Length - 1);
            GameObject go = Instantiate(plantPrefabs[prefabIndex], transform.position, Quaternion.identity);
            go.GetComponent<NetworkObject>().SpawnWithOwnership(netID); 
            go.GetComponentInChildren<SpriteRenderer>().color = color;
            ulong itemNetID = go.GetComponent<NetworkObject>().NetworkObjectId;
            SpawnClientRPC(itemNetID, color);
        }
    }

    [ServerRpc]
    private void GetPlayersServerRPC()
    {
        players.Value = NetworkManager.ConnectedClientsList.Count;
    }

    [ClientRpc]
    private void SpawnClientRPC(ulong itemNetID, Color color)
    {
        NetworkObject netObj = NetworkSpawnManager.SpawnedObjects[itemNetID];
        netObj.GetComponentInChildren<SpriteRenderer>().color = color;
    }
    
    void SpawnPlant()
    {
        Color color;
        if(IsHost) color = Color.red;
        else color = Color.blue;
        SpawnServerRPC(NetworkManager.Singleton.LocalClientId, color);
    }

    void CutPlant()
    {
        CutServerRPC();
    }

}
