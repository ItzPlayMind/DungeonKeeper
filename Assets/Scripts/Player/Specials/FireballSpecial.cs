using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FireballSpecial : AbstractSpecial
{
    [SerializeField] private NetworkObject fireballPrefab;
    [SerializeField] private NetworkObject combustionPrefab;
    [SerializeField] private float fireballSpeed = 5f;
    [DescriptionCreator.DescriptionVariable][SerializeField] private int manaIncrease = 10;
    [DescriptionCreator.DescriptionVariable][SerializeField] private int flamesDuration = 5;
    [DescriptionCreator.DescriptionVariable][SerializeField] private int flamesAmount = 10;
    [DescriptionCreator.DescriptionVariable][SerializeField] private int combustionDamage = 50;
    //[SerializeField] private float fireballKnockback = 35f;

    [DescriptionCreator.DescriptionVariable]
    private int explosionDamage
    {
        get
        {
            return (int)(Damage * 0.5f);
        }
    }
    Vector2 mouseWorldPos;
    private int stacks = 0;

    private int ResourceNeeded
    {
        get
        {
            int resource = HasUpgradeUnlocked(0) ? manaIncrease : 20;
            int resourcePerStack = (int)(resource * (20f / 100f));
            return resource + resourcePerStack * stacks;
        }
    }

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
        Resource -= ResourceNeeded;
        stacks = Mathf.Min(stacks + 1, 5);
        UpdateAmountText(stacks.ToString());
    }

    protected override bool HasResource()
    {
        return Resource > ResourceNeeded;
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        SpawnFireballServerRPC(OwnerClientId, dir, gameObject.layer);
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
    private void SpawnCombustionServerRpc(ulong owner, Vector3 pos, int layer)
    {
        var networkObject = Instantiate(combustionPrefab, pos, Quaternion.identity);
        networkObject.SpawnWithOwnership(owner);
        SpawnCombustionClientRpc(networkObject.NetworkObjectId, layer);
        Destroy(networkObject.gameObject, 3f);
    }

    [ClientRpc]
    private void SpawnCombustionClientRpc(ulong networkObjectId, int layer)
    {
        if (!IsLocalPlayer)
            return;
        var combustion = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId].GetComponent<CollisionSender>();
        combustion.gameObject.layer = layer;
        combustion.GetComponent<SpriteRenderer>().material = GameManager.instance.UNLIT_MATERIAL;
        combustion.onCollisionEnter += (GameObject collider, ref bool hit) =>
        {
            if (collider == gameObject)
                return;
            if (controller.TeamController.HasSameTeam(collider.gameObject)) return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats != null)
            {
                hit = true;
                DealDamage(stats, combustionDamage, Vector2.zero);
            }
        };
    }

    [ServerRpc]
    private void SpawnFireballServerRPC(ulong owner, Vector2 dir, int layer)
    {
        var networkObject = Instantiate(fireballPrefab, transform.position, Quaternion.identity);
        networkObject.transform.rotation = Quaternion.FromToRotation(networkObject.transform.right, dir);
        networkObject.SpawnWithOwnership(owner);
        SpawnFireballClientRPC(networkObject.NetworkObjectId, layer);
    }

    [ClientRpc]
    private void SpawnFireballClientRPC(ulong networkObjectId, int layer)
    {
        if (!IsLocalPlayer)
            return;
        var fireball = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId].GetComponent<Rigidbody2D>();
        fireball.gameObject.layer = layer;
        fireball.GetComponent<SpriteRenderer>().material = GameManager.instance.UNLIT_MATERIAL;
        fireball.AddForce(fireball.transform.right * fireballSpeed, ForceMode2D.Impulse);
        var fireballScript = fireball.GetComponent<Fireball>();
        fireballScript.onExplosionCollision += (GameObject collider, ref bool hit) =>
        {
            if (collider == gameObject)
                return;
            if (controller.TeamController.HasSameTeam(collider.gameObject)) return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats != null)
            {
                hit = true;
                DealDamage(stats, explosionDamage, Vector2.zero);
                var effectManager = stats.GetComponent<EffectManager>();
                if (HasUpgradeUnlocked(1))
                    effectManager?.AddEffect("flames", flamesDuration, flamesAmount, characterStats);
                if (HasUpgradeUnlocked(2) && (effectManager?.HasEffect("flames") ?? false))
                    SpawnCombustionServerRpc(OwnerClientId, stats.transform.position, gameObject.layer);
            }
        };
        fireballScript.onDirectHit += (collider) =>
        {
            if (collider == gameObject)
                return false;
            if (controller.TeamController.HasSameTeam(collider.gameObject)) return false;
            var stats = collider.GetComponent<CharacterStats>();
            if ((collider.transform.tag != "Special"))
            {
                if (stats != null)
                {
                    DealDamage(stats, Damage, Vector2.zero);
                    var effectManager = stats.GetComponent<EffectManager>();
                    if (HasUpgradeUnlocked(1))
                        effectManager?.AddEffect("flames", flamesDuration, flamesAmount, characterStats);
                    if (HasUpgradeUnlocked(2) && (effectManager?.HasEffect("flames") ?? false))
                        SpawnCombustionServerRpc(OwnerClientId, stats.transform.position, gameObject.layer);
                }
                return true;
            }
            return false;
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
        Destroy(NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].gameObject, 0.5f);
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
