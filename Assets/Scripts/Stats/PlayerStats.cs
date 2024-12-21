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

    [SerializeField] private TMPro.TextMeshProUGUI damageText;
    [SerializeField] private TMPro.TextMeshProUGUI specialDamageText;
    [SerializeField] private TMPro.TextMeshProUGUI speedText;
    [SerializeField] private TMPro.TextMeshProUGUI healthText;
    [SerializeField] private TMPro.TextMeshProUGUI damageReductionText;

    [SerializeField] private Volume deathScreenEffect;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private TMPro.TextMeshProUGUI deathTimer;

    public System.Action OnClientRespawn;

    //[SerializeField] private GameObject hitPrefab;
    private Animator animator;
    private UIBar playerHealthBar;

    private Dictionary<ulong, float> assistTimers = new Dictionary<ulong, float>();

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
    }


    protected override void Start()
    {
        base.Start();
        if (IsLocalPlayer)
        {
            stats.damage.OnChangeValue += () => damageText.text = stats.damage.Value.ToString();
            stats.specialDamage.OnChangeValue += () => specialDamageText.text = stats.specialDamage.Value.ToString();
            stats.speed.OnChangeValue += () => speedText.text = stats.speed.Value.ToString();
            stats.health.OnChangeValue += () => healthText.text = (stats.health.Value-stats.health.BaseValue).ToString();
            stats.damageReduction.OnChangeValue += () => damageReductionText.text = stats.damageReduction.Value.ToString();

            damageText.text = stats.damage.Value.ToString();
            specialDamageText.text = stats.specialDamage.Value.ToString();
            speedText.text = stats.speed.Value.ToString();
            healthText.text = (stats.health.Value - stats.health.BaseValue).ToString();
            damageReductionText.text = stats.damageReduction.Value.ToString();

            Healthbar.transform.parent.gameObject.SetActive(false);
            playerHealthBar = playerUI.transform.Find("Healthbar").GetComponent<UIBar>();
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
        }
        else
        {
            playerUI.gameObject.SetActive(false);
        }

        animator = GetComponentInChildren<Animator>();
    }

    [ServerRpc(RequireOwnership = false)]
    protected override void TakeDamageServerRPC(int damage, Vector2 knockback, ulong damagerID)
    {
        healthTimer = GameManager.instance.OUT_OF_COMBAT_TIME;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects[damagerID].GetComponent<PlayerStats>() != null)
            assistTimers[damagerID] = GameManager.instance.OUT_OF_COMBAT_TIME;
        base.TakeDamageServerRPC(damage, knockback, damagerID);
    }

    [ClientRpc]
    protected override void TakeDamageClientRPC(int damage, Vector2 knockback, ulong damagerID)
    {
        base.TakeDamageClientRPC(damage, knockback, damagerID);
    }

    protected override void Die(ulong damagerID)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects[damagerID].GetComponent<Inventory>()?.AddCash(GameManager.instance.GOLD_FOR_KILL);
        foreach(var key in assistTimers.Keys)
        {
            if (key == damagerID) continue;
            if (assistTimers[key] > 0)
                NetworkManager.Singleton.SpawnManager.SpawnedObjects[key].GetComponent<Inventory>()?.AddCash(GameManager.instance.GOLD_FOR_KILL/4);
        }
        assistTimers.Clear();
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

    protected override void Respawn()
    {
        base.Respawn();
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
            playerHealthBar.UpdateBar(1);
            (playerHealthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
        }
    }

    [ClientRpc]
    protected override void HealClientRPC(int health)
    {
        base.HealClientRPC(health);
        playerHealthBar.UpdateBar(Health / (float)stats.health.Value);
        (playerHealthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
    }

    private float healthTimer = 1f;

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
            foreach(var key in assistTimers.Keys)
            {
                if (assistTimers[key] > 0)
                    assistTimers[key] -= Time.deltaTime;
            }
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
        base.Update();
    }
}
