using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealHitSpecial : AbstractSpecial
{
    [SerializeField] CollisionSender hitbox;
    [SerializeField] private float healRadius = 5;
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
                stats.TakeDamage(Damage, stats.GenerateKnockBack(stats.transform, transform, 0));
            }
            var colliders = Physics2D.OverlapCircleAll(transform.position, healRadius);
            foreach (var item in colliders)
            {
                if (item.gameObject.layer != gameObject.layer)
                    continue;
                var itemStats = item.GetComponent<CharacterStats>();
                if(itemStats != null)
                {
                    itemStats.Heal(Damage);
                }
            }
        };
    }

    public override void OnSpecialFinish(PlayerController controller)
    {
        StartCooldown();
    }

    public override void OnSpecialPress(PlayerController controller)
    {
        Use();
    }
}
