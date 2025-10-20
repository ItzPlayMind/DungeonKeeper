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
        Vector3 offset = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
        var numberPrefab = Instantiate(damageNumberPrefab, pos + offset, Quaternion.identity);
        numberPrefab.setNumber(number, color);
    }

    public void SetTorch(Vector2 pos)
    {
        Instantiate(torchPrefab, pos, Quaternion.identity).Spawn();
    }
}
