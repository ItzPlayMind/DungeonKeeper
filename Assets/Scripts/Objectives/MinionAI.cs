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
        if (!IsServer) return;
        stats = GetComponent<CharacterStats>();
        agent = GetComponent<NavMeshAgent>();
        stats.OnServerDeath += OnDeath;
        agent.updateUpAxis = false;
        agent.updateRotation = false;
        agent.speed = stats.stats.speed.Value / 10f;
        agent.stoppingDistance = attackRange;
    }

    protected override void OnDeath(ulong id)
    {
        GameManager.instance.AddCashToTeamFromPlayer(id,cash);
        Destroy(gameObject);
    }

    protected override void Update()
    {
        base.Update();
        if (!IsServer) return;
        if (attackTimer <= 0)
            if (Vector2.Distance(baseTarget.transform.position, transform.position) <= attackRange)
            {
                target.TakeDamage(stats.stats.damage.Value, Vector2.zero, stats);
                attackTimer = attackTime;
            }
        if (target != null)
            agent.SetDestination(target.transform.position);
        if (agent.velocity != Vector3.zero)
            transform.localScale = new Vector2(Mathf.Sign(agent.velocity.x) * Mathf.Abs(transform.localScale.x), transform.localScale.y);
    }
}
