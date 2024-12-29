using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExecuteSpecial : AbstractSpecial
{
    [SerializeField] private int baseExecuteThreshold = 10;
    [DescriptionCreator.DescriptionVariable("green")]
    private int executeThreshold
    {
        get => baseExecuteThreshold + (int)(characterStats.stats.health.Value * (5f / 1000f));
    }
    [SerializeField] private CollisionSender hitbox;
    [SerializeField] private int baseBleedAmount = 5;

    [DescriptionCreator.DescriptionVariable("green")]
    [SerializeField] private int bleedAmount { get => baseBleedAmount + (int)(characterStats.stats.health.Value * (5f / 1000f)); }
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int bleedDuration = 5;

    private bool executed = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsLocalPlayer) return;
        hitbox.onCollisionEnter += (GameObject collider) =>
        {
            if (collider.gameObject == gameObject) return;
            if (collider.gameObject.layer == gameObject.layer) return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats != null)
            {
                if (stats.Health <= stats.stats.health.Value * (executeThreshold / 100f) && stats is PlayerStats)
                {
                    stats.TakeDamage(10000, Vector2.zero, characterStats);
                    executed = true;
                }
                else
                {
                    stats.TakeDamage(Damage, Vector2.zero, characterStats);
                    stats.GetComponent<EffectManager>()?.AddEffect("bleed", bleedDuration, bleedAmount, stats);
                }
            }
        };
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        if (executed)
            StartActive();
        else
            StartCooldown();
    }

    protected override void OnActiveOver()
    {
        StartCooldown();
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        executed = false;
    }
}
