using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using static DescriptionCreator;

public class OnHitDamageAndHealSpecial : AbstractSpecial
{
    [DescriptionVariable]
    public int ResourceDamage { get => (int)(Damage * (Resource / (float)characterStats.stats.resource.Value)); }
    [DescriptionVariable]
    public int HealAmount { get => (int)((characterStats.stats.health.Value-characterStats.Health) * 0.1f) + ResourceDamage * 2; }

    protected override bool HasResource()
    {
        return Resource > 0;
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
                Resource += 10;
            }
        };
        Resource = 0;
        UpdateResourceBar();
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        characterStats.Heal(HealAmount);
        Resource = 0;
        StartCooldown();
    }

    protected override void _OnSpecialPress(PlayerController controller) {}
}
