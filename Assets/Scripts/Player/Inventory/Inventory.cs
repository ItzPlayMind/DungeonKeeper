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
    [SerializeField] private UIIconBar[] teamInventorySlots = new UIIconBar[3];
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Sprite emptySlot;

    private PlayerItemShow playerItemShow;
    private Item[] items;
    private Item[] teamItems;
    public int Cash { get => cash.Value; }

    public Item GetItem(int slot, bool team = false)
    {
        return (team ? teamItems : items)[slot];
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
#if UNITY_EDITOR
        if(IsServer)
            cash.Value += 20000;
#endif
        items = new Item[INVENTORY_SIZE];
        teamItems = new Item[3];
        stats = GetComponent<CharacterStats>();
    }

    public void OnTeamAssigned()
    {
        if(!IsLocalPlayer)
            playerItemShow = ShopPanel.Instance.CreatePlayerItemsShow(GameManager.instance.PlayerStatistics.GetNameByClientID(OwnerClientId));
    }

    public bool CanAddItem(bool team = false)
    {
        var items = team ? teamItems : this.items;
        for (int i = 0; i < items.Length; i++)
            if (items[i] == null) return true;
        return false;
    }

    public void RemoveItem(int slot, bool team = false)
    {
        RemoveItemServerRPC(slot,team);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveItemServerRPC(int slot, bool team)
    {
        RemoveItemClientRPC(slot,team);
    }

    [ClientRpc]
    private void RemoveItemClientRPC(int slot, bool team)
    {
        var slots = team ? this.teamInventorySlots: inventorySlots;
        var items = team ? teamItems : this.items;
        var item = items[slot];
        if (item == null) return;
        items[slot] = null;
        if (IsLocalPlayer)
        {
            slots[slot].Icon = emptySlot;
            slots[slot].UpdateBar(0f);
        }
        else
        {
            playerItemShow?.SetItemInSlot(emptySlot, slot);
        }
        if(!team)
            item?.OnUnequip(stats, slot);
    }

    public void AddItem(Item item, bool team = false)
    {
        int index;
        var items = team ? this.teamItems : this.items;
        for (index = 0; index < (team ? 3 : INVENTORY_SIZE); index++)
        {
            if (items[index] == null)
                break;
        };
        AddItemServerRPC(item.ID, index, team);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddItemServerRPC(string itemID, int slot, bool team)
    {
        AddItemClientRPC(itemID,slot,team);
    }

    [ClientRpc]
    private void AddItemClientRPC(string itemID, int slot, bool team)
    {
        var item = (ItemRegistry.Instance as ItemRegistry).GetByID(itemID);
        (team ? teamItems : items)[slot] = item;
        if (IsLocalPlayer)
            (team ? teamInventorySlots : inventorySlots)[slot].Icon = item.icon;
        else
            playerItemShow?.SetItemInSlot(item.icon, slot);
        if (!team)
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

    public void SwapItems(int src, int dest, bool team = false)
    {
        SwapItemsServerRPC(src, dest,team);
    }

    public void SwapItemsForTeamFromPlayer(int src, int dest)
    {
        SwapItemsForTeamFromPlayerServerRPC(src, dest);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwapItemsForTeamFromPlayerServerRPC(int src, int dest)
    {
        GameManager.instance.SwapItemsForTeamFromPlayer(NetworkObjectId,src,dest);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwapItemsServerRPC(int src, int dest, bool team)
    {
        SwapItemsClientRPC(src, dest,team);
    }

    [ClientRpc]
    private void SwapItemsClientRPC(int src, int dest, bool team)
    {
        var items = team ? teamItems : this.items;
        var inventorySlots = team ? teamInventorySlots : this.inventorySlots;
        var oldItem = items[src];
        items[src] = items[dest];
        items[dest] = oldItem;
        if (IsLocalPlayer)
        {
            inventorySlots[src].UpdateBar(0f);
            inventorySlots[src].Icon = items[src] != null ? items[src].icon : emptySlot;
            inventorySlots[dest].Icon = items[dest] != null ? items[dest].icon : emptySlot;
        }
        else
        {
            playerItemShow?.SetItemInSlot(items[src] != null ? items[src].icon : emptySlot, src);
            playerItemShow?.SetItemInSlot(items[dest] != null ? items[dest].icon : emptySlot, dest);
        }
    }

    public void AddItemToTeamFromPlayer(Item item)
    {
        AddItemToTeamFromPlayerServerRPC(item.ID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddItemToTeamFromPlayerServerRPC(string itemID)
    {
        var item = ItemRegistry.Instance.GetByID(itemID);
        GameManager.instance.AddItemToTeamFromPlayer(NetworkObjectId, item);
    }

    public void RemoveItemFromTeamFromPlayer(int slot)
    {
        RemoveItemFromTeamPlayerServerRPC(slot);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveItemFromTeamPlayerServerRPC(int slot)
    {
        GameManager.instance.RemoveItemToTeamFromPlayer(NetworkObjectId, slot);
    }
}
