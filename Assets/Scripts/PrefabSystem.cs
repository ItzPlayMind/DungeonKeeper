using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PrefabSystem : MonoBehaviour
{
    [SerializeField] private DamageNumber damageNumberPrefab;
    [SerializeField] private NetworkObject torchPrefab;

    public void SpawnDamageNumber(Vector3 pos, int number, Color color)
    {
        Instantiate(damageNumberPrefab, pos, Quaternion.identity).setNumber(number, color);
    }

    public void SetTorch(Vector2 pos)
    {
        Instantiate(torchPrefab, pos, Quaternion.identity).Spawn();
    }
}
