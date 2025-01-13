using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChargeSpecial : AbstractSpecial
{
    [SerializeField] private Vector2 movementSpeedMultiplier = new Vector2(0.1f,0.1f);
    [SerializeField] private float chargeSpeed = 25;
    [SerializeField] private float knockBackForce = 15;
    [DescriptionCreator.DescriptionVariable("green")]
    public int BonusHPScaling { get => 5; }
    [DescriptionCreator.DescriptionVariable("green", "{Damage} + {BonusHPScaling}% Bonus HP")]
    public int ChargeDamage { get => Damage + (int)((characterStats.stats.health.Value - characterStats.stats.health.BaseValue) * (BonusHPScaling/100f)); }
    Vector2 mouseWorldPos;
    bool isCharging = false;
    Rigidbody2D rb;
    CollisionSender sender;
    protected override void _Start()
    {
        UseRotation = true;
        rb = GetComponent<Rigidbody2D>();
        sender = GetComponent<CollisionSender>();
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        isCharging = false;
        StartCooldown();
        sender.onCollisionEnter = null;
    }

    protected override void _Update()
    {
        if (!IsLocalPlayer)
            return;
        if (isCharging)
        {
            mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
            var dir = (mouseWorldPos-(Vector2)transform.position).normalized;
            rb.AddForce(dir * chargeSpeed * (characterStats.stats.speed.Value* movementSpeedMultiplier) * 500 * Time.deltaTime, ForceMode2D.Force);
        }
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
        if (!IsLocalPlayer) return;
        sender.onCollisionEnter = Hit;
        isCharging = true;
    }

    private void Hit(GameObject collision, ref bool hit)
    {
        if (collision == gameObject)
            return;
        var stats = collision.GetComponent<CharacterStats>();
        if(stats != null)
        {
            stats.TakeDamage(ChargeDamage, stats.GenerateKnockBack(stats.transform, transform, knockBackForce), characterStats);
        }
    }
}
