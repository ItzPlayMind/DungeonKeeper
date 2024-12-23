using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerProjectileAttack : PlayerRangeAttack
{
    public float projectileSpeed = 3f;

    private bool isPiercing;

    public void SetPiercing(bool isPiercing) => this.isPiercing = isPiercing;

    protected override Vector3 GetProjectileSpawnPosition(Vector3 mousePos)
    {
        return transform.position;
    }

    protected override void OnProjectileSpawn(NetworkObject obj, Vector3 mousePos)
    {
        obj.GetComponent<Rigidbody2D>().AddForce(obj.transform.right*projectileSpeed,ForceMode2D.Impulse);
    }

    protected override void OnAttackHit(NetworkObject obj, GameObject collider)
    {
        var stats = collider.GetComponent<CharacterStats>();
        if (((stats == null && isPiercing) || !isPiercing) && collider.tag != "Special")
        {
            rb.velocity = Vector3.zero;
            DestroyServerRPC(obj.NetworkObjectId);
        }
    }
}
