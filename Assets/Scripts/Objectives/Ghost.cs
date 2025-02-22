using Cinemachine.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Ghost : Objective
{
    [SerializeField] private float followRange;
    [SerializeField] private float attackRange;
    [SerializeField] private int gold = 500;
    private UIBar healthbar;

    private CharacterStats stats;
    private Rigidbody2D rb;
    private CharacterStats target;
    private Vector2 originalPos;
    private SpriteRenderer spriteRenderer;
    private Light2D ownLight;
    private Collider2D coll;

    public override void OnNetworkSpawn()
    {
        healthbar = GetComponentInChildren<UIBar>();
        stats = GetComponent<CharacterStats>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        ownLight = GetComponentInChildren<Light2D>();
        coll = GetComponent<Collider2D>();
        if (!IsServer) return;
        originalPos = transform.position;
        rb = GetComponent<Rigidbody2D>();
        stats.OnServerDeath += (ulong damager) =>
        {
            OnDeathClientRPC(damager);
            GameManager.instance.AddCashToTeamFromPlayer(damager, gold);
            Complete(damager);
            transform.position = originalPos;
        };
        stats.OnServerTakeDamage += (ulong damager, int damage) =>
        {
            target = NetworkManager.Singleton.SpawnManager.SpawnedObjects[damager].GetComponent<CharacterStats>();
        };
        stats.OnServerRespawn += () =>
        {
            GameManager.instance.Chat.AddMessage($"<color=green>{gameObject.name}</color> has spawned!");
            OnRespawnClientRPC();
        };
    }

    [ClientRpc]
    private void OnDeathClientRPC(ulong damager)
    {
        healthbar.gameObject.SetActive(false);
        spriteRenderer.enabled = false;
        ownLight.enabled = false;
        coll.enabled = false;
    }

    [ClientRpc]
    private void OnRespawnClientRPC()
    {
        spriteRenderer.enabled = true;
        ownLight.enabled = true;
        coll.enabled = true;
        healthbar.gameObject.SetActive(true);
        healthbar.UpdateBar(1f);
    }


    private float timer = 1f;

    bool isReturning;

    private void Update()
    {
        if (!IsServer) return;
        if (stats.IsDead) target = null;
        if (Vector2.Distance(transform.position, originalPos) > followRange)
        {
            isReturning = true;
            target = null;
        }
        if (isReturning)
        {
            stats.Heal((int)(stats.stats.health.Value / 100f));
            healthbar.UpdateBar(stats.Health/(float)stats.stats.health.Value);
            var originalPosDir = (originalPos - (Vector2)transform.position).normalized;
            rb.velocity = (originalPosDir * stats.stats.speed.Value * 2 * Time.deltaTime);
            if (Vector2.Distance(transform.position, originalPos) <= 0.1)
            {
                rb.velocity = Vector2.zero;
                isReturning = false;
            }
            return;
        }
        if (target == null) return;
        var dir = (target.transform.position - transform.position).normalized;
        if (Vector2.Distance(transform.position, target.transform.position) <= attackRange)
        {
            rb.velocity = Vector2.zero;
            if (timer > 0)
            {
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    target.TakeDamage(stats.stats.damage.Value, Vector2.zero, stats);
                    timer = 1f;
                }
            }
        }
        else
        {
            timer = 1f;
            rb.velocity = (dir * stats.stats.speed.Value * Time.deltaTime);
        }
    }
}
