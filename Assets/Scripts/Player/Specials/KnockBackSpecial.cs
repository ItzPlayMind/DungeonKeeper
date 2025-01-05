using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DescriptionCreator;

public class KnockBackSpecial : AbstractSpecial
{
    [SerializeField] private float knockBackForce = 55;
    [SerializeField] CollisionSender hitbox;
    [DescriptionVariable]
    [SerializeField] private int slowAmount = 50;
    [DescriptionVariable]
    [SerializeField] private int slowDuration = 3;
    protected override void _Start()
    {
        if (!IsLocalPlayer) return;
        hitbox.gameObject.layer = gameObject.layer;
        hitbox.onCollisionEnter += (GameObject collider, ref bool hit) =>
        {
            if (collider == gameObject) return;
            if (collider.layer == gameObject.layer)
                return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats != null)
            {
                stats.TakeDamage(Damage, stats.GenerateKnockBack(stats.transform, transform, knockBackForce), characterStats);
                stats.GetComponent<EffectManager>()?.AddEffect("slow", slowDuration, slowAmount,characterStats);
            }
        };
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
