using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterStats : NetworkBehaviour
{
    [SerializeField] protected float respawnTime = 0;
    [SerializeField] private bool CanRevive = true;
    [SerializeField] private SpriteRenderer gfx;
    [SerializeField] private Color hitColor = new Color(248/255f, 156/255f, 156/255f,1f);
    private Coroutine hitCoroutine;
    public StatBlock stats;
    protected NetworkVariable<int> currentHealth = new NetworkVariable<int>(0);
    public int Health { get => currentHealth.Value; }
    public System.Action<int, int> OnHealthChange;
    public System.Action<ulong,int> OnServerTakeDamage;
    public System.Action<ulong> OnServerDeath;
    public System.Action OnServerRespawn;
    Rigidbody2D rb;
    public bool IsDead { get => dead.Value; }

    private NetworkVariable<bool> dead = new NetworkVariable<bool>();

    protected NetworkVariable<float> respawnTimer = new NetworkVariable<float>(0);

    // Start is called before the first frame update
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth.Value = stats.health.Value;
       currentHealth.OnValueChanged += (int oldValue, int newValue) => OnHealthChange?.Invoke(oldValue,newValue);
    }

    public void TakeDamage(int damage, Vector2 knockback, CharacterStats damager)
    {
        if (IsDead)
            return;
        TakeDamageServerRPC(damage,knockback,damager == null ? ulong.MaxValue : damager.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRPC(int damage, Vector2 knockback,ulong damagerID)
    {
        currentHealth.Value -= (int)(damage * (1 - (stats.damageReduction.Value / 100f)));
        OnServerTakeDamage?.Invoke(damagerID, damage);
        TakeDamageClientRPC(damage, knockback, damagerID);
        if (currentHealth.Value <= 0)
            Die(damagerID);
    }

    [ClientRpc]
    protected virtual void TakeDamageClientRPC(int damage, Vector2 knockback,ulong damagerID)
    {
        if (IsOwner)
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.AddForce(knockback, ForceMode2D.Impulse);
            }
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

    protected virtual void Update()
    {
        if (!IsServer) return;
        if (IsDead && CanRevive)
        {
            respawnTimer.Value -= Time.deltaTime;
            if(respawnTimer.Value <= 0)
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

    [ClientRpc]
    protected virtual void HealClientRPC(int health)
    {
    }

    public Vector2 GenerateKnockBack(Transform hit, Transform damager, float force)
    {
        var dir = (hit.position - damager.position).normalized;
        return dir * force;
    }
}
