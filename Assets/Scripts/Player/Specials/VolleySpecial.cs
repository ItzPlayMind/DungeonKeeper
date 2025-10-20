using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class VolleySpecial : AbstractSpecial
{
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int arrowAmount = 3;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int newArrowAmount = 5;

    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int hitAmount = 3;
    [DescriptionCreator.DescriptionVariable("green")]
    [SerializeField] private float MaxHPDamage = 2.5f;
    [DescriptionCreator.DescriptionVariable("white")]
    [SerializeField] private int InvisDuration = 3;

    [SerializeField] private NetworkObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 5f;
    [SerializeField] private float dashForce = 5f;

    Vector2 mouseWorldPos;
    Rigidbody2D rb;
    private int hits = 0;
    private ulong lastTarget;
    EffectManager effectManager;

    protected override void _Start()
    {
        base._Start();
        rb = GetComponent<Rigidbody2D>();
        if (!IsLocalPlayer) return;
        var attack = GetComponent<PlayerAttack>();
        attack.OnAttack += (ulong target, ulong user, ref int amount) =>
        {
            if (!HasUpgradeUnlocked(1)) return;
            if (target == lastTarget)
                hits++;
            else
                hits = 1;

            if(hits == hitAmount)
            {
                var targetStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<CharacterStats>();
                amount += (int)(targetStats.stats.health.Value * (MaxHPDamage / 100f));
                hits = 0;
            }
            lastTarget = target;
        };
        effectManager = GetComponent<EffectManager>();
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
    }

    protected override void _OnSpecialFinish(PlayerController controller)
    {
        SpawnArrowServerRPC(OwnerClientId, gameObject.layer, HasUpgradeUnlocked(0) ? newArrowAmount: arrowAmount);
        if(HasUpgradeUnlocked(2))
            effectManager.AddEffect("invisible", InvisDuration, 1, characterStats);
        StartCooldown();
    }

    [ServerRpc]
    private void SpawnArrowServerRPC(ulong owner, int layer, int _arrowAmount)
    {
        ulong[] ids = new ulong[_arrowAmount];
        for (int i = 0; i < _arrowAmount; i++)
        {
            var networkObject = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
            networkObject.SpawnWithOwnership(owner);
            ids[i] = networkObject.NetworkObjectId;
        }
        SpawnArrowClientRPC(ids, layer);
    }

    [ClientRpc]
    private void SpawnArrowClientRPC(ulong[] networkObjectId, int layer)
    {
        if (!IsLocalPlayer)
            return;
        mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        rb.AddForce(-dir * dashForce, ForceMode2D.Impulse);
        int arrowCount = networkObjectId.Length;
        float angleStep = 10f; // angle between each arrow
        int middleIndex = arrowCount / 2;
        int i = 0;
        foreach (var id in networkObjectId) {
            float angleOffset = (i - middleIndex) * angleStep;
            Quaternion rotation = Quaternion.AngleAxis(angleOffset, Vector3.forward);
            Vector2 finalDir = rotation * dir;
            var arrow = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].GetComponent<Rigidbody2D>();
            arrow.gameObject.layer = layer;
            arrow.GetComponent<SpriteRenderer>().material = GameManager.instance.UNLIT_MATERIAL;
            arrow.transform.rotation = Quaternion.FromToRotation(arrow.transform.right, finalDir);
            arrow.AddForce(arrow.transform.right * arrowSpeed, ForceMode2D.Impulse);
            var proj = arrow.GetComponent<Projectile>();
            proj.onCollisionEnter += (GameObject collider, ref bool hit) =>
            {
                if (collider == gameObject)
                    return;
                var stats = collider.GetComponentInParent<CharacterStats>();
                if (stats != null)
                {
                    if (controller.TeamController.HasSameTeam(stats.gameObject)) return;
                    DealDamage(stats, Damage, Vector2.zero);
                }
                if (collider.tag != "Special")
                {
                    DespawnArrowServerRPC(arrow.GetComponent<NetworkBehaviour>().NetworkObjectId);
                    hit = true;
                }
            };
            proj.OnMaxRangeReached += () =>
            {
                DespawnArrowServerRPC(arrow.GetComponent<NetworkBehaviour>().NetworkObjectId);
            };
            proj.startPos = transform.position;
            i++;
        }
        mouseWorldPos = transform.position;
    }

    [ServerRpc]
    private void DespawnArrowServerRPC(ulong objectId)
    {
        Destroy(NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].gameObject);
    }
}
