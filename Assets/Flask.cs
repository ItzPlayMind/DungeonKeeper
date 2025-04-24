using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Flask : NetworkBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        var effectManager = collision.GetComponent<EffectManager>();
        if (effectManager != null)
        {
            effectManager.AddEffect("potion", 5, 100, collision.GetComponent<CharacterStats>());
            Destroy(gameObject);
        }
    }
}
