using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class HealHitSpecial : AbstractSpecial
{
    [SerializeField] CollisionSender hitbox;
    [SerializeField] private float healRadius = 5;
    private PlayerController controller;
    protected override void _Start()
    {
        if (!IsLocalPlayer) return;
        controller = GetComponent<PlayerController>();
        hitbox.gameObject.layer = gameObject.layer;
        hitbox.onCollisionEnter += (GameObject collider) =>
        {
            controller.HitTarget(collider.GetComponent<CharacterStats>(), Damage, 0);
            var colliders = Physics2D.OverlapCircleAll(transform.position, healRadius);
            foreach (var item in colliders)
            {
                if (item.gameObject.layer != gameObject.layer)
                    continue;
                if (item.gameObject == gameObject) continue;
                var itemStats = item.GetComponent<CharacterStats>();
                if(itemStats != null)
                {
                    itemStats.Heal(Damage);
                }
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
