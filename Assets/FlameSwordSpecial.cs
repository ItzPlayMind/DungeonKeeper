using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameSwordSpecial : KnockBackSpecial
{
    protected override void _Start()
    {
        base._Start();
        if (!IsLocalPlayer) return;
        GetComponent<PlayerAttack>().OnAttack += (ulong target, ulong user, ref int amount) =>
        {
            var manager = Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<EffectManager>();
            if(manager != null)
            {
                if (manager.HasEffect("flames"))
                {
                    manager.AddEffect("flames", duration, this.amount, characterStats);
                }
            }
        };
    }
}
