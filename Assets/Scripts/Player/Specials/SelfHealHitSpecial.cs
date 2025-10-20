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
    [SerializeField] private int maxHealthIncrease = 300;
    [DescriptionCreator.DescriptionVariable("green")]
    [SerializeField] private float playerMaxHealthPerc = 5f;
    [DescriptionCreator.DescriptionVariable("green")]
    [SerializeField] private int stackHealthIncrease = 5;

    private List<ulong> hits = new List<ulong>();

    private int stacks = 0;
    private int stacksPerHit = 0;
    private Vector3 baseScale;

    public override int Damage => base.Damage + (int)((characterStats.stats.health.Value - characterStats.stats.health.BaseValue)*0.05f);

    protected override void _Start()
    {
        if (!IsLocalPlayer) return;
        hitbox.gameObject.layer = gameObject.layer;
        baseScale = transform.localScale;
        hitbox.onCollisionEnter += (GameObject collider, ref bool hit) =>
        {
            if (controller.TeamController.HasSameTeam(collider.gameObject)) return;
            if (collider.gameObject == gameObject) return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats == null) return;
            if(hits.Contains(stats.NetworkObjectId)) return;
            hits.Add(stats.NetworkObjectId);
            DealDamage(stats, Damage, Vector2.zero);
            controller.Heal(characterStats, (int)(characterStats.stats.health.Value * ((HasUpgradeUnlocked(1) ? playerMaxHealthPerc : maxHealthPerc) / 100f)));
            if (HasUpgradeUnlocked(2))
                stacksPerHit++;
            ReduceCooldown(onHitSecondsReduce);
        };
        characterStats.stats.health.OnChangeValue += () =>
        {
            var bonusHealth = characterStats.stats.health.Value - characterStats.stats.health.BaseValue;
            transform.localScale = baseScale * (1 + (bonusHealth / 3000f));
        };
        characterStats.stats.health.ChangeValueAdd += (ref int value, int old) =>
        {
            if (HasUpgradeUnlocked(0))
            {
                value += maxHealthIncrease;
            }
            if (HasUpgradeUnlocked(2))
            {
                value += (stacks * stackHealthIncrease);
            }
        };
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        Debug.Log(stacksPerHit);
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
