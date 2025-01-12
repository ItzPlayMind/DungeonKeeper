using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffectManager : EffectManager
{
    [SerializeField] private Transform otherPlayerUIBar;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(!IsLocalPlayer)
            effectBar = otherPlayerUIBar;
    }
}
