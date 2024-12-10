using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DestroyableStats : CharacterStats
{
    protected override void Die(ulong damagerID)
    {
        base.Die(damagerID);
        DestroyServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyServerRPC()
    {
        Destroy(gameObject);
    }
}
