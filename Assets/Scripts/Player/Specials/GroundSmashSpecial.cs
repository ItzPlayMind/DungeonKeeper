using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class GroundSmashSpecial : KnockBackSpecial
{
    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")] private int valueIncrease = 20;
    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")] private int rageGainIncrease = 100;

    private Vector3 originalSize;

    protected override void _Start()
    {
        base._Start();
        originalSize = transform.localScale;
        if (!IsLocalPlayer) return;
        characterStats.stats.resource.ChangeValueAdd += (ref int value, int old) =>
        {
            if (HasUpgradeUnlocked(0))
                value += 100;
        };
        characterStats.stats.attackSpeed.ChangeValueAdd += (ref int value, int old) =>
        {
            if (HasUpgradeUnlocked(2) && Resource >= characterStats.stats.resource.Value)
                value += valueIncrease;
        };
        characterStats.stats.damageReduction.ChangeValueAdd += (ref int value, int old) =>
        {
            if (HasUpgradeUnlocked(2) && Resource >= characterStats.stats.resource.Value)
                value += valueIncrease;
        };
        characterStats.OnClientTakeDamage += (ulong damager, int damage) =>
        {
            if (HasUpgradeUnlocked(0)) {
                float increase = (damage / (float)characterStats.stats.health.Value) * 100 * 2;
                if (HasUpgradeUnlocked(1))
                    increase *= 1 + (rageGainIncrease / 100f);
                Resource += (int)(increase);
            }
        };
    }

    protected override void OnUpgradeUnlocked(int index)
    {
        base.OnUpgradeUnlocked(index);
        if(index == 0)
            UpdateResourceBar();
    }

    protected override void _Update()
    {
        base._Update();
        if (!IsLocalPlayer) return;
        if (HasUpgradeUnlocked(2))
        {
            if (Resource >= characterStats.stats.resource.Value)
                transform.localScale = originalSize * (1+valueIncrease/100f);
            else
                transform.localScale = originalSize;
        }
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        base._OnSpecialFinish(controller);
        if (HasUpgradeUnlocked(0)) {
            characterStats.Heal((int)((Resource/100f)*characterStats.stats.health.Value),characterStats);
            Resource = 0;
        }
    }
}
