using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ObjectiveStats : CharacterStats
{
    [SerializeField] private bool showDeathInChat = true;
    private Animator animator;

    protected override void Start()
    {
        base.Start();
        animator = GetComponentInChildren<Animator>();
    }

    protected override void Die(ulong damagerID)
    {
        base.Die(damagerID);
        if(showDeathInChat)
            GameManager.instance.Chat.AddMessage($"{damagerID} <color=red>killed</color> {gameObject.name}");
    }

    [ClientRpc]
    protected override void TakeDamageClientRPC(int damage, Vector2 knockback, ulong damagerID)
    {
        base.TakeDamageClientRPC(damage, knockback, damagerID);
    }
}
