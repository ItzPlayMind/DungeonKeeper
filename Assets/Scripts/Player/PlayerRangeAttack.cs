using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerRangeAttack : PlayerAttack
{
    public NetworkObject projectile;
    [SerializeField] private float attackRange = 2;
    [SerializeField] private bool setRotation = false;
    [SerializeField] private float knockBackForce = 0;

    

    protected override void OnAttackEnd()
    {
    }

    protected override void OnAttackTriggered()
    {
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnProjectileServerRPC(Vector2 mousePos, int layer)
    {
        Vector2 dir = (mousePos - (Vector2)transform.position).normalized;
        var rotation = Quaternion.FromToRotation(projectile.transform.right, dir);
        var obj = Instantiate(projectile, GetProjectileSpawnPosition(mousePos), setRotation? rotation: Quaternion.identity);
        obj.Spawn();
        OnProjectileSpawn(obj,mousePos);
        SpawnProjectileClientRPC(obj.NetworkObjectId, layer);
        Destroy(obj.gameObject, 3f);
    }

    protected virtual Vector3 GetProjectileSpawnPosition(Vector3 mousePos)
    {
        if (Vector3.Distance(mousePos, transform.position) <= attackRange)
            return mousePos;
        else
        {
            var dir = (mousePos - transform.position).normalized;
            return transform.position + dir * attackRange;
        }
    }

    protected virtual void OnProjectileSpawn(NetworkObject obj, Vector3 mousePos) {
        
    }

    [ClientRpc]
    private void SpawnProjectileClientRPC(ulong id, int layer)
    {
        var obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id];
        obj.gameObject.layer = layer;
        if (!IsLocalPlayer) return;
        obj.GetComponent<CollisionSender>().onCollisionEnter += (collider) =>
        {
            if (collider == gameObject)
                return;
            if (collider.gameObject.layer == gameObject.layer) return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats != null && !stats.IsDead)
            {
                
                var damage = (int)(this.stats.stats.damage.Value);
                OnAttack?.Invoke(stats.NetworkObjectId, this.stats.NetworkObjectId, ref damage);
                stats.TakeDamage(damage, stats.GenerateKnockBack(stats.transform, transform, knockBackForce), this.stats);
            }
            OnAttackHit(obj,collider);
        };
        var proj = obj.GetComponent<Projectile>();
        if (proj == null) return;
        proj.startPos = obj.transform.position;
        proj.OnMaxRangeReached += () =>
        {
            DestroyServerRPC(obj.NetworkObjectId);
        };
    }

    [ServerRpc(RequireOwnership = false)]
    protected void DestroyServerRPC(ulong id)
    {
        Destroy(NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].gameObject);
    }

    protected virtual void OnAttackHit(NetworkObject obj, GameObject collider) { }

    protected override void OnSelfKnockback()
    {
        Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
        SpawnProjectileServerRPC(worldMousePos, gameObject.layer);
    }

    protected override void _Update()
    {
    }
}
