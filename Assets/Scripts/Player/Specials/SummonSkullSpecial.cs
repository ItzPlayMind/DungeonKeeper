using Cinemachine.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SummonSkullSpecial : AbstractSpecial
{
    [SerializeField] private float commandRange = 2f;
    [SerializeField] private NetworkObject skullPrefab;
    [SerializeField] private float knockBackForce = 5f;

    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private float newCommandRange = 3;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int curseAmount = 50;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int curseDuration = 3;

    private List<Skeleton> activeMinions = new List<Skeleton>();
    private int activeMinionIndex = 0;

    public float CommandRange { get => HasUpgradeUnlocked(0) ? newCommandRange : commandRange; }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
        StartCooldown();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(IsOwner)
            SpawnSkullServerRPC(transform.position, OwnerClientId, gameObject.layer);
    }

    protected override void _Start()
    {
        base._Start();
        if (!IsLocalPlayer) return;
    }
    protected override void _UpdateAll()
    {
        base._UpdateAll();
        if (!IsServer && activeMinions.Count <= 0) return;
        foreach (var minion in activeMinions)
        {
            if (Vector3.Distance(minion.transform.position, transform.position) > CommandRange + 1)
            {
                minion.Teleport(transform.position);
            }
        }
    }

    protected override void OnUpgradeUnlocked(int index)
    {
        base.OnUpgradeUnlocked(index);
        if (index == 2)
        {
            SpawnSkullServerRPC(transform.position, OwnerClientId, gameObject.layer);
        }
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        if (Vector2.Distance(transform.position, mouseWorldPos) > CommandRange)
            mouseWorldPos = (Vector2)transform.position + dir * CommandRange;
        SetDestinationServerRPC(mouseWorldPos);
    }

    [ServerRpc]
    private void SetDestinationServerRPC(Vector2 pos)
    {
        var minion = activeMinions[activeMinionIndex];
        if (!minion.Spawned) return;
        minion.SetDestination(pos);
        activeMinionIndex = (activeMinionIndex + 1) % activeMinions.Count;
    }

    [ServerRpc]
    private void SpawnSkullServerRPC(Vector2 position, ulong owner, int layer)
    {
        var networkObject = Instantiate(skullPrefab, position, Quaternion.identity);
        networkObject.SpawnWithOwnership(owner);
        networkObject.gameObject.layer = layer;
        var skeleton = networkObject.GetComponent<Skeleton>();
        activeMinions.Add(skeleton);
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
                if (controller.TeamController.HasSameTeam(stats.gameObject)) return;
                DealDamage(stats, Damage, stats.GenerateKnockBack(stats.transform,sender.transform,knockBackForce));
                if(HasUpgradeUnlocked(1))
                    stats.GetComponent<EffectManager>()?.AddEffect("curse", curseDuration, curseAmount, characterStats);
            }
        };
    }
}
