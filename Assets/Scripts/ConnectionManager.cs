using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class ConnectionManager : MonoBehaviour
{
    public GameObject connectionButtonPanel;
    public void Host()
    {
        connectionButtonPanel.SetActive(false);
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.StartHost(new Vector3(-6f, 0, 0), Quaternion.identity);
    }

    private void ApprovalCheck(byte[] connectionData, ulong clientID, NetworkManager.ConnectionApprovedDelegate callback)
    {
        Debug.Log("Approving a connection");
        callback(true, null, true, new Vector3(6f, 0, 0), Quaternion.identity);
    }

    public void Join()
    {
        connectionButtonPanel.SetActive(false);
        NetworkManager.Singleton.StartClient();
    }
}
