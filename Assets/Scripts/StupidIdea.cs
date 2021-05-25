using UnityEngine;
using System.Collections.Generic;

public class AuxComponent : MonoBehaviour
{
    [SerializeField]
    public Dictionary<ulong, int> roundWins = new Dictionary<ulong, int>();
}