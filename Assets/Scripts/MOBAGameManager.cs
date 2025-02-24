using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MOBAGameManager : GameManager
{
    protected override void OnPlayerSpawned(NetworkObject player)
    {
        if (!IsServer) return;
        player.GetComponent<CharacterStats>().OnServerDeath += (ulong killer) =>
        {
            NetworkManager.Singleton.SpawnManager.SpawnedObjects[killer]?.GetComponent<AbstractSpecial>()?.UnlockUpgrade(0);
        };
    }
}
