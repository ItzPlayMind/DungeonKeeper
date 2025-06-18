using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TrainingGameManager : GameManager
{
    [SerializeField] private List<ObjectSpawner> trainingDummySpawns = new List<ObjectSpawner>();
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        foreach (var spawn in trainingDummySpawns)
        {
            var dummy = spawn.Instantiate();
            dummy.Spawn();
        }
    }
}
