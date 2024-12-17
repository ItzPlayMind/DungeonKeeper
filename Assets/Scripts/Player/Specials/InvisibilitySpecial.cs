using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class InvisibilitySpecial : AbstractSpecial
{


    public override bool CanMoveWhileUsing() => false;

    private SpriteRenderer gfx;
    private PlayerController controller;
    protected override void _Start()
    {
        gfx = transform.Find("GFX").GetComponent<SpriteRenderer>();
        controller = GetComponent<PlayerController>();
        if (!IsLocalPlayer) return;
        controller.OnAttackPress += () =>
        {
            if (IsActive)
                Visible();
        };
        characterStats.OnClientTakeDamage += (ulong damager, int damage) =>
        {
            if (IsActive)
                Visible();
        };
    }
    private int alpha = 255;

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        controller.SetCurrentAttackIndex(1);
        if (!IsLocalPlayer) return;
        characterStats.stats.speed.ChangeValue += InvisSpeed;
        StartActive();
        InvisibleServerRPC(25, 0);
    }

    private void InvisSpeed(ref int speed, int oldSpeed) => speed += 20;

    protected override void OnActiveOver()
    {
        if (!IsLocalPlayer) return;
        Visible();
    }

    private void Visible()
    {
        StartCooldown();
        InvisibleServerRPC(255, 255);
        characterStats.stats.speed.ChangeValue -= InvisSpeed;
    }

    [ServerRpc]
    private void InvisibleServerRPC(int local, int others)
    {
        InvisibleClientRPC(local,others);
    }

    [ClientRpc]
    private void InvisibleClientRPC(int local, int others)
    {
        if (PlayerController.LocalPlayer.gameObject.layer == gameObject.layer)
        {
            alpha = local;
        }
        else
            alpha = others;
    }

    protected override void _UpdateAll()
    {
        if(gfx.color.a != alpha)
        {
            var color = gfx.color;
            color.a = Mathf.Lerp(gfx.color.a, alpha / 255f, 0.1f);
            gfx.color = color;
        }
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
    }
}
