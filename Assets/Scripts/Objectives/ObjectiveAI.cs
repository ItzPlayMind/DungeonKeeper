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

    public void SetTarget(CharacterStats target)
    {
        if (Vector2.Distance(target.transform.position, transform.position) <= attackRange)
        {
            Debug.Log("SET TARGET TO " + target.name);
            this.target = target;
        }
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
        stats.OnServerDeath += OnDeath;
    }

    protected abstract void OnDeath(ulong id);

    protected CharacterStats GetTargetFromCollisions(Collider2D[] collider)
    {
        List<CharacterStats > result = new List<CharacterStats>();
        foreach (var item in collider)
        {
            if (item.gameObject.layer == gameObject.layer) continue;
            var stats = item.GetComponent<CharacterStats>();
            if(stats != null && !stats.IsDead)
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
        if(GameManager.instance.GameOver) return;
        if(target == null && baseTarget != null) target = baseTarget;
        attackTimer -= Time.deltaTime;
        if (attackTimer < 0f)
        {
            if (target == baseTarget)
            {
                var collisions = Physics2D.OverlapCircleAll(transform.position, detectionRange);
                target = GetTargetFromCollisions(collisions);
                if(target != null && !target.IsDead)
                    attackTimer = attackTime;
                if (target == null)
                    attackTimer = 0.1f;
            }
            else
            {
                if (Vector2.Distance(target.transform.position, transform.position) > attackRange || target.IsDead)
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
