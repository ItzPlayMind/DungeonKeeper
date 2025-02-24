using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameSwordSpecial : KnockBackSpecial
{
    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")] private int damageIncrease = 5;
    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")] private int healAmount = 10;
    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")] private int cooldownReduce = 2;

    public override float Cooldown => HasUpgradeUnlocked(0) ? base.Cooldown - cooldownReduce : base.Cooldown;

    protected override void OnSpecialHit(CharacterStats enemyStats)
    {
        base.OnSpecialHit(enemyStats);
        if(HasUpgradeUnlocked(1))
            characterStats.Heal((int)(Damage * (healAmount / 100f)));
    }

    protected override void _Start()
    {
        base._Start();
        if (!IsLocalPlayer) return;
        GetComponent<PlayerAttack>().OnAttack += (ulong target, ulong user, ref int amount) =>
        {
            var manager = Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<EffectManager>();
            if(manager != null)
            {
                if (manager.HasEffect("flames"))
                {
                    float flameAmount = 0;
                    float flameDuration = 0;
                    manager.GetEffectStats("flames", out flameDuration, out flameAmount);
                    manager.AddEffect("flames", duration, (int)flameAmount + (HasUpgradeUnlocked(2) ? damageIncrease : 0), characterStats);
                }
            }
        };
    }
}
