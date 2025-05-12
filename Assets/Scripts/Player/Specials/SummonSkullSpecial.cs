using Cinemachine.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SummonSkullSpecial : AbstractSpecial
{
    [SerializeField] private float spawnRange = 3f;
    [SerializeField] private NetworkObject skullPrefab;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int hitsNeededForResource = 5;
    [SerializeField] private float knockBackForce = 5f;

    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int newHitsNeeded = 3;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int curseAmount = 50;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int curseDuration = 3;

    private int hits = 0;
    private ulong target = 0;
    public override bool canUse()
    {
        return HasResource() && target != 0;
    }

    protected override bool HasResource()
    {
        return Resource > 0;
    }

    protected override void RemoveResource()
    {
        Resource--;
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
        StartCooldown();
    }

    protected override void _Start()
    {
        base._Start();
        if (!IsLocalPlayer) return;
        var playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.OnAttack += (ulong damager, ulong client, ref int amount) =>
            {
                this.target = damager;
                hits++;
                if(hits == (HasUpgradeUnlocked(0)? newHitsNeeded : hitsNeededForResource))
                {
                    Resource++;
                    hits = 0;
                }
            };
        }
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        if (Vector2.Distance(transform.position, mouseWorldPos) > spawnRange)
            mouseWorldPos = (Vector2)transform.position + dir * spawnRange;
        SpawnSkullServerRPC(mouseWorldPos,OwnerClientId, target, gameObject.layer);
        if (HasUpgradeUnlocked(2))
            SpawnSkullServerRPC(transform.position, OwnerClientId, target, gameObject.layer);
    }

    [ServerRpc]
    private void SpawnSkullServerRPC(Vector2 position, ulong owner, ulong target, int layer)
    {
        CharacterStats targetStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<CharacterStats>();
        var networkObject = Instantiate(skullPrefab, position, Quaternion.identity);
        networkObject.SpawnWithOwnership(owner);
        networkObject.gameObject.layer = layer;
        var skeleton = networkObject.GetComponent<Skeleton>();
        skeleton.SetTarget(targetStats);
        SpawnSkullClientRPC(networkObject.NetworkObjectId, layer);
    }

    [ClientRpc]
    private void SpawnSkullClientRPC(ulong networkObjectId, int layer)
    {
        if (!IsLocalPlayer)
            return;
        var spear = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
        spear.gameObject.layer = layer;
        var sender = spear.GetComponentInChildren<CollisionSender>();
        sender.onCollisionEnter += (GameObject collider, ref bool hit) =>
        {
            if (collider == gameObject)
                return;
            var stats = collider.GetComponentInParent<CharacterStats>();
            if (stats != null)
            {
                if (stats.gameObject.layer == gameObject.layer) return;
                DealDamage(stats, Damage, stats.GenerateKnockBack(stats.transform,sender.transform,knockBackForce));
                if(HasUpgradeUnlocked(1))
                    stats.GetComponent<EffectManager>()?.AddEffect("curse", curseDuration, curseAmount, characterStats);
            }
        };
    }
}
