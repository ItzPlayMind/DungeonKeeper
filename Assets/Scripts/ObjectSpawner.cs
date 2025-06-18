using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject prefab;
    
    public NetworkObject Instantiate()
    {
        return Instantiate(prefab,transform.position,transform.rotation);
    }
}
