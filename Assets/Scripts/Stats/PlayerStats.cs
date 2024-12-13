using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : CharacterStats
{
    [SerializeField] private Canvas playerUI;
    //[SerializeField] private GameObject hitPrefab;
    private UIBar healthBar;
    private Animator animator;
    private AnimationEventSender animatorEvent;
    private TMPro.TextMeshProUGUI healthText;

    [ClientRpc]
    protected override void TakeDamageClientRPC(int damage, Vector2 knockback, ulong damagerID)
    {
        base.TakeDamageClientRPC(damage, knockback, damagerID);
        //Destroy(Instantiate(hitPrefab,transform.position,Quaternion.identity),1f);
        healthBar?.UpdateBar(Health / (float)stats.health.Value);
        if(healthText != null )
            healthText.text = Health + "/" + stats.health.Value;
    }


    protected override void Start()
    {
        base.Start();
        if (IsLocalPlayer)
        {
            healthBar = playerUI.transform.Find("Healthbar").GetComponent<UIBar>();
            healthText = healthBar.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            healthBar.UpdateBar(1f);
            healthText.text = Health + "/" + stats.health.Value;
            stats.OnValuesChange += () =>
            {
                healthBar.UpdateBar(Health / (float)stats.health.Value);
                healthText.text = Health + "/" + stats.health.Value;
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
        respawnTime = GameManager.instance.RESPAWN_TIME.Value;
        base.Die(damagerID);
        animator.SetBool("death", true);
        GameManager.instance.Chat.AddMessage($"{damagerID} <color=red>killed</color> {NetworkObjectId}");
    }

    protected override void Respawn()
    {
        base.Respawn();
        animator.SetBool("death", false);
        if (IsLocalPlayer)
        {
            healthBar.UpdateBar(1);
            healthText.text = Health + "/" + stats.health.Value;
        }
    }

    [ClientRpc]
    protected override void HealClientRPC(int health)
    {
        base.HealClientRPC(health);
        if (IsLocalPlayer)
        {
            healthBar.UpdateBar(Health / (float)stats.health.Value);
            healthText.text = Health + "/" + stats.health.Value;
        }
    }
}
