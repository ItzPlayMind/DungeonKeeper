using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArrowSpecial : AbstractSpecial
{
    [SerializeField] private NetworkObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 5f;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int maxStackCount = 3;

    [DescriptionCreator.DescriptionVariable]
    private int DamageStacks { get => Damage + DamagePerStack * stacks; }
    [DescriptionCreator.DescriptionVariable]
    private int DamagePerStack { get => Damage / maxStackCount; }

    private int stacks = 0;
    Vector2 mouseWorldPos;

    public override bool canUse()
    {
        return HasResource();
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
    }

    protected override void _Start()
    {
        base._Start();
        if (!IsLocalPlayer) return;
        characterStats.stats.resource.ChangeValueAdd += (ref int value, int old) =>
        {
            if (HasUpgradeUnlocked(0)) value += 2;
        };
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        StartCoroutine(SpawnArrows());
        Finish();
    }

    private IEnumerator SpawnArrows()
    {
        SpawnArrowServerRPC(OwnerClientId, gameObject.layer);
        yield return new WaitForSeconds(0.1f);
        if(HasUpgradeUnlocked(2)) SpawnArrowServerRPC(OwnerClientId, gameObject.layer);
    }

    protected override void FinishedCooldown()
    {
        Resource++;
        if (Resource < characterStats.stats.resource.Value)
            StartCooldown();
    }

    protected override void _Update()
    {
        if(Resource < characterStats.stats.resource.Value && !OnCooldown)
        {
            StartCooldown();
        }
    }

    [ServerRpc]
    private void SpawnArrowServerRPC(ulong owner, int layer)
    {
        var networkObject = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
        networkObject.SpawnWithOwnership(owner);
        SpawnArrowClientRPC(networkObject.NetworkObjectId, layer);
    }

    [ClientRpc]
    private void SpawnArrowClientRPC(ulong networkObjectId, int layer)
    {
        if (!IsLocalPlayer)
            return;
        mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        var arrow = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId].GetComponent<Rigidbody2D>();
        arrow.gameObject.layer = layer;
        arrow.GetComponent<SpriteRenderer>().material = GameManager.instance.UNLIT_MATERIAL;
        arrow.transform.rotation = Quaternion.FromToRotation(arrow.transform.right,dir);
        arrow.AddForce(arrow.transform.right * (arrowSpeed + (HasUpgradeUnlocked(1) ? 4 : 0)), ForceMode2D.Impulse);
        var proj = arrow.GetComponent<Projectile>();
        proj.onCollisionEnter += (GameObject collider, ref bool hit) =>
        {
            bool hasHitTarget = false;
            if (collider == gameObject)
                return;
            var stats = collider.GetComponentInParent<CharacterStats>();
            if (stats != null)
            {
                if (controller.TeamController.HasSameTeam(stats.gameObject)) return;
                DealDamage(stats, DamageStacks, Vector2.zero);
                stacks = Mathf.Clamp(stacks+1,0,maxStackCount);
                UpdateAmountText(stacks.ToString());
                hasHitTarget = true;
            }
            if (collider.tag != "Special")
            {
                DespawnArrowServerRPC(arrow.GetComponent<NetworkBehaviour>().NetworkObjectId);
                hit = true;
                if (!hasHitTarget)
                {
                    stacks = 0;
                    UpdateAmountText("");
                }
            }
        };
        proj.OnMaxRangeReached += () =>
        {
            DespawnArrowServerRPC(arrow.GetComponent<NetworkBehaviour>().NetworkObjectId);
            stacks = 0;
            UpdateAmountText("");
        };
        proj.startPos = transform.position;
        mouseWorldPos = transform.position;
    }

    [ServerRpc]
    private void DespawnArrowServerRPC(ulong objectId)
    {
        Destroy(NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].gameObject);
    }
}
