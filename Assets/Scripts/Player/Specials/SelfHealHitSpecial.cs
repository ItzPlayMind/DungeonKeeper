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

    [DescriptionCreator.DescriptionVariable("green")]
    [SerializeField] private int maxHealhIncrease = 300;
    [DescriptionCreator.DescriptionVariable("green")]
    [SerializeField] private float playerMaxHealthPerc = 5f;
    [DescriptionCreator.DescriptionVariable("green")]
    [SerializeField] private int stackHealthIncrease = 5;

    private List<ulong> hits = new List<ulong>();
    private PlayerController controller;

    private int stacks = 0;
    private int stacksPerHit = 0;

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
            if (stats is PlayerStats)
            {
                controller.Heal(characterStats, (int)(characterStats.stats.health.Value * ((HasUpgradeUnlocked(1) ? playerMaxHealthPerc : maxHealthPerc) / 100f)));
                if (HasUpgradeUnlocked(2))
                    stacksPerHit++;
            }
            else
                controller.Heal(characterStats, (int)(characterStats.stats.health.Value * (maxHealthPerc / 100f)));
            ReduceCooldown(onHitSecondsReduce);
        };
        characterStats.stats.health.OnChangeValue += () =>
        {
            var bonusHealth = characterStats.stats.health.Value - characterStats.stats.health.BaseValue;
            transform.localScale *= 1 + (bonusHealth / 3000f);
        };
        characterStats.stats.health.ChangeValueAdd += (ref int value, int old) =>
        {
            if (HasUpgradeUnlocked(0))
            {
                value += maxHealhIncrease;
            }
            if (HasUpgradeUnlocked(2))
            {
                value += (stacks * stackHealthIncrease);
            }
        };
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        stacks += stacksPerHit;
        stacksPerHit = 0;
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        hits.Clear();
        Use();
        StartCooldown();
    }

    protected override void OnUpgradeUnlocked(int index)
    {
        if(index == 1)
            characterStats.stats.health.OnChangeValue?.Invoke();
    }
}
