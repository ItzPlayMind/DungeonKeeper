using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HealDummy : NetworkBehaviour
{
    CharacterStats stats;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        stats = GetComponent<CharacterStats>();
    }

    float timer = 1f;
    private void Update()
    {
        if (!IsServer) return;
        if (timer <= 0)
        {
            timer = 1f;
            if(stats.Health > stats.stats.health.Value / 2f)
                stats.TakeDamage(200,Vector2.zero, stats);
        }
        else
            timer -= Time.deltaTime;
    }
}
