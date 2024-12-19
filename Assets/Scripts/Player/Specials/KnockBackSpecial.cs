using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockBackSpecial : AbstractSpecial
{
    [SerializeField] private float knockBackForce = 55;
    [SerializeField] CollisionSender hitbox;
    protected override void _Start()
    {
        if (!IsLocalPlayer) return;
        hitbox.gameObject.layer = gameObject.layer;
        hitbox.onCollisionEnter += (GameObject collider) =>
        {
            if (collider == gameObject) return;
            if (collider.layer == gameObject.layer)
                return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats != null)
            {
                stats.TakeDamage(Damage, stats.GenerateKnockBack(stats.transform, transform, knockBackForce), characterStats);
                stats.GetComponent<EffectManager>().AddEffect("slow", 3, 50,characterStats);
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
