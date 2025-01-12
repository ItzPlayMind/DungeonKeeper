using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private PlayerAttack attack;
    private AbstractSpecial special;
    private Rigidbody2D rb;
    private PlayerStats stats;
    public bool canMove { get => !attack.isAttacking || (special != null && special.isUsing && special.CanMoveWhileUsing()); }

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



    public void Move(Vector2 input)
    {
        if (!enabled) return;
        if (canMove)
            rb.AddForce(input * new Vector2(stats.stats.speed.Value, Mathf.Max(stats.stats.speed.Value - 10, 0)), ForceMode2D.Force);
    }

    public void Stop()
    {
        rb.velocity = Vector2.zero;
    }
}
