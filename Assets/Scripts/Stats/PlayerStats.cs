using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerStats : CharacterStats
{
    [SerializeField] private Canvas playerUI;
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
            currentHealth.OnValueChanged += (int old, int value) =>
            {
                healthBar?.UpdateBar(value / (float)stats.health.Value);
                (healthBar as TextUIBar).Text = value + "/" + stats.health.Value;
            };
        }
    }


    protected override void Start()
    {
        base.Start();
        if (IsLocalPlayer)
        {
            healthBar = playerUI.transform.Find("Healthbar").GetComponent<UIBar>();
            healthBar.UpdateBar(1f);
            (healthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
            stats.OnValuesChange += () =>
            {
                healthBar.UpdateBar(Health / (float)stats.health.Value);
                (healthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
            };
        }
        else
        {
            playerUI.gameObject.SetActive(false);
        }
        animator = GetComponentInChildren<Animator>();
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
            healthBar.UpdateBar(1);
            (healthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
        }
    }

    [ClientRpc]
    protected override void HealClientRPC(int health)
    {
        base.HealClientRPC(health);
        if (IsLocalPlayer)
        {
            healthBar.UpdateBar(Health / (float)stats.health.Value);
            (healthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
        }
    }

    protected override void Update()
    {
        if (IsLocalPlayer)
        {
            if (IsDead && deathScreenEffect.weight < 1)
                deathScreenEffect.weight += Time.deltaTime;
            if (!IsDead && deathScreenEffect.weight > 0)
                deathScreenEffect.weight -= Time.deltaTime;
        }
        base.Update();
    }
}
