using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Spawning;
using MLAPI.NetworkVariable;
using UnityEngine.Networking;
using TMPro;
using System.Collections.Generic;
using System;

public class StupidIdea : MonoBehaviour
{
    [SerializeField]
    public Dictionary<ulong, int> roundWins = new Dictionary<ulong, int>();
}