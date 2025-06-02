using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class PlayerMeeleAttack : PlayerAttack
{
    [System.Serializable]
    private class AttackSetting
    {
        public float damageMultiplier = 1;
        public float knockBack = 4;
        public float selfKnockBack = 0;
        public float stagger = 0.1f;
    }

    [SerializeField] private AttackSetting[] attackSettings = new AttackSetting[2];
    [SerializeField] private Transform hitboxes;
    [SerializeField] private float attackComboTime = 0.2f;

    private float attackComboTimer = 0;
    private int currentAttack = 0;

    private bool currentAttackFixed = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsLocalPlayer)
            return;
        foreach (var item in hitboxes.GetComponentsInChildren<CollisionSender>())
        {
            item.onCollisionEnter += (GameObject collider, ref bool _) =>
            {
                if (collider == gameObject)
                    return;
                var stats = collider.GetComponent<CharacterStats>();
                if (stats != null && !stats.IsDead)
                {
                    var damage = (int)(this.stats.stats.damage.Value * attackSettings[currentAttack].damageMultiplier);
                    OnAttack?.Invoke(stats.NetworkObjectId, this.stats.NetworkObjectId, ref damage);
                    stats.TakeDamage(damage, stats.GenerateKnockBack(stats.transform, transform, attackSettings[currentAttack].knockBack), this.stats, attackSettings[currentAttack].stagger);
                }
            };
        }
        stats.OnClientRespawn += () =>
        {
            currentAttack = 0;
        };
    }

    public void SetCurrentAttackIndex(int index)
    {
        currentAttack = index;
        currentAttackFixed = true;
    }

    protected override void OnAttackEnd()
    {
        attackComboTimer = attackComboTime;
        currentAttack = (currentAttack + 1) % attackSettings.Length;
    }

    public override void OnTeamAssigned()
    {
        for (int i = 0; i < hitboxes.childCount; i++)
            hitboxes.GetChild(i).gameObject.layer = gameObject.layer;
    }

    protected override void OnSelfKnockback()
    {
        rb.AddForce((controller.isFlipped.Value ? transform.right : -transform.right) * attackSettings[currentAttack].selfKnockBack, ForceMode2D.Impulse);
    }

    protected override void OnAttackTriggered()
    {
        currentAttackFixed = false;
        animator.SetInteger("attack", currentAttack);
    }

    protected override void _Update()
    {
        if (!currentAttackFixed)
        {
            if (attackComboTimer > 0)
                attackComboTimer -= Time.deltaTime;
            else if (currentAttack != 0)
                currentAttack = 0;
        }
    }
}
