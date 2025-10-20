using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class HealHitSpecial : AbstractSpecial
{
    [SerializeField] CollisionSender hitbox;
    [SerializeField] private float healRadius = 5;
    [DescriptionCreator.DescriptionVariable]
    private int HealAmount { get => Damage * 2; }
    [DescriptionCreator.DescriptionVariable("green")] [SerializeField] private int conversionRate = 1;
    [DescriptionCreator.DescriptionVariable] [SerializeField] private int slowDuration = 3;
    [DescriptionCreator.DescriptionVariable] [SerializeField] private int slowAmount = 50;
    [DescriptionCreator.DescriptionVariable][SerializeField] private int slimyDuration = 10;
    [DescriptionCreator.DescriptionVariable][SerializeField] private int slimyAmount = 25;
    protected override void _Start()
    {
        if (!IsLocalPlayer) return;
        GetComponent<PlayerAttack>().OnAttack += (ulong target, ulong user, ref int amount) =>
        {
            if(Resource < characterStats.stats.resource.Value)
                Resource++;
        };
        characterStats.stats.specialDamage.ChangeValueAdd += (ref int value, int old) =>
        {
            if (HasUpgradeUnlocked(0))
            {
                value += (int)((characterStats.stats.health.Value) * (conversionRate / 100f));
            }
        };
        hitbox.onCollisionEnter += (GameObject collider, ref bool hit) =>
        {
            var target = collider.GetComponent<CharacterStats>(); 
            if (target == null) return;
            if (target.gameObject == gameObject) return;
            if (controller.TeamController.HasSameTeam(target.gameObject))
            {
                Debug.Log("SAME TEAM");
                controller.Heal(target, HealAmount);
                if (HasUpgradeUnlocked(2))
                    target.GetComponent<EffectManager>()?.AddEffect("slimy", slimyDuration, slimyAmount, characterStats);
            }
            else
            {
                DealDamage(target, Damage, Vector2.zero);
                if (HasUpgradeUnlocked(1))
                    target.GetComponent<EffectManager>()?.AddEffect("slow", slowDuration, slowAmount, characterStats);
            }
        };
    }


    protected override bool HasResource()
    {
        return Resource >= 1;
    }

    protected override void RemoveResource()
    {
        Resource -= 1;
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        StartCooldown();
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
    }
}
