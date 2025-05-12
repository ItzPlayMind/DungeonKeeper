using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using static DescriptionCreator;

public class KnockBackSpecial : AbstractSpecial
{
    [SerializeField] private float knockBackForce = 55;
    [SerializeField] CollisionSender hitbox;
    [SerializeField] private string effectName = "slow";
    [DescriptionVariable("white")]
    [SerializeField] protected int amount = 50;
    [DescriptionVariable("white")]
    [SerializeField] protected int duration = 3;
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
                OnSpecialHit(stats);
            }
        };
    }

    protected virtual void OnSpecialHit(CharacterStats enemyStats)
    {
        DealDamage(enemyStats, Damage, enemyStats.GenerateKnockBack(enemyStats.transform, transform, knockBackForce));
        enemyStats.GetComponent<EffectManager>()?.AddEffect(effectName, duration, amount, characterStats);
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
