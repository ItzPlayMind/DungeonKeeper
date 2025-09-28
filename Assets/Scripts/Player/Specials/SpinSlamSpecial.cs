using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DescriptionCreator;

public class SpinSlamSpecial : ComboSpecial
{
    [DescriptionVariable("white")]
    [SerializeField] private int specialDamageIncrease = 15;

    [DescriptionVariable("white")]
    [SerializeField] private int MissCooldown = 5;

    [DescriptionVariable("white")]
    [SerializeField] private int damageIncrease = 5;

    private int stack;

    public override int Damage => base.Damage+(stack*damageIncrease);

    protected override void OnAttackHit(int index, int damage, CharacterStats target)
    {
        if (!HasUpgradeUnlocked(2)) return;
        stack++;
        UpdateAmountText(stack.ToString());
    }

    protected override void _Start()
    {
        base._Start();
        if (!IsLocalPlayer) return;
        characterStats.stats.specialDamage.ChangeValueAdd += (ref int current, int old) =>
        {
            if (HasUpgradeUnlocked(0) && IsActive)
            {
                current += specialDamageIncrease;
            }
        };
    }

    protected override void OnAttackMiss(int index)
    {
        if(index == 0 && HasUpgradeUnlocked(1))
        {
            StartCooldown(MissCooldown);
        }
    }

}
