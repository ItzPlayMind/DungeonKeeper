using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ObjectiveStats : CharacterStats
{
    private Animator animator;
    private AnimationEventSender animationEventSender;

    protected override bool CanBeHitConstantly()
    {
        return false;
    }

    protected override void Start()
    {
        base.Start();
        animator = GetComponentInChildren<Animator>();
        animationEventSender = GetComponentInChildren<AnimationEventSender>();
        animationEventSender.OnAnimationEvent += (e) =>
        {
            if (e == AnimationEventSender.AnimationEvent.Hit)
                CanBeHit = true;
        };
    }

    protected override void Die(ulong damagerID)
    {
        base.Die(damagerID);
        GameManager.instance.Chat.AddMessage($"{damagerID} <color=red>killed</color> {gameObject.name}");
    }

    [ClientRpc]
    protected override void TakeDamageClientRPC(int damage, Vector2 knockback, ulong damagerID)
    {
        base.TakeDamageClientRPC(damage, knockback, damagerID);
        animator.SetTrigger("hit");
    }
}
