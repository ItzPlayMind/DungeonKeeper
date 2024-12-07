using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HealSpecial : AbstractSpecial
{
    [SerializeField] private NetworkObject healFieldPrefab;
    [SerializeField] private float healRange;
    [SerializeField] private float healDuration;
    Vector2 mouseWorldPos;
    List<GameObject> alreadyHealed = new List<GameObject>();
    protected override void _OnSpecialFinish(PlayerController controller)
    {
        SpawnHealFieldServerRPC(OwnerClientId);
        StartCooldown();
    }

    [ServerRpc]
    private void SpawnHealFieldServerRPC(ulong owner)
    {
        var networkObject = Instantiate(healFieldPrefab, transform.position, Quaternion.identity);
        networkObject.SpawnWithOwnership(owner);
        SpawnHealFieldClientRPC(networkObject.NetworkObjectId);
    }

    [ClientRpc]
    private void SpawnHealFieldClientRPC(ulong id)
    {
        if (!IsLocalPlayer)
            return;
        var healFieldNetwork = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id];
        healFieldNetwork.GetComponentInChildren<SpriteRenderer>().material = GameManager.instance.UNLIT_MATERIAL;
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        if (Vector2.Distance(transform.position,mouseWorldPos) > healRange)
            healFieldNetwork.transform.position = (Vector2)transform.position + dir * healRange;
        else
            healFieldNetwork.transform.position = mouseWorldPos;
        healFieldNetwork.GetComponent<CollisionSender>().onCollisionEnter += (GameObject collider) =>
        {
            if (collider == gameObject)
                return;
            if (collider.layer != gameObject.layer)
                return;
            if (alreadyHealed.Contains(collider))
                return;
            alreadyHealed.Add(collider);
            var stats = collider.GetComponent<CharacterStats>();
            if (stats != null)
            {
                stats.Heal(Damage);
            }
        };
        DespawnHealFieldServerRPC(id);
    }


    [ServerRpc]
    private void DespawnHealFieldServerRPC(ulong id)
    {
        Destroy(NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].gameObject, healDuration);
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
        alreadyHealed.Clear();
        this.mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
    }
}
