using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static DebugConsole;

public class PlayerStats : CharacterStats
{
    [SerializeField] private float healthPerSecond = 1;
    [SerializeField] private Canvas playerUI;

    [SerializeField] private UIBar playerHealthBar;
    [SerializeField] private TMPro.TextMeshProUGUI damageText;
    [SerializeField] private TMPro.TextMeshProUGUI specialDamageText;
    [SerializeField] private TMPro.TextMeshProUGUI speedText;
    [SerializeField] private TMPro.TextMeshProUGUI attackSpeedText;
    [SerializeField] private TMPro.TextMeshProUGUI healthText;
    [SerializeField] private TMPro.TextMeshProUGUI damageReductionText;

    public System.Action OnClientRespawn;
    private PlayerMovement movement;

    private Animator animator;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CanRevive = GameManager.instance.RESPAWN_TIME != -1;
        movement = GetComponent<PlayerMovement>();
    }


    protected override void Start()
    {
        base.Start();
        if (IsLocalPlayer)
        {
            stats.damage.OnChangeValue += () => damageText.text = stats.damage.Value.ToString();
            stats.specialDamage.OnChangeValue += () => specialDamageText.text = stats.specialDamage.Value.ToString();
            stats.speed.OnChangeValue += () => speedText.text = stats.speed.Value.ToString();
            stats.attackSpeed.OnChangeValue += () => attackSpeedText.text = stats.attackSpeed.Value.ToString();
            stats.health.OnChangeValue += () => healthText.text = (stats.health.Value-stats.health.BaseValue).ToString();
            stats.damageReduction.OnChangeValue += () => damageReductionText.text = stats.damageReduction.Value.ToString();

            Healthbar.transform.parent.gameObject.SetActive(false);
            playerHealthBar.UpdateBar(1f);
            (playerHealthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
            OnHealthChange += (int _, int value) =>
            {
                playerHealthBar.UpdateBar(value / (float)stats.health.Value);
                (playerHealthBar as TextUIBar).Text = value + "/" + stats.health.Value;
            };
            stats.OnValuesChange += () =>
            {
                playerHealthBar.UpdateBar(Health / (float)stats.health.Value);
                (playerHealthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
            };
            DebugConsole.OnCommand((Command command) =>
            {
                if (command.args.Length != 1) return;
                TakeDamage(1000000, Vector2.zero, this);
            }, "player", "kill");
        }
        else
        {
            playerUI.gameObject.SetActive(false);
        }

        animator = GetComponentInChildren<Animator>();
    }

    [ServerRpc(RequireOwnership = false)]
    protected override void TakeDamageServerRPC(int damage, Vector2 knockback, ulong damagerID, float stagger)
    {
        healthTimer = GameManager.OUT_OF_COMBAT_TIME;
        base.TakeDamageServerRPC(damage, knockback, damagerID,stagger);
    }

    [ClientRpc]
    protected override void TakeDamageClientRPC(int damage, Vector2 knockback, ulong damagerID, float stagger)
    {
        if (IsLocalPlayer)
        {
            CinemachineShake.Instance.Shake(0.5f, 0.1f);
            movement.Stagger(stagger);
        }
        base.TakeDamageClientRPC(damage, knockback, damagerID, stagger);
    }

    protected override void Die(ulong damagerID)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects[damagerID].GetComponent<Inventory>()?.AddCash(GameManager.instance.GOLD_FOR_KILL/4*3);
        GameManager.instance.AddCashToTeamFromPlayer(NetworkObjectId, GameManager.instance.GOLD_FOR_KILL / 4);
        respawnTime = GameManager.instance.RESPAWN_TIME;
        base.Die(damagerID);
        GameManager.instance.Chat.AddMessage($"{damagerID} <color=red>killed</color> {NetworkObjectId}");
    }

    [ClientRpc]
    protected override void DieClientRPC(ulong damagerID)
    {
        if (damagerID == PlayerController.LocalPlayer.NetworkObjectId)
            PlayerController.LocalPlayer.OnKill?.Invoke();
        if (IsLocalPlayer)
        {
            GameManager.instance.DeathScreen.Show();
            animator.SetBool("death", true);
        }
    }

    public override void Respawn(bool revived = false)
    {
        base.Respawn(revived);
    }

    [ClientRpc]
    protected override void RespawnClientRPC(bool revived = false)
    {
        if (IsLocalPlayer)
        {
            OnClientRespawn?.Invoke();
            GameManager.instance.DeathScreen.Hide();
            if(!revived)
                transform.position = GameManager.instance.GetSpawnPoint(gameObject.layer).position;
            animator.SetBool("death", false);
            playerHealthBar.UpdateBar(1);
            (playerHealthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
        }
    }

    [ClientRpc]
    protected override void HealClientRPC(int health, ulong healerID)
    {
        base.HealClientRPC(health, healerID);
        if (IsLocalPlayer)
        {
            playerHealthBar.UpdateBar(Health / (float)stats.health.Value);
            (playerHealthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
        }
    }

    private float healthTimer = 1f;
    private float statTimer = 0f;
    protected override void Update()
    {
        if (IsLocalPlayer)
        {
            if (statTimer > 0)
            {
                statTimer -= Time.deltaTime;
            } else {
                damageText.text = stats.damage.Value.ToString();
                specialDamageText.text = stats.specialDamage.Value.ToString();
                speedText.text = stats.speed.Value.ToString();
                attackSpeedText.text = stats.attackSpeed.Value.ToString();
                healthText.text = (stats.health.Value - stats.health.BaseValue).ToString();
                damageReductionText.text = stats.damageReduction.Value.ToString();
                statTimer = 0.1f;
            }
        }
        if (IsServer)
        {
            if (!IsDead)
            {
                if (healthTimer > 0f)
                    healthTimer -= Time.deltaTime;
                else
                {
                    if (currentHealth.Value < stats.health.Value)
                    {
                        currentHealth.Value++;
                        healthTimer = 1 / healthPerSecond;
                    }
                }
            }
        }
        base.Update();
    }
}
