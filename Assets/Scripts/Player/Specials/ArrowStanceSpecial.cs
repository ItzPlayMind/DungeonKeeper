using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;

public class ArrowStanceSpecial : AbstractSpecial
{
    [SerializeField] private NetworkObject enhancedArrowPrefab;
    [SerializeField] private float enhancedArrowSpeed = 15;
    [DescriptionCreator.DescriptionVariable]
    [SerializeField] private int slowAmount = 25;
    [DescriptionCreator.DescriptionVariable]
    [SerializeField] private int slowDuration = 1;
    private Animator animator;
    private PlayerController controller;
    private bool isInStance = false;

    private PlayerProjectileAttack attack;
    private float normalArrowSpeed;
    private NetworkObject normalArrowPrefab;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        controller = GetComponent<PlayerController>();
        animator = GetComponentInChildren<Animator>();
        attack = GetComponent<PlayerProjectileAttack>();
        normalArrowPrefab = attack.projectile;
        normalArrowSpeed = attack.projectileSpeed;
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        if (!isInStance)
        {
            animator.SetInteger("attack", 1);
            attack.OnAttack += AddAdditionalDamage;
            attack.SetPiercing(true);
            StartActive();
            isInStance = true;
            SetAttackOptionsServerRpc(true);
        }
        else
        {
            SwitchToDefaultStance(); 
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetAttackOptionsServerRpc(bool enhanced)
    {
        attack.projectile = enhanced ? enhancedArrowPrefab : normalArrowPrefab;
        attack.projectileSpeed = enhanced ? enhancedArrowSpeed : normalArrowSpeed;
    }

    private void AddAdditionalDamage(ulong target, ulong user, ref int amount)
    {
        amount += Damage;
        var effectManager = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<EffectManager>();
        if(effectManager != null)
            effectManager.AddEffect("slow", slowDuration, slowAmount, characterStats);
    }

    protected override void OnActiveOver()
    {
        if(IsLocalPlayer) { 
            SwitchToDefaultStance();
        }
    }

    private void SwitchToDefaultStance()
    {
        animator.SetInteger("attack", 0);
        controller.Attack.OnAttack -= AddAdditionalDamage; 
        attack.SetPiercing(false);
        StartCooldown();
        isInStance = false;
        SetAttackOptionsServerRpc(false);
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
    }
}