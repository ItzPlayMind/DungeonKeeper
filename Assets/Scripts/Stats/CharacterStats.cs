using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterStats : NetworkBehaviour
{
    public StatBlock stats;
    [SerializeField] private float deathTimer = 5f;
    private int currentHealth;
    public int Health { get => currentHealth; }
    public System.Action<ulong> OnTakeDamage;
    public System.Action<ulong> OnDeath;
    public System.Action OnRespawn;
    public bool CanBeHit { get; protected set; } = true;
    Rigidbody2D rb;
    public bool IsDead { get; protected set; }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        currentHealth = stats.health.Value;
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual bool CanBeHitConstantly() { return true; }

    public void TakeDamage(int damage, Vector2 knockback, ulong damagerID = ulong.MaxValue)
    {
        if ((!CanBeHit && !CanBeHitConstantly()) || IsDead)
            return;
        TakeDamageServerRPC(damage,knockback,damagerID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRPC(int damage, Vector2 knockback,ulong damagerID)
    {
        TakeDamageClientRPC(damage, knockback, damagerID);
    }

    [ClientRpc]
    protected virtual void TakeDamageClientRPC(int damage, Vector2 knockback,ulong damagerID)
    {
        CanBeHit = false;
        currentHealth -= (int)(damage * (1-(stats.damageReduction.Value/100f)));
        if (IsLocalPlayer)
        {
            rb.velocity = Vector2.zero;
            rb.AddForce(knockback, ForceMode2D.Impulse);
        }
        OnTakeDamage?.Invoke(damagerID);
        if (currentHealth <= 0)
        {
            Die(damagerID);
        }
    }

    protected virtual void Die(ulong damagerID)
    {
        if (IsLocalPlayer)
            OnDeathServerRPC(damagerID);
        OnDeath?.Invoke(damagerID);
        IsDead = true;
        timer = deathTimer;
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnDeathServerRPC(ulong damagerID)
    {
        if (damagerID == ulong.MaxValue)
            return;
        NetworkManager.Singleton.ConnectedClients[damagerID].PlayerObject.GetComponent<Inventory>().AddCash(GameManager.instance.GOLD_FOR_KILL);
    }

    private float timer = 0f;

    private void Update()
    {
        if (IsDead)
        {
            timer -= Time.deltaTime;
            if(timer <= 0)
            {
                Respawn();
            }
        }
    }

    protected virtual void Respawn()
    {
        CanBeHit = true;
        IsDead = false;
        currentHealth = stats.health.Value;
        OnRespawn?.Invoke();
    }

    public void Heal(int health)
    {
        HealServerRPC(health);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HealServerRPC(int health)
    {
        HealClientRPC(health);
    }

    [ClientRpc]
    protected virtual void HealClientRPC(int health)
    {
        if (IsDead)
            return;
        currentHealth += health;
        currentHealth = Mathf.Clamp(currentHealth, 0, stats.health.Value);
    }

    public Vector2 GenerateKnockBack(Transform hit, Transform damager, float force)
    {
        var dir = (hit.position - damager.position).normalized;
        return dir * force;
    }
}