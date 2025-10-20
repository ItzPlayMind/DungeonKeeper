using Cinemachine.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterStats : NetworkBehaviour
{
    [SerializeField] private bool alwayShowHealthbar = false;
    [SerializeField] private UIBar healthBar;
    [SerializeField] protected float respawnTime = 0;
    [SerializeField] protected bool CanRevive = true;
    [SerializeField] private SpriteRenderer gfx;
    [SerializeField] private Color hitColor = new Color(248 / 255f, 156 / 255f, 156 / 255f, 1f);
    [SerializeField] private Color healColor = new Color(134 / 255f, 255 / 255f, 119 / 255f, 1f);
    private Coroutine hitCoroutine;
    private Coroutine healCoroutine;
    public StatBlock stats;
    protected NetworkVariable<int> currentHealth = new NetworkVariable<int>(0);

    public delegate void DamageDelegate(ulong damager, int damage);
    public delegate void ServerDamageDelegate(ulong damager, ref int damage);
    public delegate void HealDelegate(ref int amount);
    public delegate void DeathDelegate(ulong killer);

    private Material outlineMaterial;

    public int Health { get => currentHealth.Value; }
    public System.Action<int, int> OnHealthChange;
    public ServerDamageDelegate OnServerTakeDamage;
    public DamageDelegate OnClientTakeDamage;
    public DeathDelegate OnServerDeath; 
    public HealDelegate OnClientHeal;
    public System.Action OnServerRespawn;
    Rigidbody2D rb;
    public bool IsDead { get => dead.Value; }

    private NetworkVariable<bool> dead = new NetworkVariable<bool>();

    protected NetworkVariable<float> respawnTimer = new NetworkVariable<float>(0);

    public UIBar Healthbar { get => healthBar; }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        healthBar?.gameObject.SetActive(alwayShowHealthbar);
        OnHealthChange += (int _, int value) =>
        {
            if (healthBar != null)
            {
                healthBar?.UpdateBar(value / (float)stats.health.Value);
                if (healthBar is TextUIBar)
                    (healthBar as TextUIBar).Text = value + "/" + stats.health.Value;
            }
        };
        if (healthBar != null)
        {
            if(healthBar is TextUIBar)
                (healthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
            healthBar?.UpdateBar(1f);
        }
        stats.OnValuesChange += () =>
        {
            if (healthBar != null)
            {
                healthBar.UpdateBar(Health / (float)stats.health.Value); 
                if (healthBar is TextUIBar)
                    (healthBar as TextUIBar).Text = Health + "/" + stats.health.Value;
            }
        };
    }

    public void Hover(bool value)
    {
        outlineMaterial.SetInt("_Show", value ? 1 : 0);
        healthBar.gameObject.SetActive(value);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = stats.health.Value;
        }
        currentHealth.OnValueChanged += (int oldValue, int newValue) => OnHealthChange?.Invoke(oldValue, newValue);
        healthBar.transform.rotation = Quaternion.identity;

        if (gfx != null)
        {
            outlineMaterial = Instantiate(gfx.material);
            gfx.material = outlineMaterial;
        }

        var teamController = GetComponent<TeamController>();

        teamController.OnTeamChanged += (team) => SetOutlineForTeam();
        SetOutlineForTeam();
    }

    private void SetOutlineForTeam()
    {
        if (PlayerController.LocalPlayer == null) return;
        var teamController = GetComponent<TeamController>();
        if (teamController.HasSameTeam(PlayerController.LocalPlayer.gameObject))
        {
            outlineMaterial.SetColor("_OutlineColor", Color.green);
        }
    }

    public void TakeDamage(int damage, Vector2 knockback, CharacterStats damager, float stagger = 0f)
    {
        if (!enabled) return;
        if (IsDead)
            return;
        if(damager != null)
            if (damager.NetworkObjectId == PlayerController.LocalPlayer.NetworkObjectId)
            {
                ShowHealtBar();
            }
        TakeDamageServerRPC(damage, knockback, damager == null ? ulong.MaxValue : damager.NetworkObjectId, stagger);
    }

    [ServerRpc(RequireOwnership = false)]
    protected virtual void TakeDamageServerRPC(int damage, Vector2 knockback, ulong damagerID, float stagger)
    {
        OnServerTakeDamage?.Invoke(damagerID, ref damage);
        currentHealth.Value -= Mathf.Max((int)(damage * (1 - (stats.damageReduction.Value / 100f))), 0);
        TakeDamageClientRPC(damage, knockback, damagerID, stagger);
        if (currentHealth.Value <= 0)
            Die(damagerID);
    }

    [ClientRpc]
    protected virtual void TakeDamageClientRPC(int damage, Vector2 knockback, ulong damagerID, float stagger)
    {
        if (damagerID == PlayerController.LocalPlayer.NetworkObjectId)
            GameManager.instance.PrefabSystem.SpawnDamageNumber(transform.position,damage,Color.red);
        if (IsOwner)
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.AddForce(knockback, ForceMode2D.Impulse);
            }
            OnClientTakeDamage?.Invoke(damagerID, damage);
        }
        if (hitCoroutine != null)
            StopCoroutine(hitCoroutine);
        if (gfx != null)
            hitCoroutine = StartCoroutine(hitColorChange());
    }

    private IEnumerator hitColorChange()
    {
        gfx.color = hitColor;
        yield return new WaitForSeconds(0.1f);
        gfx.color = Color.white;
    }

    protected virtual void Die(ulong damagerID)
    {
        dead.Value = true;
        respawnTimer.Value = respawnTime;
        DieClientRPC(damagerID);
        OnServerDeath?.Invoke(damagerID);
    }

    [ClientRpc]
    protected virtual void DieClientRPC(ulong damagerID)
    {
    }

    private float healthBarTimer = 0f;

    protected virtual void Update()
    {
        if (!alwayShowHealthbar)
        {
            if (healthBar != null)
            {
                if (healthBarTimer > 0f)
                {
                    if (!healthBar.gameObject.activeSelf)
                        healthBar.gameObject.SetActive(true);
                    healthBarTimer -= Time.deltaTime;
                    if (healthBarTimer <= 0)
                    {
                        healthBar.gameObject.SetActive(false);
                    }
                }
            }
        }
        if (!IsServer) return;
        if (IsDead && CanRevive)
        {
            respawnTimer.Value -= Time.deltaTime;
            if (respawnTimer.Value <= 0)
            {
                Respawn();
            }
        }
    }

    public virtual void Respawn(bool revived = false)
    {
        OnServerRespawn?.Invoke();
        currentHealth.Value = stats.health.Value;
        dead.Value = false;
        respawnTimer.Value = 0;
        RespawnClientRPC(revived);
    }

    [ClientRpc]
    protected virtual void RespawnClientRPC(bool revived = false)
    {
        healthBar?.UpdateBar(1f);
    }

    public void Heal(int health,CharacterStats healer)
    {
        if (!enabled) return;
        if (IsDead)
            return;
        OnClientHeal?.Invoke(ref health);
        HealServerRPC(health, healer == null ? ulong.MaxValue : healer.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HealServerRPC(int health, ulong healerID)
    {
        currentHealth.Value += health;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, stats.health.Value);
        HealClientRPC(health,healerID);
    }

    public void ShowHealtBar()
    {
        healthBarTimer = 1f;
    }

    [ClientRpc]
    protected virtual void HealClientRPC(int health, ulong healerID)
    {
        if (healerID == PlayerController.LocalPlayer.NetworkObjectId)
            GameManager.instance.PrefabSystem.SpawnDamageNumber(transform.position, health, Color.green);
        if (healCoroutine != null)
            StopCoroutine(healCoroutine);
        if (gfx != null)
            healCoroutine = StartCoroutine(healColorChange()); 
    }

    public Vector2 GenerateKnockBack(Transform hit, Transform damager, float force)
    {
        var dir = (hit.position - damager.position).normalized;
        return dir * force * 2;
    }

    private IEnumerator healColorChange()
    {
        gfx.color = healColor;
        yield return new WaitForSeconds(0.1f);
        gfx.color = Color.white;
    }
}
