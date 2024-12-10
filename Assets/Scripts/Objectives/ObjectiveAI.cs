using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class ObjectiveAI : NetworkBehaviour
{
    [SerializeField] private float attackTime = 2f;
    [SerializeField] private float attackRange = 3f;
    protected CharacterStats stats;
    protected PlayerStats target;

    private float attackTimer = 0f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        stats = GetComponent<CharacterStats>();
        stats.stats.damageReduction.ChangeValue += (ref float value, float _) =>
        {
            if (target == null) value = 90;
            else value = 0;
        };
        stats.OnDeath += OnDeath;
    }

    protected abstract void OnDeath(ulong id);

    private void Update()
    {
        if (!IsServer) return;
        attackTimer -= Time.deltaTime;
        if (attackTimer < 0f)
        {
            if (target == null)
            {
                var collisions = Physics2D.OverlapCircleAll(transform.position, attackRange);
                foreach (var item in collisions)
                {
                    if (gameObject.layer == item.gameObject.layer) continue;
                    var playerController = item.GetComponent<PlayerStats>();
                    if (playerController != null)
                    {
                        target = playerController;
                        attackTimer = attackTime;
                    }
                }
                if (target == null)
                    attackTimer = 0.1f;
            }
            else
            {
                if (Vector2.Distance(target.transform.position, transform.position) > attackRange)
                {
                    target = null;
                }
                else
                {
                    target.TakeDamage(stats.stats.damage.Value, Vector2.zero, stats);
                    attackTimer = attackTime;
                }
            }
        }
    }
}
