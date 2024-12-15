using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;

public class Nexus : ObjectiveAI
{
    [SerializeField] private List<ObjectiveAI> otherObjectives = new List<ObjectiveAI>();
    private NetworkAnimator animator;
    private AnimationEventSender eventSender;

    public System.Action OnMinionSpawnEvent;

    public void AddPreviousObjective(ObjectiveAI objective) => otherObjectives.Add(objective);

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        animator = GetComponentInChildren<NetworkAnimator>();
        stats = GetComponent<CharacterStats>();
        eventSender = GetComponentInChildren<AnimationEventSender>();
        eventSender.OnAnimationEvent += (e) =>
        {
            if (e == AnimationEventSender.AnimationEvent.EndAttack)
                canAttack = true;
            if(e == AnimationEventSender.AnimationEvent.Special)
                target.TakeDamage(stats.stats.damage.Value, Vector2.zero, stats);
            if (e == AnimationEventSender.AnimationEvent.SelfKnockBack)
                OnMinionSpawnEvent?.Invoke();
        };
        stats.stats.damageReduction.ChangeValue += (ref float value, float old) =>
        {
            if (otherObjectives.Any(x => x != null)) value = 100;
        };
        stats.OnServerDeath += OnDeath;
    }

    protected override void OnDeath(ulong id)
    {
        Destroy();
        var team = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].gameObject.layer;
        GameManager.instance.Win(team);
    }

    protected override int SortTargets(CharacterStats stat1, CharacterStats stat2)
    {
        if (stat1 is ObjectiveStats)
            return -1;
        return 0;
    }

    private bool canAttack = true;

    protected override void Update()
    {
        if (!IsServer) return;
        if (stats.IsDead) return;
        if (target == null && baseTarget != null) target = baseTarget;
        if(attackTimer > 0)
            attackTimer -= Time.deltaTime;
        if (canAttack && attackTimer <= 0)
        {
            var collisions = Physics2D.OverlapCircleAll(transform.position, detectionRange);
            target = GetTargetFromCollisions(collisions);
            if (target != null && !target.IsDead)
                Attack();
            else
                attackTimer = 0.1f;
        }
    }
    public void SpawnMinions()
    {
        animator.SetTrigger("Spawn");
    }

    public void Attack()
    {
        canAttack = false;
        animator.SetTrigger("Attack");
    }

    public void Destroy()
    {
        animator.SetTrigger("Destroy");
    }
}
