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
        characterStats = GetComponent<CharacterStats>();
        GetComponent<CharacterStats>().OnTakeDamage += (ulong damager, int damage) =>
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
        StartCooldown();
        if (IsLocalPlayer)
            characterStats.stats.damageReduction.ChangeValue -= ChangeDamageReduction; 
        isBlocking = false;
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
        if(IsLocalPlayer)
            characterStats.stats.damageReduction.ChangeValue += ChangeDamageReduction;
        isBlocking = true;
    }

    private void ChangeDamageReduction(ref float newValue, float value)
    {
        newValue = 100;
    }
}
