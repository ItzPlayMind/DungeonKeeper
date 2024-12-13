using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Turret : ObjectiveAI
{
    protected override void OnDeath(ulong id)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].GetComponent<Inventory>()?.AddCash(GameManager.instance.GOLD_PER_TURRET);
        Destroy(gameObject);
    }

    protected override int SortTargets(CharacterStats stat1, CharacterStats stat2)
    {
        if(stat1 is ObjectiveStats)
            return -1;
        return 0;
    }
}
