using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using static DescriptionCreator;

public class OnHitDamageAndHealSpecial : AbstractSpecial
{
    [DescriptionVariable]
    public int ResourceDamage { get => (int)(Damage * (Resource / (float)characterStats.stats.resource.BaseValue)); }
    [DescriptionVariable]
    public int HealAmount { get => (int)((characterStats.stats.health.Value-characterStats.Health) * 0.1f) + ResourceDamage * 2; }
    [DescriptionVariable("white")]
    public int RageOverTime = 5;
    [DescriptionVariable("white")]
    public int FrenzyDuration = 5;
    [DescriptionVariable("white")]
    public int FrenzyAmount = 30;
    [DescriptionVariable("white")]
    public int dRIncrease = 10;

    protected override bool HasResource()
    {
        return Resource > 0;
    }

    EffectManager effectManager;

    protected override void _Start()
    {
        base._Start();
        characterStats.stats.damageReduction.ChangeValueAdd += (ref int current, int old) =>
        {
            if (HasUpgradeUnlocked(0) && Resource < characterStats.stats.resource.BaseValue)
            {
                current += dRIncrease;
            }
        };
        if (!IsLocalPlayer) return;
        Resource = 0;
        UpdateResourceBar();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsLocalPlayer) return;
        GetComponent<PlayerAttack>().OnAttack += (ulong target, ulong damager, ref int damage) =>
        {
            if (!OnCooldown)
            {
                damage += ResourceDamage;
                int old = Resource;
                Resource += 10;
                if (old < Resource && Resource == 100 && HasUpgradeUnlocked(2))
                {
                    effectManager.AddEffect("frenzy", FrenzyDuration, FrenzyAmount, characterStats);
                }
            }
        };
        effectManager = GetComponent<EffectManager>();
    }

    private float timer = 1f;
    protected override void _Update()
    {
        base._Update();
        if (HasUpgradeUnlocked(1))
        {
            if (timer > 0f)
                timer -= Time.deltaTime;
            else
            {
                int old = Resource;
                Resource += RageOverTime;
                timer = 1f;
                if(old < Resource && Resource == 100 && HasUpgradeUnlocked(2))
                {
                    effectManager.AddEffect("frenzy", FrenzyDuration, FrenzyAmount, characterStats);
                }
            }

        }
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        characterStats.Heal(HealAmount);
        Resource = 0;
        StartCooldown();
    }

    protected override void _OnSpecialPress(PlayerController controller) {}
}
