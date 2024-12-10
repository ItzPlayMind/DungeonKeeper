using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Nexus : ObjectiveAI
{

    protected override void OnDeath(ulong id)
    {
        var team = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].gameObject.layer;
        GameManager.instance.Win(team);
    }
}
