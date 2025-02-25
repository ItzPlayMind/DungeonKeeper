using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FireballSpecial : AbstractSpecial
{
    [SerializeField] private NetworkObject fireballPrefab;
    [SerializeField] private float fireballSpeed = 5f;
    //[SerializeField] private float fireballKnockback = 35f;

    Vector2 mouseWorldPos;
    private int stacks = 0;

    protected override void _Start()
    {
        base._Start();
        if (!IsLocalPlayer) return;
        GetComponent<PlayerAttack>().OnAttack += (ulong _, ulong _, ref int damage) =>
        {
            Resource += 20;
        };
    }

    protected override void RemoveResource()
    {
        Resource -= 20 + (4 * stacks);
        stacks = Mathf.Min(stacks + 1, 5);
        UpdateAmountText(stacks.ToString());
    }

    protected override bool HasResource()
    {
        return Resource > 20 + (4*stacks);
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        SpawnFireballServerRPC(OwnerClientId, gameObject.layer);
        StartActive();
        Finish();
    }

    protected override void OnActiveOver()
    {
        stacks = 0;
        UpdateAmountText("");
        StartCooldown();
    }

    [ServerRpc]
    private void SpawnFireballServerRPC(ulong owner, int layer)
    {
        var networkObject = Instantiate(fireballPrefab, transform.position, Quaternion.identity);
        networkObject.SpawnWithOwnership(owner);
        SpawnFireballClientRPC(networkObject.NetworkObjectId, layer);
    }

    [ClientRpc]
    private void SpawnFireballClientRPC(ulong networkObjectId, int layer)
    {
        if (!IsLocalPlayer)
            return;
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        var fireball = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId].GetComponent<Rigidbody2D>();
        fireball.gameObject.layer = layer;
        fireball.GetComponent<SpriteRenderer>().material = GameManager.instance.UNLIT_MATERIAL;
        fireball.transform.rotation = Quaternion.FromToRotation(fireball.transform.right, dir);
        fireball.AddForce(fireball.transform.right * fireballSpeed, ForceMode2D.Impulse);
        var fireballScript = fireball.GetComponent<Fireball>();
        fireballScript.onExplosionCollision += (GameObject collider, ref bool hit) =>
        {
            if (collider == gameObject)
                return;
            if (collider.layer == gameObject.layer) return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats != null)
            {
                hit = true;
                stats.TakeDamage(Damage+(int)((stacks - 1) * (Damage/5f)), Vector2.zero, characterStats);
            }
        };
        fireballScript.onDirectHit += (collider) =>
        {
            if (collider == gameObject)
                return;
            if (collider.layer == gameObject.layer) return;
            var stats = collider.GetComponent<CharacterStats>();
            if ((collider.transform.tag != "Special"))
            {
                if (stats != null)
                {
                    stats.TakeDamage(Damage + (int)((stacks - 1) * (Damage / 5f)), Vector2.zero, characterStats);
                }
            }
        };
        fireballScript.onExplosion += () =>
        {
            DespawnArrowServerRPC(fireball.GetComponent<NetworkBehaviour>().NetworkObjectId);
        };
        mouseWorldPos = transform.position;
    }

    [ServerRpc]
    private void DespawnArrowServerRPC(ulong objectId)
    {
        Destroy(NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].gameObject,0.5f);
    }

    private float timer = 0f;

    protected override void _Update()
    {
        if (IsLocalPlayer)
        {
            if (timer <= 0f)
            {
                if (Resource < characterStats.stats.resource.Value)
                {
                    Resource += 2;
                }
                timer = 1f;
            }
            else
                timer -= Time.deltaTime;
        }
    }
}
