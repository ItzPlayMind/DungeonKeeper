using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class EffectManager : NetworkBehaviour
{
    [SerializeField] private UIIconBar iconPrefab;
    [SerializeField] protected Transform effectBar;
    private Dictionary<string,Effect> activeEffects = new Dictionary<string,Effect>();

    private CharacterStats stats;

    public override void OnNetworkSpawn()
    {
        stats = GetComponent<CharacterStats>();
    }

    private void Update()
    {
        var keys = activeEffects.Keys.ToArray();
        for (int i = 0; i < keys.Length; i++)
        {
            activeEffects[keys[i]].Update(stats);
        }
    }

    private void AddEffect(Effect effect)
    {
        if(activeEffects.ContainsKey(effect.ID))
        {
            if (activeEffects[effect.ID].amount < effect.amount)
            {
                activeEffects[effect.ID].End(stats);
                activeEffects.Remove(effect.ID);
            }
            else return;
        }
        activeEffects.Add(effect.ID, effect);
        effect.onEnd += (_, _) => activeEffects.Remove(effect.ID);
        if (effectBar != null && iconPrefab != null)
        {
            var icon = Instantiate(iconPrefab, effectBar);
            icon.Icon = effect.icon;
            effect.activeIcon = icon;
        }
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
        //if(!IsLocalPlayer) return;
        var applier = NetworkManager.Singleton.SpawnManager.SpawnedObjects[applierID].GetComponent<CharacterStats>();
        var effect = (EffectRegistry.Instance as EffectRegistry).CreateEffect(id, duration, amount);
        effect.applier = applier;
        AddEffect(effect);
    }
}
