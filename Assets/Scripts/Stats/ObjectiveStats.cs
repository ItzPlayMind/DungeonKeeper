using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ObjectiveStats : CharacterStats
{
    [SerializeField] private bool showDeathInChat = true;

    protected override void Die(ulong damagerID)
    {
        base.Die(damagerID);
        if(showDeathInChat)
            GameManager.instance.Chat.AddMessage($"{damagerID} <color=red>killed</color> {gameObject.name}");
    }
}
