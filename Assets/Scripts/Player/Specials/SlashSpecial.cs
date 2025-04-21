using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DescriptionCreator;

public class SlashSpecial : ComboSpecial
{
    [DescriptionVariable("white")]
    [SerializeField] private int windyDuration = 1;

    [DescriptionVariable("white")]
    [SerializeField] private int windyAmount = 10;

    [DescriptionVariable("white")]
    [SerializeField] private int hpAmount = 10;

    [DescriptionVariable("white")]
    [SerializeField] private int stunDuration = 2;

    private EffectManager effectManager;

    protected override void OnAttackHit(int index,int damage, CharacterStats target)
    {
        Debug.Log(index);
        if (HasUpgradeUnlocked(0))
            effectManager.AddEffect("windy", windyDuration, windyAmount, characterStats);
        if (HasUpgradeUnlocked(1))
            characterStats.Heal((int)(damage * (hpAmount / 100f)));
        if (index == 2 && HasUpgradeUnlocked(2))
            target.GetComponent<EffectManager>()?.AddEffect("stunned", stunDuration, 1, characterStats);
    }

    protected override void _Start()
    {
        base._Start();
        if (!IsLocalPlayer) return;
        effectManager = GetComponent<EffectManager>();
    }

    protected override void OnAttackMiss(int index)
    {

    }
}
