using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterStats : NetworkBehaviour
{
    [SerializeField] private bool alwayShowHealthbar = false;
    [SerializeField] private UIBar healthBar;
    [SerializeField] protected float respawnTime = 0;
    [SerializeField] private bool CanRevive = true;
    [SerializeField] private SpriteRenderer gfx;
    [SerializeField] private Color hitColor = new Color(248 / 255f, 156 / 255f, 156 / 255f, 1f);
    [SerializeField] private Color healColor = new Color(134 / 255f, 255 / 255f, 119 / 255f, 1f);
    private Coroutine hitCoroutine;
    private Coroutine healCoroutine;
    public StatBlock stats;
    protected NetworkVariable<int> currentHealth = new NetworkVariable<int>(0);

    public delegate void DamageDelegate(ulong damager, int damage);
    public delegate void DeathDelegate(ulong killer);

    public int Health { get => currentHealth.Value; }
    public System.Action<int, int> OnHealthChange;
    public DamageDelegate OnServerTakeDamage;
    public DamageDelegate OnClientTakeDamage;
    public DeathDelegate OnServerDeath;
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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth.Value = stats.health.Value;
        currentHealth.OnValueChanged += (int oldValue, int newValue) => OnHealthChange?.Invoke(oldValue, newValue);
    }

    public void TakeDamage(int damage, Vector2 knockback, CharacterStats damager)
    {
        if (IsDead)
            return;
        ShowHealtBar();
        TakeDamageServerRPC(damage, knockback, damager == null ? ulong.MaxValue : damager.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    protected virtual void TakeDamageServerRPC(int damage, Vector2 knockback, ulong damagerID)
    {
        currentHealth.Value -= Mathf.Max((int)(damage * (1 - (stats.damageReduction.Value / 100f))), 0);
        OnServerTakeDamage?.Invoke(damagerID, damage);
        TakeDamageClientRPC(damage, knockback, damagerID);
        if (currentHealth.Value <= 0)
            Die(damagerID);
    }

    [ClientRpc]
    protected virtual void TakeDamageClientRPC(int damage, Vector2 knockback, ulong damagerID)
    {
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
        DieClientRPC(damagerID);
        OnServerDeath?.Invoke(damagerID);
        dead.Value = true;
        respawnTimer.Value = respawnTime;
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

    protected virtual void Respawn()
    {
        OnServerRespawn?.Invoke();
        currentHealth.Value = stats.health.Value;
        dead.Value = false;
        RespawnClientRPC();
    }

    [ClientRpc]
    protected virtual void RespawnClientRPC()
    {
        healthBar?.UpdateBar(1f);
    }

    public void Heal(int health)
    {
        HealServerRPC(health);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HealServerRPC(int health)
    {
        if (IsDead)
            return;
        currentHealth.Value += health;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, stats.health.Value);
        HealClientRPC(health);
    }

    public void ShowHealtBar()
    {
        healthBarTimer = 1f;
    }

    [ClientRpc]
    protected virtual void HealClientRPC(int health)
    {
        if (healCoroutine != null)
            StopCoroutine(healCoroutine);
        healCoroutine = StartCoroutine(healColorChange()); 
    }

    public Vector2 GenerateKnockBack(Transform hit, Transform damager, float force)
    {
        var dir = (hit.position - damager.position).normalized;
        return dir * force;
    }

    private IEnumerator healColorChange()
    {
        gfx.color = healColor;
        yield return new WaitForSeconds(0.1f);
        gfx.color = Color.white;
    }
}
