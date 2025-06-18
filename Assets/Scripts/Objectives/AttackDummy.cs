using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AttackDummy : NetworkBehaviour
{
    [SerializeField]
    private float attackRange = 0.75f;

    private CharacterStats target;
    private AnimationEventSender sender;
    private CharacterStats stats;
    private bool isAttacking = false;
    private Animator animator;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        target = null;
        sender = GetComponentInChildren<AnimationEventSender>();
        animator = GetComponentInChildren<Animator>();
        sender.OnAnimationEvent += OnAnimationEvent;
        stats = GetComponent<CharacterStats>();
        stats.OnServerTakeDamage += (ulong damager, ref int damage) =>
        {
            var networkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[damager];
            if(networkObject != null)
                target = networkObject.GetComponent<CharacterStats>();
        };
    }

    private void OnAnimationEvent(AnimationEventSender.AnimationEvent e)
    {
        if (e == AnimationEventSender.AnimationEvent.EndAttack)
        {
            if (target != null)
            {
                if (Vector2.Distance(transform.position, target.transform.position) < attackRange)
                {
                    target.TakeDamage(10, Vector2.zero, stats);
                }
                else
                    target = null;
            }
            isAttacking = false;
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        if(target != null && !isAttacking)
        {
            animator.SetTrigger("attack");
            isAttacking = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
