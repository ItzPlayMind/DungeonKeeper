using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerStats : CharacterStats
{
    [SerializeField] private float healthPerSecond = 1;
    [SerializeField] private Canvas playerUI;
    [SerializeField] private Canvas otherPlayerUI;
    [SerializeField] private Volume deathScreenEffect;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private TMPro.TextMeshProUGUI deathTimer;

    public System.Action OnClientRespawn;

    //[SerializeField] private GameObject hitPrefab;
    private UIBar healthBar;
    private Animator animator;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsLocalPlayer)
        {
            respawnTimer.OnValueChanged += (float old, float value) =>
            {
                deathTimer.text = (Mathf.CeilToInt(value)).ToString();
            };
        }
        currentHealth.OnValueChanged += (int old, int value) =>
        {
            healthBar?.UpdateBar(value / (float)stats.health.Value);
            (healthBar as TextUIBar).Text = value + "/" + stats.health.Value;
        };
    }


    protected override void Start()
    {
        base.Start();
        if (IsLocalPlayer)
        {
            otherPlayerUI.gameObject.SetActive(false);
            healthBar = playerUI.transform.Find("Healthbar").GetComponent<UIBar>();
        }
        else
        {
            playerUI.gameObject.SetActive(false);
            healthBar = otherPlayerUI.transform.Find("Healthbar").GetComponent<UIBar>();
            healthBar.gameObject.SetActive(false);
        }
        healthBar.UpdateBar(1f);
        (healthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
        stats.OnValuesChange += () =>
        {
            healthBar.UpdateBar(Health / (float)stats.health.Value);
            (healthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
        };
        animator = GetComponentInChildren<Animator>();
    }

    [ServerRpc(RequireOwnership = false)]
    protected override void TakeDamageServerRPC(int damage, Vector2 knockback, ulong damagerID)
    {
        healthTimer = GameManager.instance.OUT_OF_COMBAT_TIME;
        base.TakeDamageServerRPC(damage, knockback, damagerID);
    }

    [ClientRpc]
    protected override void TakeDamageClientRPC(int damage, Vector2 knockback, ulong damagerID)
    {
        base.TakeDamageClientRPC(damage, knockback, damagerID);
        if(!IsLocalPlayer)
            healthBarTimer = 1f;
    }

    protected override void Die(ulong damagerID)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects[damagerID].GetComponent<Inventory>()?.AddCash(GameManager.instance.GOLD_FOR_KILL);
        respawnTime = GameManager.instance.RESPAWN_TIME.Value;
        base.Die(damagerID);
        GameManager.instance.Chat.AddMessage($"{damagerID} <color=red>killed</color> {NetworkObjectId}");
    }

    [ClientRpc]
    protected override void DieClientRPC(ulong damagerID)
    {
        if (IsLocalPlayer)
        {
            deathScreen.SetActive(true);
            animator.SetBool("death", true);
        }
    }

    [ClientRpc]
    protected override void RespawnClientRPC()
    {
        if (IsLocalPlayer)
        {
            OnClientRespawn?.Invoke();
            deathScreen.SetActive(false);
            transform.position = GameManager.instance.GetSpawnPoint(gameObject.layer).position;
            animator.SetBool("death", false);
        }
        healthBar.UpdateBar(1);
        (healthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
    }

    [ClientRpc]
    protected override void HealClientRPC(int health)
    {
        base.HealClientRPC(health);
        healthBar.UpdateBar(Health / (float)stats.health.Value);
        (healthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
    }

    private float healthTimer = 1f;
    private float healthBarTimer = 0f;

    protected override void Update()
    {
        if (IsLocalPlayer)
        {
            if (IsDead && deathScreenEffect.weight < 1)
                deathScreenEffect.weight += Time.deltaTime;
            if (!IsDead && deathScreenEffect.weight > 0)
                deathScreenEffect.weight -= Time.deltaTime;
            
        }
        if (IsServer)
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
        if (!IsLocalPlayer)
        {
            if(healthBarTimer > 0f)
            {
                if(!healthBar.gameObject.activeSelf)
                    healthBar.gameObject.SetActive(true);
                healthBarTimer -= Time.deltaTime;
                if(healthBarTimer <= 0)
                {
                    healthBar.gameObject.SetActive(false);
                }
            }
        }
        base.Update();
    }
}
