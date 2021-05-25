using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Spawning;

public class ConnectionManager : MonoBehaviour
{
    public GameObject connectionButtonPanel;
    ulong? prefabHash = NetworkSpawnManager.GetPrefabHashFromGenerator("Player2"); 
    public void Host()
    {
        connectionButtonPanel.SetActive(false);
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.StartHost(new Vector3(-6f, 0, 0), Quaternion.identity);
    }

    private void ApprovalCheck(byte[] connectionData, ulong clientID, NetworkManager.ConnectionApprovedDelegate callback)
    {
        Debug.Log("Approving a connection");
        callback(true, prefabHash, true, new Vector3(6f, 0, 0), Quaternion.identity);
    }

    public void Join()
    {
        connectionButtonPanel.SetActive(false);
        NetworkManager.Singleton.StartClient();
    }
}
