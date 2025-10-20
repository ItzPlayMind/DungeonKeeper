using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class InvisibilitySpecial : AbstractSpecial
{
    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")] private int speedIncrease = 20;
    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")] private int cripple = 80;
    [SerializeField]
    [DescriptionCreator.DescriptionVariable("white")] private int crippleTime = 1;
    public override bool CanMoveWhileUsing() => true;

    private SpriteRenderer gfx;
    private PlayerMeeleAttack attack;
    private PlayerController playerController;
    protected override void _Start()
    {
        gfx = transform.Find("GFX").GetComponent<SpriteRenderer>();
        attack = controller.Attack as PlayerMeeleAttack;
        if (!IsLocalPlayer) return;
        playerController = GetComponent<PlayerController>();
        playerController.OnKill += () =>
        {
            if (HasUpgradeUnlocked(2))
                SetCooldown(0);
        };
        attack.OnAttackPress += () =>
        {
            if (IsActive)
                Visible();
        };
        characterStats.OnClientTakeDamage += (ulong damager, int damage) =>
        {
            if (IsActive)
                Visible();
        };
        characterStats.stats.speed.ChangeValueAdd += InvisSpeed;
    }
    private int alpha = 255;

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        this.attack.SetCurrentAttackIndex(1);
        if(HasUpgradeUnlocked(1))
            this.attack.OnAttack += CrippleTarget;
        if (!IsLocalPlayer) return;
        StartActive();
        InvisibleServerRPC(25, 0);
    }

    private void CrippleTarget(ulong target, ulong damager, ref int amount)
    {
        var manager = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target]?.GetComponent<EffectManager>();
        if (manager != null)
            manager.AddEffect("slow", crippleTime, cripple, characterStats);
        this.attack.OnAttack -= CrippleTarget;
    }

    private void InvisSpeed(ref int speed, int oldSpeed)
    {
        if (IsActive && HasUpgradeUnlocked(0))
        {
            speed += speedIncrease;
        }
    }

    protected override void OnActiveOver()
    {
        if (!IsLocalPlayer) return;
        Visible();
    }

    private void Visible()
    {
        StartCooldown();
        InvisibleServerRPC(255, 255);
    }

    [ServerRpc]
    private void InvisibleServerRPC(int local, int others)
    {
        InvisibleClientRPC(local,others);
    }

    [ClientRpc]
    private void InvisibleClientRPC(int local, int others)
    {
        if (controller.TeamController.HasSameTeam(PlayerController.LocalPlayer.gameObject))
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
