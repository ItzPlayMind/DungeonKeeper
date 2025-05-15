using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BlockSpecial : AbstractSpecial
{
    [SerializeField] private float knockBackForce = 35;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int counterDuration = 10;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int damageReductionIncrease = 10;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int blockingDuration = 5;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int blockingAmount = 30;
    private bool isBlocking = false;

    EffectManager effectManager;

    public override bool CanMoveWhileUsing()
    {
        return HasUpgradeUnlocked(1);
    }

    protected override void _Start()
    {
        if (!IsLocalPlayer) return;
        characterStats.stats.damageReduction.ConstraintValue += ChangeDamageReduction;
        characterStats.OnClientTakeDamage += (ulong damager, int damage) =>
        {
            if (isBlocking)
            {
                var enemy = NetworkManager.Singleton.SpawnManager.SpawnedObjects[damager].GetComponent<CharacterStats>();
                DealDamage(enemy,damage, enemy.GenerateKnockBack(enemy.transform, transform, knockBackForce));
                StartActive();
            }
        };
        characterStats.stats.damageReduction.ChangeValueAdd += (ref int value, int old) =>
        {
            if (HasUpgradeUnlocked(0))
            {
                value += damageReductionIncrease;
            }
        };
        characterStats.stats.speed.ConstraintValue += (ref int value, int old) =>
        {
            if (HasUpgradeUnlocked(1) && isBlocking)
            {
                value = 40;
            }
        };
        effectManager = GetComponent<EffectManager>();
        var playerAttack = GetComponent<PlayerAttack>();
        playerAttack.OnAttack += (ulong targetID, ulong user, ref int amount) =>
        {
            if (!IsActive) return;
            var target = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetID];
            if (target != null)
            {
                target.GetComponent<EffectManager>()?.AddEffect("stunned", counterDuration, 1, characterStats);
                StartCooldown();
            }
        };
    }
    protected override void _OnSpecialFinish(PlayerController controller)
    {
        isBlocking = false;
        if (!IsActive)
            StartCooldown();
        if(HasUpgradeUnlocked(2))
            effectManager.AddEffect("blocking", blockingDuration, blockingAmount, characterStats);
    }

    protected override void OnActiveOver()
    {
        StartCooldown();
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        isBlocking = true;
        Use();
    }

    private void ChangeDamageReduction(ref int newValue, int value)
    {
        if(isBlocking)
            newValue = 100;
    }
}
