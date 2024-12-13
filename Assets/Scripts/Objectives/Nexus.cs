using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Nexus : ObjectiveAI
{
    [SerializeField] private List<ObjectiveAI> otherObjectives = new List<ObjectiveAI>();

    public void AddPreviousObjective(ObjectiveAI objective) => otherObjectives.Add(objective);

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        stats = GetComponent<CharacterStats>();
        stats.stats.damageReduction.ChangeValue += (ref float value, float old) =>
        {
            if (otherObjectives.Any(x => x != null)) value = 100;
        };
        stats.OnServerDeath += OnDeath;
    }

    protected override void OnDeath(ulong id)
    {
        var team = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].gameObject.layer;
        GameManager.instance.Win(team);
    }

    protected override int SortTargets(CharacterStats stat1, CharacterStats stat2)
    {
        if (stat1 is ObjectiveStats)
            return -1;
        return 0;
    }
}
