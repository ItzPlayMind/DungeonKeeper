using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : CharacterStats
{
    [SerializeField] private Canvas playerUI;
    [SerializeField] private Canvas otherPlayerUI;
    private Canvas ui;
    private UIBar healthBar;
    private Animator animator;
    private AnimationEventSender animatorEvent;

    protected override bool CanBeHitConstantly()
    {
        return false;
    }

    [ClientRpc]
    protected override void TakeDamageClientRPC(int damage, Vector2 knockback, ulong damagerID)
    {
        base.TakeDamageClientRPC(damage, knockback, damagerID);
        healthBar.UpdateBar(Health / (float)stats.health.Value);
        animator.SetTrigger("hit");
    }
    protected override void Start()
    {
        base.Start();
        if (IsLocalPlayer)
        {
            Destroy(otherPlayerUI.gameObject);
            ui = playerUI;
        }
        else
        {
            Destroy(playerUI.gameObject);
            ui = otherPlayerUI;
        }
        animator = GetComponentInChildren<Animator>();
        animatorEvent = animator.GetComponent<AnimationEventSender>();
        animatorEvent.OnAnimationEvent += (AnimationEventSender.AnimationEvent e) =>
        {
            if (e == AnimationEventSender.AnimationEvent.Hit)
            {
                CanBeHit = true;
            }
        };
        healthBar = ui.transform.Find("Healthbar").GetComponent<UIBar>();
        healthBar.UpdateBar(1f);
    }

    protected override void Die(ulong damagerID)
    {
        base.Die(damagerID);
        animator.SetBool("death", true);
    }

    protected override void Respawn()
    {
        base.Respawn();
        animator.SetBool("death", false);
        healthBar.UpdateBar(1);
    }

    [ClientRpc]
    protected override void HealClientRPC(int health)
    {
        base.HealClientRPC(health);
        healthBar.UpdateBar(Health / (float)stats.health.Value);
    }
}
