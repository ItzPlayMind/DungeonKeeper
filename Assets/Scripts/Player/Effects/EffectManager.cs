using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EffectManager : NetworkBehaviour
{
    private Dictionary<string,Effect> activeEffects = new Dictionary<string,Effect>();

    private CharacterStats stats;

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer) return;
        stats = GetComponent<CharacterStats>();
    }

    private void Update()
    {
        if (!IsLocalPlayer) return;
        foreach (var effect in activeEffects)
        {
            effect.Value.Update(stats);
        }
    }

    private void AddEffect(Effect effect)
    {
        if(activeEffects.ContainsKey(effect.ID))
        {
            activeEffects[effect.ID].End(stats);
            activeEffects.Remove(effect.ID);
        }
        activeEffects.Add(effect.ID, effect);
        effect.onEnd += (_, _) => activeEffects.Remove(effect.ID);
        effect.Start(stats);
    }

    public void AddEffect(string id, int duration, int amount, CharacterStats applier)
    {
        AddEffectServerRPC(applier.NetworkObjectId, id, duration, amount);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddEffectServerRPC(ulong applierID, string id, int duration, int amount)
    {
        AddEffectClientRPC(applierID, id, duration, amount);
    }

    [ClientRpc]
    private void AddEffectClientRPC(ulong applierID, string id, int duration, int amount)
    {
        if(!IsLocalPlayer) return;
        var applier = NetworkManager.Singleton.SpawnManager.SpawnedObjects[applierID].GetComponent<CharacterStats>();
        var effect = (EffectRegistry.Instance as EffectRegistry).CreateEffect(id, duration, amount);
        effect.applier = applier;
        AddEffect(effect);
    }
}
