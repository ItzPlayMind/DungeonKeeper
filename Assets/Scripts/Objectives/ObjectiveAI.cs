using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

public abstract class ObjectiveAI : NetworkBehaviour
{
    [SerializeField] protected float attackTime = 2f;
    [SerializeField] protected float detectionRange = 2f;
    [SerializeField] protected float attackRange = 3f;
    protected CharacterStats baseTarget = null;
    protected CharacterStats stats;
    protected CharacterStats target;

    protected float attackTimer = 0f;

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

    private CharacterStats GetTargetFromCollisions(Collider2D[] collider)
    {
        List<CharacterStats > result = new List<CharacterStats>();
        foreach (var item in collider)
        {
            if (item.gameObject.layer == gameObject.layer) continue;
            var stats = item.GetComponent<CharacterStats>();
            if(stats != null)
                result.Add(stats);
        }
        if(result.Count <= 0) return null;
        result.Sort(SortTargets);
        return result[0];
    }

    protected virtual int SortTargets(CharacterStats stat1, CharacterStats stat2)
    {
        return 0;
    }

    protected virtual void Update()
    {
        if (!IsServer) return;
        if (stats.IsDead) return;
        if(target == null && baseTarget != null) target = baseTarget;
        attackTimer -= Time.deltaTime;
        if (attackTimer < 0f)
        {
            if (target == baseTarget)
            {
                var collisions = Physics2D.OverlapCircleAll(transform.position, detectionRange);
                target = GetTargetFromCollisions(collisions);
                if(target != null)
                    attackTimer = attackTime;
                if (target == null)
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
