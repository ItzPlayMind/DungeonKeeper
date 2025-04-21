using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class HealHitSpecial : AbstractSpecial
{
    [SerializeField] CollisionSender hitbox;
    [SerializeField] private float healRadius = 5;
    [DescriptionCreator.DescriptionVariable("green")] [SerializeField] private int conversionRate = 1;
    [DescriptionCreator.DescriptionVariable] [SerializeField] private int slowDuration = 3;
    [DescriptionCreator.DescriptionVariable] [SerializeField] private int slowAmount = 50;
    [DescriptionCreator.DescriptionVariable][SerializeField] private int slimyDuration = 10;
    [DescriptionCreator.DescriptionVariable][SerializeField] private int slimyAmount = 25;
    private PlayerController controller;
    protected override void _Start()
    {
        if (!IsLocalPlayer) return;
        controller = GetComponent<PlayerController>();
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
        hitbox.gameObject.layer = gameObject.layer;
        hitbox.onCollisionEnter += (GameObject collider, ref bool hit) =>
        {
            var target = collider.GetComponent<CharacterStats>(); 
            if (target == null) return;
            if (target.gameObject == gameObject) return;
            if (target.gameObject.layer == gameObject.layer)
                return;
            target.TakeDamage(Damage, Vector2.zero, characterStats);
            if(HasUpgradeUnlocked(1))
                target.GetComponent<EffectManager>()?.AddEffect("slow", slowDuration, slowAmount, characterStats);
            var colliders = Physics2D.OverlapCircleAll(transform.position, healRadius);
            foreach (var item in colliders)
            {
                if (item.gameObject.layer != gameObject.layer)
                    continue;
                var itemStats = item.GetComponent<CharacterStats>();
                if(itemStats != null)
                {
                    controller.Heal(itemStats, Damage);
                    if(HasUpgradeUnlocked(2))
                        controller.GetComponent<EffectManager>()?.AddEffect("slimy", slimyDuration, slimyAmount, characterStats);
                }
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
