using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfHealHitSpecial : AbstractSpecial
{
    [SerializeField] CollisionSender hitbox;
    [DescriptionCreator.DescriptionVariable("green")]
    [SerializeField] private float maxHealthPerc = 2f;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int onHitSecondsReduce = 5;

    private List<ulong> hits = new List<ulong>();
    private PlayerController controller;

    protected override void _Start()
    {
        if (!IsLocalPlayer) return;
        hitbox.gameObject.layer = gameObject.layer;
        controller = GetComponent<PlayerController>();
        hitbox.onCollisionEnter += (GameObject collider, ref bool hit) =>
        {
            if (collider.gameObject.layer == gameObject.layer) return;
            if (collider.gameObject == gameObject) return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats == null) return;
            if(hits.Contains(stats.NetworkObjectId)) return;
            hits.Add(stats.NetworkObjectId);
            stats.TakeDamage(Damage, Vector2.zero, characterStats);
            controller.Heal(characterStats, (int)(characterStats.stats.health.Value * (maxHealthPerc/100f)));
            ReduceCooldown(onHitSecondsReduce);
        };
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        hits.Clear();
        Use();
        StartCooldown();
    }
}
