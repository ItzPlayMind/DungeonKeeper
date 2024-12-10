using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Turret : ObjectiveAI
{
    protected override void OnDeath(ulong id)
    {
        OnDeathServerRPC(id);
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnDeathServerRPC(ulong killer)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects[killer].GetComponent<Inventory>()?.AddCash(GameManager.instance.GOLD_PER_TURRET);
        Destroy(gameObject);
    }
}
