using Cinemachine.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Turret : ObjectiveAI
{
    private NetworkAnimator animator;
    private AnimationEventSender eventSender;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        animator = GetComponentInChildren<NetworkAnimator>();
        eventSender = GetComponentInChildren<AnimationEventSender>();
        eventSender.OnAnimationEvent += (e) =>
        {
            if (e == AnimationEventSender.AnimationEvent.EndAttack)
                canAttack = true;
            if (e == AnimationEventSender.AnimationEvent.Special)
                if(target != null)
                    target?.TakeDamage(stats.stats.damage.Value, Vector2.zero, stats);
        };
    }

    protected override void OnDeath(ulong id)
    {
        GameManager.instance.AddCashToTeamFromPlayer(id, GameManager.instance.GOLD_PER_TURRET);
        Destroy(gameObject);
    }

    protected override int SortTargets(CharacterStats stat1, CharacterStats stat2)
    {
        if(stat1 is ObjectiveStats)
            return -1;
        return 0;
    }
    private bool canAttack = true;
    protected override void Update()
    {
        if (!IsServer) return;
        if (stats.IsDead) return;
        if (target == null && baseTarget != null) target = baseTarget;
        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        if (target != null && Vector2.Distance(target.transform.position, transform.position) > attackRange)
            target = null;
        if (canAttack && attackTimer <= 0)
        {
            if (target == null)
            {
                var collisions = Physics2D.OverlapCircleAll(transform.position, detectionRange);
                target = GetTargetFromCollisions(collisions);
            }
            if (target != null && !target.IsDead)
                Attack();
            else
                attackTimer = 0.1f;
        }
    }
    public void Attack()
    {
        canAttack = false;
        animator.SetTrigger("Attack");
    }

}
