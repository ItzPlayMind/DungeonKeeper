using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using static AnimationEventSender;

public abstract class PlayerAttack : NetworkBehaviour
{

    private AnimationEventSender animatorEvent;
    protected Rigidbody2D rb;
    protected Animator animator;
    public bool isAttacking { get; protected set; }
    protected PlayerStats stats;
    public delegate void ActionDelegate(ulong target, ulong user, ref int amount);
    public ActionDelegate OnAttack;
    public System.Action OnAttackPress;
    protected PlayerController controller;

    public void SetAttacking()
    {
        isAttacking = true;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer)
            return;
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        animatorEvent = animator.GetComponent<AnimationEventSender>();
        stats = GetComponent<PlayerStats>();
        stats.OnClientRespawn += () =>
        {
            isAttacking = false;
        };
        if (animatorEvent != null)
            animatorEvent.OnAnimationEvent += AnimationEventCallaback;
    }

    private void AnimationEventCallaback(AnimationEventSender.AnimationEvent animationEvent)
    {
        switch (animationEvent)
        {
            case AnimationEventSender.AnimationEvent.EndAttack:
                animator.ResetTrigger("attacking");
                OnAttackEnd();
                isAttacking = false;
                break;
            case AnimationEventSender.AnimationEvent.SelfKnockBack:
                OnSelfKnockback();
                break;
            default:
                break;
        }
    }


    protected abstract void OnAttackEnd();
    protected abstract void OnSelfKnockback();

    public virtual void OnTeamAssigned() {}

    public void Attack()
    {
        if (isAttacking)
            return;
        OnAttackTriggered();
        animator.SetTrigger("attacking");
        rb.velocity = Vector2.zero;
        OnAttackPress?.Invoke();
        isAttacking = true;
    }

    protected abstract void OnAttackTriggered();

    private void Update()
    {
        if (isAttacking)
            return;
        _Update();
    }

    protected abstract void _Update();
}
