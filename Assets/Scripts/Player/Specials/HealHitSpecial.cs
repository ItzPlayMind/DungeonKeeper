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
            var target = collider.GetComponent<CharacterStats>(); 
            if (target == null) return;
            if (target.gameObject == gameObject) return;
            if (target.gameObject.layer == gameObject.layer)
                return;
            target.TakeDamage(Damage, Vector2.zero, characterStats);
            var colliders = Physics2D.OverlapCircleAll(transform.position, healRadius);
            foreach (var item in colliders)
            {
                if (item.gameObject.layer != gameObject.layer)
                    continue;
                if (item.gameObject == gameObject) continue;
                var itemStats = item.GetComponent<CharacterStats>();
                if(itemStats != null)
                {
                    controller.Heal(itemStats, Damage);
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
