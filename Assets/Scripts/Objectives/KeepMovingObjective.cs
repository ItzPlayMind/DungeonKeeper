using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepMovingObjective : Objective
{
    Rigidbody2D rb;
    CharacterStats stats;
    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer) return;
        rb = PlayerController.LocalPlayer.GetComponent<Rigidbody2D>();
        stats = PlayerController.LocalPlayer.GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (rb.velocity == Vector2.zero && !PlayerController.LocalPlayer.Attack.isAttacking)
            stats.TakeDamage(1,Vector2.zero,null);
    }
}
