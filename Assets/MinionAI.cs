using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class MinionAI : ObjectiveAI
{
    [SerializeField] private int cash;
    private NavMeshAgent agent;


    public override void OnNetworkSpawn()
    {
        stats = GetComponent<CharacterStats>();
        agent = GetComponent<NavMeshAgent>();
        stats.OnDeath += OnDeath;
        agent.updateUpAxis = false;
        agent.updateRotation = false;
        agent.speed = stats.stats.speed.Value / 10f;
        agent.stoppingDistance = attackRange;
    }

    protected override void OnDeath(ulong id)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].GetComponent<Inventory>()?.AddCash(cash);
        DespawnServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnServerRPC()
    {
        Destroy(gameObject);
    }

    protected override void Update()
    {
        base.Update();
        if(target != null)
            agent.SetDestination(target.transform.position);
        transform.localScale = new Vector2(Mathf.Sign(agent.velocity.x) * Mathf.Abs(transform.localScale.x), transform.localScale.y);
    }
}
