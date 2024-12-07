using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AxeSpecial : AbstractSpecial
{
    [SerializeField] private int returnDamage = 5;
    [SerializeField] private float returnDamageMultiplier = 0.5f;
    [SerializeField] private NetworkObject axePrefab;
    [SerializeField] private float axeSpeed = 5;
    [SerializeField] private float axeReturnSpeed = 50;
    [SerializeField] private float axeDistance = 5;
    Vector2 mouseWorldPos;
    Vector2 originalPos;
    [SerializeField] Rigidbody2D axe;
    bool returning = false;
    protected override void _OnSpecialFinish(PlayerController controller)
    {
        SpawnAxeServerRPC(OwnerClientId, gameObject.layer);
    }

    [ServerRpc]
    private void SpawnAxeServerRPC(ulong owner, int layer)
    {
        var networkObject = Instantiate(axePrefab, transform.position, Quaternion.identity);
        networkObject.SpawnWithOwnership(owner);
        networkObject.gameObject.layer = layer;
        SpawnAxeClientRPC(networkObject.NetworkObjectId);
    }

    [ClientRpc]
    private void SpawnAxeClientRPC(ulong id)
    {
        if (!IsLocalPlayer)
            return;
        var axeNetwork = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id];
        axeNetwork.GetComponentInChildren<SpriteRenderer>().material = GameManager.instance.UNLIT_MATERIAL;
        axe = axeNetwork.GetComponent<Rigidbody2D>();
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        axe.GetComponent<Projectile>().onCollisionEnter += (GameObject collider) =>
        {
            if (collider == gameObject)
                return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats != null)
            {
                if(!returning)
                    stats.TakeDamage(Damage, axe.velocity.normalized*10, NetworkManager.Singleton.LocalClientId);
                else
                    stats.TakeDamage(returnDamage + (int)(characterStats.stats.specialDamage.Value * returnDamageMultiplier), axe.velocity.normalized * 3, NetworkManager.Singleton.LocalClientId);
            }
            if (!returning)
                axe.velocity = Vector2.zero;
            returning = true;
        };
        axe.AddForce(dir * axeSpeed, ForceMode2D.Impulse);
    }

    protected override void _Update()
    {
        if (!IsLocalPlayer)
            return;
        if (axe == null)
            return;
        if (Vector2.Distance(axe.transform.position, originalPos) > axeDistance)
        {
            returning = true;
            axe.velocity = Vector2.zero;
        }
        if (returning)
        {
            var dir = (transform.position - axe.transform.position).normalized;
            axe.velocity = dir * axeReturnSpeed;
            if (Vector2.Distance(axe.transform.position, transform.position) <= 0.2)
            {
                StartCooldown();
                DespawnAxeServerRPC(axe.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }
    }

    [ServerRpc]
    private void DespawnAxeServerRPC(ulong id)
    {
        Destroy(NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].gameObject);
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
        returning = false;
        originalPos = transform.position;
        this.mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
    }
}
