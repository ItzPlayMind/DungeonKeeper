using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterStats : NetworkBehaviour
{
    public StatBlock stats;
    [SerializeField] private Canvas playerUI;
    [SerializeField] private Canvas otherPlayerUI;
    [SerializeField] private float deathTimer = 5f;
    private int currentHealth;
    public int Health { get => currentHealth; }
    public System.Action OnTakeDamage;
    public System.Action OnDeath;
    public System.Action OnRespawn;
    public bool CanBeHit { get; private set; } = true;
    private Animator animator;
    private AnimationEventSender animatorEvent;
    Rigidbody2D rb;
    Canvas ui;
    UIBar healthBar;
    public bool IsDead { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        if (IsLocalPlayer)
        {
            Destroy(otherPlayerUI.gameObject);
            ui = playerUI;
            
        }
        else
        {
            Destroy(playerUI.gameObject);
            ui = otherPlayerUI;
        }
        currentHealth = stats.health.Value;
        animator = GetComponentInChildren<Animator>();
        animatorEvent = animator.GetComponent<AnimationEventSender>();
        animatorEvent.OnAnimationEvent += (AnimationEventSender.AnimationEvent e) =>
        {
            if (e == AnimationEventSender.AnimationEvent.Hit)
            {
                CanBeHit = true;
            }
        };
        rb = GetComponent<Rigidbody2D>();
        healthBar = ui.transform.Find("Healthbar").GetComponent<UIBar>();
        healthBar.UpdateBar(1f);
    }

    public void TakeDamage(int damage, Vector2 knockback)
    {
        if (!CanBeHit || IsDead)
            return;
        TakeDamageServerRPC(damage,knockback);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRPC(int damage, Vector2 knockback)
    {
        TakeDamageClientRPC(damage, knockback);
    }

    [ClientRpc]
    private void TakeDamageClientRPC(int damage, Vector2 knockback)
    {
        CanBeHit = false;
        if (IsLocalPlayer)
        {
            rb.velocity = Vector2.zero;
            rb.AddForce(knockback, ForceMode2D.Impulse);
            OnTakeDamage?.Invoke();
        }
        currentHealth -= damage;
        healthBar.UpdateBar(currentHealth / (float)stats.health.Value);
        animator.SetTrigger("hit");
        if(currentHealth <= 0)
        {
            animator.SetBool("death",true);
            OnDeath?.Invoke();
            IsDead = true;
            timer = deathTimer;
        }
    }

    private float timer = 0f;

    private void Update()
    {
        if (IsDead)
        {
            timer -= Time.deltaTime;
            if(timer <= 0)
            {
                CanBeHit = true;
                animator.SetBool("death",false);
                IsDead = false;
                currentHealth = stats.health.Value;
                healthBar.UpdateBar(1);
                OnRespawn?.Invoke();
            }
        }
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
    private void HealClientRPC(int health)
    {
        currentHealth += health;
        healthBar.UpdateBar(currentHealth / (float)stats.health.Value);
    }

    public Vector2 GenerateKnockBack(Transform hit, Transform damager, float force)
    {
        var dir = (hit.position - damager.position).normalized;
        return dir * force;
    }
}
