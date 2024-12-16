using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class Inventory : NetworkBehaviour
{
    public static int INVENTORY_SIZE = 6;
    [SerializeField] private NetworkVariable<int> cash = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private UIIconBar[] inventorySlots = new UIIconBar[INVENTORY_SIZE];
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Sprite emptySlot;
    private Item[] items;
    public int Cash { get => cash.Value; }

    public Item GetItem(int slot)
    {
        return items[slot];
    }

    public void OnCashChange(System.Action<int> callback)
    {
        cash.OnValueChanged += (_, value) => callback?.Invoke(value);
    }

    public void AddCash(int cash)
    {
        AddCashServerRPC(cash);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddCashServerRPC(int cash)
    {
        this.cash.Value += cash;
    }

    public void RemoveCash(int cash)
    {
        RemoveCashServerRPC(cash);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveCashServerRPC(int cash)
    {
        this.cash.Value -= cash;
    }

    private CharacterStats stats;

    public void UseItem(int slot)
    {
        if (items.Length <= slot) return;
        if (items[slot] == null) return;
        if (items[slot].CanUse)
            items[slot].Use(stats, slot);
    }

    public override void OnNetworkSpawn()
    {
        items = new Item[INVENTORY_SIZE];
        stats = GetComponent<CharacterStats>();
    }

    public bool CanAddItem
    {
        get
        {
            for (int i = 0; i < items.Length; i++)
                if (items[i] == null) return true;
            return false;
        }
    }

    public void RemoveItem(int slot)
    {
        RemoveItemServerRPC(slot);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveItemServerRPC(int slot)
    {
        RemoveItemClientRPC(slot);
    }

    [ClientRpc]
    private void RemoveItemClientRPC(int slot)
    {
        var item = items[slot];
        if (item == null) return;
        items[slot] = null;
        if (IsLocalPlayer)
        {
            inventorySlots[slot].Icon = emptySlot;
            inventorySlots[slot].UpdateBar(0f);
        }
        item?.OnUnequip(stats, slot);
    }

    public void AddItem(Item item)
    {
        int index;
        for (index = 0; index < INVENTORY_SIZE; index++)
        {
            if (items[index] == null)
                break;
        };
        AddItemServerRPC(item.ID, index);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddItemServerRPC(string itemID, int slot)
    {
        AddItemClientRPC(itemID,slot);
    }

    [ClientRpc]
    private void AddItemClientRPC(string itemID, int slot)
    {
        var item = ItemRegistry.Instance.GetItemById(itemID);
        items[slot] = item;
        if (IsLocalPlayer)
            inventorySlots[slot].Icon = item.icon;
        item?.OnEquip(stats, slot);
    }

    private float goldTimer = 1f;
    private void Update()
    {
        if (!IsOwner) return;
        goldTimer -= Time.deltaTime;
        if (goldTimer < 0f)
        {
            AddCash(GameManager.instance.GOLD_PER_SECOND);
            goldTimer = 1f;
        }
    }

    public void UpdateItems()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;
            items[i].Update(stats, i);
            inventorySlots[i].UpdateBar(items[i].CurrentCooldownDelta);
        }
    }

    public void SwapItems(int src, int dest)
    {
        SwapItemsServerRPC(src, dest);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwapItemsServerRPC(int src, int dest)
    {
        SwapItemsClientRPC(src, dest);
    }

    [ClientRpc]
    private void SwapItemsClientRPC(int src, int dest)
    {
        var oldItem = items[src];
        items[src] = items[dest];
        items[dest] = oldItem;
        if (IsLocalPlayer)
        {
            inventorySlots[src].UpdateBar(0f);
            inventorySlots[src].Icon = items[src] != null ? items[src].icon : emptySlot;
            inventorySlots[dest].Icon = items[dest] != null ? items[dest].icon : emptySlot;
        }
    }
}
