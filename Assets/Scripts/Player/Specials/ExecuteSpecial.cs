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
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int dRIncrease = 10;
    [SerializeField] private float scaleIncrease = 1.5f;

    private bool executed = false;
    private PlayerAttack attack;

    protected override void _Start()
    {
        base._Start();
        characterStats.stats.damageReduction.ChangeValueAdd += (ref int current, int old) =>
        {
            if (HasUpgradeUnlocked(0))
            {
                current += dRIncrease;
            }
        };
        if (!IsLocalPlayer) return;
        attack = GetComponent<PlayerAttack>();
        attack.OnAttack += (ulong target, ulong damager, ref int amount) =>
        {
            if (!HasUpgradeUnlocked(2)) return;
            var targetObject = Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects[target];
            var effectManager = targetObject.GetComponent<EffectManager>();
            var stats = targetObject.GetComponent<CharacterStats>();
            if (effectManager != null && stats != null)
            {
                if (effectManager.HasEffect("bleed") && stats.Health <= stats.stats.health.Value * (executeThreshold / 100f))
                {
                    amount = 10000;
                }
            }
        };
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsLocalPlayer) return;
        hitbox.onCollisionEnter += (GameObject collider, ref bool hit) =>
        {
            if (collider.gameObject == gameObject) return;
            if (collider.gameObject.layer == gameObject.layer) return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats != null)
            {
                if (stats.Health <= stats.stats.health.Value * (executeThreshold / 100f) && stats is PlayerStats)
                {
                    DealDamage(stats, 10000, Vector2.zero);
                    executed = true;
                }
                else
                {
                    DealDamage(stats, Damage, Vector2.zero);
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

    protected override void OnUpgradeUnlocked(int index)
    {
        if (!IsLocalPlayer) return;
        if(index == 1)
        {
            transform.localScale *= scaleIncrease;
            GetComponent<Rigidbody2D>().mass *= scaleIncrease;
        }
    }
}
