using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : CharacterStats
{
    [SerializeField] private Canvas playerUI;
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
        healthBar?.UpdateBar(Health / (float)stats.health.Value);
        animator.SetTrigger("hit");
    }
    protected override void Start()
    {
        base.Start();
        if (IsLocalPlayer)
        {
            healthBar = playerUI.transform.Find("Healthbar").GetComponent<UIBar>();
            healthBar.UpdateBar(1f);
            stats.OnValuesChange += () => healthBar.UpdateBar(Health/(float)stats.health.Value);
        }
        else
        {
            playerUI.gameObject.SetActive(false);
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
    }

    protected override void Die(ulong damagerID)
    {
        respawnTime = GameManager.instance.RESPAWN_TIME.Value;
        base.Die(damagerID);
        animator.SetBool("death", true);


    }

    protected override void Respawn()
    {
        base.Respawn();
        animator.SetBool("death", false);
        if(IsLocalPlayer)
            healthBar.UpdateBar(1);
    }

    [ClientRpc]
    protected override void HealClientRPC(int health)
    {
        base.HealClientRPC(health);
        if(IsLocalPlayer)
            healthBar.UpdateBar(Health / (float)stats.health.Value);
    }
}
