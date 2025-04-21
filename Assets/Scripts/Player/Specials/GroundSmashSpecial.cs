using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class GroundSmashSpecial : KnockBackSpecial
{
    [SerializeField] private int hpToMaxResource = 50;
    [SerializeField] private int damageToResource = 25; 
    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")] private int hpToDamage = 2;
    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")] private int rageGainIncrease = 100;

    protected override void _Start()
    {
        base._Start();
        if (!IsLocalPlayer) return;
        characterStats.stats.resource.ChangeValueAdd += (ref int value, int old) =>
        {
            if (HasUpgradeUnlocked(0))
                value += (int)(characterStats.stats.health.Value * (hpToMaxResource / 100f));
        };
        characterStats.OnClientTakeDamage += (ulong damager, int damage) =>
        {
            if (HasUpgradeUnlocked(0)) {
                float increase = damageToResource;
                if (HasUpgradeUnlocked(1))
                    increase *= 1 + (rageGainIncrease / 100f);
                Resource += (int)(damage * (increase / 100f));
            }
        };
        characterStats.stats.damage.ChangeValueAdd += (ref int value, int old) =>
        {
            if (HasUpgradeUnlocked(2) && Resource >= characterStats.stats.resource.Value)
            {
                value += (int)(characterStats.stats.health.Value * (hpToDamage / 100f));
            }
        };
    }

    protected override void OnUpgradeUnlocked(int index)
    {
        base.OnUpgradeUnlocked(index);
        if(index == 0)
            UpdateResourceBar();
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        base._OnSpecialFinish(controller);
        if (HasUpgradeUnlocked(0)) {
            characterStats.Heal(Resource);
            Resource = 0;
        }
    }
}
