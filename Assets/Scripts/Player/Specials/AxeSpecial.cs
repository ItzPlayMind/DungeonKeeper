using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static DescriptionCreator;

public class AxeSpecial : AbstractSpecial
{
    [DescriptionVariable]
    [SerializeField] private int returnDamage = 5;
    [SerializeField] private float returnDamageMultiplier = 0.5f;
    [SerializeField] private NetworkObject axePrefab;
    [SerializeField] private float axeSpeed = 5;
    [SerializeField] private float axeReturnSpeed = 50;
    [SerializeField] private float axeDistance = 5;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int damageIncrease = 10;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int cDReduce = 2;
    Vector2 mouseWorldPos;
    Vector2 originalPos;
    [SerializeField] Rigidbody2D axe;
    bool returning = false;
    private int targetsHit = 0;

    private int ReturnDamage { get => returnDamage + (int)(characterStats.stats.specialDamage.Value * returnDamageMultiplier); }

    //public override bool CanMoveWhileUsing() => true;

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        SpawnAxeServerRPC(OwnerClientId, gameObject.layer);
    }

    protected override void _Start()
    {
        base._Start();
        if (!IsLocalPlayer) return;
        characterStats.stats.damage.ChangeValueAdd += (ref int value, int old) =>
        {
            if (HasUpgradeUnlocked(0))
            {
                value += damageIncrease;
            }
        };
    }

    [ServerRpc]
    private void SpawnAxeServerRPC(ulong owner, int layer)
    {
        var networkObject = Instantiate(axePrefab, transform.position, Quaternion.identity);
        networkObject.SpawnWithOwnership(owner);
        SpawnAxeClientRPC(networkObject.NetworkObjectId, layer);
    }

    [ClientRpc]
    private void SpawnAxeClientRPC(ulong id, int layer)
    {
        if (!IsLocalPlayer)
            return;
        var axeNetwork = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id];
        axeNetwork.GetComponentInChildren<SpriteRenderer>().material = GameManager.instance.UNLIT_MATERIAL;
        axeNetwork.gameObject.layer = layer;
        axe = axeNetwork.GetComponent<Rigidbody2D>();
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        var projectile = axe.GetComponent<Projectile>();
        projectile.onCollisionEnter += (GameObject collider, ref bool _) =>
        {
            if (collider == gameObject)
                return;
            if (collider.layer == gameObject.layer) return;
            if (collider.tag == "Special") return;
            var stats = collider.GetComponent<CharacterStats>();
            if (stats != null)
            {
                if(!returning)
                    DealDamage(stats, Damage, axe.velocity.normalized * 10);
                else
                    DealDamage(stats, ReturnDamage, axe.velocity.normalized * 3);
                targetsHit++;
                if (!HasUpgradeUnlocked(1))
                {
                    if (!returning)
                        axe.velocity = Vector2.zero;
                    returning = true;
                }
            }
            else
            {
                if (!returning)
                    axe.velocity = Vector2.zero;
                returning = true;
            }
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
                if (HasUpgradeUnlocked(2))
                {
                    Debug.Log(targetsHit * cDReduce);
                    ReduceCooldown(targetsHit * cDReduce);
                }
                targetsHit = 0;
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
        isUsing = false;
        returning = false;
        originalPos = transform.position;
        this.mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
    }
}
