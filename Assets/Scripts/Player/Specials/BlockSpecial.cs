using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BlockSpecial : AbstractSpecial
{
    [SerializeField] private float knockBackForce = 35;
    private bool isBlocking = false;
    protected override void _Start()
    {
        if (!IsLocalPlayer) return;
        characterStats.stats.damageReduction.ConstraintValue += ChangeDamageReduction;
        characterStats.OnClientTakeDamage += (ulong damager, int damage) =>
        {
            if (isBlocking)
            {
                var enemy = NetworkManager.Singleton.SpawnManager.SpawnedObjects[damager].GetComponent<CharacterStats>();
                enemy.TakeDamage(damage, enemy.GenerateKnockBack(enemy.transform, transform, knockBackForce),characterStats);
            }
        };
    }
    protected override void _OnSpecialFinish(PlayerController controller)
    {
        isBlocking = false;
        StartCooldown();
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        isBlocking = true;
        Use();
    }

    private void ChangeDamageReduction(ref int newValue, int value)
    {
        if(isBlocking)
            newValue = 100;
    }
}
