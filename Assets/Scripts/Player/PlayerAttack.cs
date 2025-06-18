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
    private NetworkVariable<float> animationSpeed = new NetworkVariable<float>(1,NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public void SetAttacking()
    {
        isAttacking = true;
    }

    public override void OnNetworkSpawn()
    {
        animator = GetComponentInChildren<Animator>();
        if (!IsLocalPlayer)
            return;
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
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
                animationSpeed.Value = 1;
                isAttacking = false;
                break;
            case AnimationEventSender.AnimationEvent.SelfKnockBack:
                OnSelfKnockback();
                break;
            case AnimationEventSender.AnimationEvent.SpawnAttack:
                OnSpawnAttack();
                break;
            default:
                break;
        }
    }


    protected abstract void OnAttackEnd();
    protected virtual void OnSelfKnockback() { }
    protected virtual void OnSpawnAttack() { }

    public virtual void OnTeamAssigned() {}

    public void Attack()
    {
        if (!enabled) return;
        if (isAttacking)
            return;
        animationSpeed.Value = 1 + (stats.stats.attackSpeed.Value/100f);
        OnAttackTriggered();
        animator.SetTrigger("attacking");
        rb.velocity = Vector2.zero;
        OnAttackPress?.Invoke();
        isAttacking = true;
    }

    protected abstract void OnAttackTriggered();

    private void Update()
    {
        animator.speed = animationSpeed.Value;
        if (isAttacking)
            return;
        _Update();
    }

    protected abstract void _Update();
}
