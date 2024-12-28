using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeamInventory
{
    public static int INVENTORY_SIZE = 3;
    private Item[] items;
    private List<ulong> clientIDs = new List<ulong>();
    private System.Action<Item, int> OnAddItemToSlot;
    private System.Action<int> OnRemoveItemFromSlot;

    public void Setup(List<ulong> ids)
    {
        clientIDs = ids;
    }

    public Item GetItem(int slot)
    {
        return items[slot];
    }

    public TeamInventory()
    {
        clientIDs.Clear();
        items = new Item[INVENTORY_SIZE];
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
        RemoveItemClientRPC(slot, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = clientIDs } });
    }

    [ClientRpc]
    private void RemoveItemClientRPC(int slot, ClientRpcParams param)
    {
        var item = items[slot];
        if (item == null) return;
        items[slot] = null;
        OnRemoveItemFromSlot?.Invoke(slot);
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
        AddItemClientRPC(itemID, slot, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = clientIDs } });
    }

    [ClientRpc]
    private void AddItemClientRPC(string itemID, int slot, ClientRpcParams param)
    {
        var item = (ItemRegistry.Instance as ItemRegistry).GetByID(itemID);
        items[slot] = item;
        OnAddItemToSlot?.Invoke(item, slot);
    }
}
