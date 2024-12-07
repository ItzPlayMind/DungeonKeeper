using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArrowSpecial : AbstractSpecial
{
    [SerializeField] private int arrowAmount = 3;
    [SerializeField] private NetworkObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 5f;

    private int currentArrowAmount;
    Vector2 mouseWorldPos;
    protected override void _Start()
    {
        currentArrowAmount = arrowAmount;
        UpdateAmountText(currentArrowAmount.ToString());
    }

    public override bool canUse()
    {
        return currentArrowAmount > 0;
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        currentArrowAmount--;
        UpdateAmountText(currentArrowAmount.ToString());
        SpawnArrowServerRPC(OwnerClientId,gameObject.layer);
    }

    protected override void FinishedCooldown()
    {
        currentArrowAmount++;
        UpdateAmountText(currentArrowAmount.ToString());
        if (currentArrowAmount < arrowAmount)
        {
            StartCooldown();
        }
    }

    protected override void _Update()
    {
        if(currentArrowAmount < arrowAmount && !OnCooldown)
        {
            StartCooldown();
        }
    }

    [ServerRpc]
    private void SpawnArrowServerRPC(ulong owner, int layer)
    {
        var networkObject = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
        networkObject.gameObject.layer = layer;
        networkObject.SpawnWithOwnership(owner);
        SpawnArrowClientRPC(networkObject.NetworkObjectId);
    }

    [ClientRpc]
    private void SpawnArrowClientRPC(ulong networkObjectId)
    {
        if (!IsLocalPlayer)
            return;
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        var arrow = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId].GetComponent<Rigidbody2D>();
        arrow.GetComponent<SpriteRenderer>().material = GameManager.instance.UNLIT_MATERIAL;
        arrow.transform.rotation = Quaternion.FromToRotation(arrow.transform.right,dir);
        arrow.AddForce(arrow.transform.right * arrowSpeed, ForceMode2D.Impulse);
        arrow.GetComponent<Projectile>().onCollisionEnter += (collider) =>
        {
            if (collider == gameObject)
                return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats != null)
            {
                stats.TakeDamage(Damage, Vector2.zero, NetworkManager.Singleton.LocalClientId);
            }
            DespawnArrowServerRPC(arrow.GetComponent<NetworkBehaviour>().NetworkObjectId);
        };
        mouseWorldPos = transform.position;
    }

    [ServerRpc]
    private void DespawnArrowServerRPC(ulong objectId)
    {
        Destroy(NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].gameObject);
    }
}
