using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfHealHitSpecial : AbstractSpecial
{
    [SerializeField] CollisionSender hitbox;
    [SerializeField] private float maxHealthPerc = 0.02f;
    [SerializeField] private int onHitSecondsReduction = 5;

    private List<ulong> hits = new List<ulong>();

    protected override void _Start()
    {
        if (!IsLocalPlayer) return;
        hitbox.gameObject.layer = gameObject.layer;
        hitbox.onCollisionEnter += (GameObject collider) =>
        {
            if (collider.gameObject.layer == gameObject.layer) return;
            if (collider.gameObject == gameObject) return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats == null) return;
            if(hits.Contains(stats.NetworkObjectId)) return;
            hits.Add(stats.NetworkObjectId);
            stats.TakeDamage(Damage, Vector2.zero, characterStats);
            characterStats.Heal((int)(characterStats.stats.health.Value * maxHealthPerc));
            ReduceCooldown(onHitSecondsReduction);
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
