using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HealSpecial : AbstractSpecial
{
    [SerializeField] private NetworkObject healFieldPrefab;
    [SerializeField] private float healRange;
    [SerializeField] private float healDuration;
    Vector2 mouseWorldPos;
    protected override void _OnSpecialFinish(PlayerController controller)
    {
        SpawnHealFieldServerRPC(OwnerClientId);
        StartCooldown();
    }

    protected override bool HasResource()
    {
        return Resource >= 50;
    }

    protected override void RemoveResource()
    {
        Resource -= 50;
    }

    protected override Dictionary<string, object> GetVariablesForDescription()
    {
        var variables = base.GetVariablesForDescription();
        variables.Add("HealDuration", healDuration);
        return variables;
    }

    [ServerRpc]
    private void SpawnHealFieldServerRPC(ulong owner)
    {
        var networkObject = Instantiate(healFieldPrefab, transform.position, Quaternion.identity);
        networkObject.SpawnWithOwnership(owner);
        SpawnHealFieldClientRPC(networkObject.NetworkObjectId);
        var heal = networkObject.GetComponent<HealField>();
        heal.SetHealAmount(Damage);
        heal.SetController(GetComponent<PlayerController>());
    }

    [ClientRpc]
    private void SpawnHealFieldClientRPC(ulong id)
    {
        var healFieldNetwork = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id];
        healFieldNetwork.gameObject.layer = gameObject.layer;
        if (!IsLocalPlayer)
            return;
        healFieldNetwork.GetComponentInChildren<SpriteRenderer>().material = GameManager.instance.UNLIT_MATERIAL;
        Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
        if (Vector2.Distance(transform.position,mouseWorldPos) > healRange)
            healFieldNetwork.transform.position = (Vector2)transform.position + dir * healRange;
        else
            healFieldNetwork.transform.position = mouseWorldPos;
        DespawnHealFieldServerRPC(id);
    }


    [ServerRpc]
    private void DespawnHealFieldServerRPC(ulong id)
    {
        Destroy(NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].gameObject, healDuration);
    }

    protected override void _OnSpecialPress(PlayerController controller)
    {
        Use();
        this.mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
    }

    private float timer = 0f;

    protected override void _Update()
    {
        if (IsLocalPlayer)
        {
            if (timer <= 0f)
            {
                if (Resource < resourceAmount)
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
