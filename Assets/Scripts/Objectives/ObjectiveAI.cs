using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class ObjectiveAI : NetworkBehaviour
{
    [SerializeField] private float attackTime = 2f;
    [SerializeField] private float detectionRange = 2f;
    [SerializeField] protected float attackRange = 3f;
    protected CharacterStats baseTarget = null;
    protected CharacterStats stats;
    protected CharacterStats target;

    private float attackTimer = 0f;

    public void SetBaseTarget(CharacterStats target)
    {
        baseTarget = target;
        this.target = baseTarget;
    }

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

    protected virtual void Update()
    {
        if (!IsServer) return;
        if(target == null && baseTarget != null) target = baseTarget;
        attackTimer -= Time.deltaTime;
        if (attackTimer < 0f)
        {
            if (target == baseTarget)
            {
                var collisions = Physics2D.OverlapCircleAll(transform.position, detectionRange);
                foreach (var item in collisions)
                {
                    if (gameObject.layer == item.gameObject.layer) continue;
                    var playerController = item.GetComponent<CharacterStats>();
                    if (playerController != null)
                    {
                        target = playerController;
                        attackTimer = attackTime;
                    }
                }
                if (target == baseTarget)
                    attackTimer = 0.1f;
            }
            else
            {
                if (Vector2.Distance(target.transform.position, transform.position) > attackRange)
                {
                    target = baseTarget;
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
