using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float attackingSpeedMultiplier = 0.1f;
    private PlayerAttack attack;
    private AbstractSpecial special;
    private Rigidbody2D rb;
    private PlayerStats stats;
    public bool canMove { get => (special != null && !special.isUsing) || (special != null && special.isUsing && special.CanMoveWhileUsing()); }

    private float speedAfterDamage;
    private float staggerTime;

    private void Start()
    {
        attack = GetComponent<PlayerAttack>();
        special = GetComponent<AbstractSpecial>();
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
    }

    public void OnTeamAssigned()
    {
    }

    public void Stagger(float time)
    {
        speedAfterDamage = 0.01f;
        staggerTime = time;
    }

    private void Update()
    {
        if (speedAfterDamage < 1)
            speedAfterDamage = Math.Min(1, speedAfterDamage + Time.deltaTime / staggerTime);
    }

    public void Move(Vector2 input)
    {
        if (!enabled) return;
        if (!canMove) return;
        var speed = new Vector2(stats.stats.speed.Value, Mathf.Max(stats.stats.speed.Value - 10, 0));
        if (attack.isAttacking && !special.isUsing)
            speed *= attackingSpeedMultiplier;
        rb.AddForce(input * speedAfterDamage * speed * rb.mass, ForceMode2D.Force);
    }

    public void Stop()
    {
        rb.velocity = Vector2.zero;
    }
}
