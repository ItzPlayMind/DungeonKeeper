using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static DebugConsole;

public class Inventory : NetworkBehaviour
{
    public static int INVENTORY_SIZE = 6;
    [SerializeField] private NetworkVariable<int> cash = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private UITextIconBar[] inventorySlots = new UITextIconBar[INVENTORY_SIZE];
    [SerializeField] private UIIconBar[] teamInventorySlots = new UIIconBar[3];
    [SerializeField] private Sprite emptySlot;
    [SerializeField] private TMPro.TextMeshProUGUI cashText;

    private PlayerItemShow playerItemShow;
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
        OnCashChange((value) =>
        {
            cashText.text = value + "";
        });
        DebugConsole.OnCommand((Command command) =>
        {
            if (command.args.Length != 2) return;
            AddCash(int.Parse(command.args[1]));
            Debug.Log("Cash added");
        }, "cash", "add");
        items = new Item[INVENTORY_SIZE];
        stats = GetComponent<CharacterStats>();
    }

    public void OnTeamAssigned()
    {
        if(!IsLocalPlayer)
            playerItemShow = ShopPanel.Instance.CreatePlayerItemsShow(Lobby.Instance.PlayerStatistic.GetNameByClientID(OwnerClientId));
    }

    public bool CanAddItem()
    {
        var items = this.items;
        for (int i = 0; i < items.Length; i++)
            if (items[i] == null) return true;
        return false;
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
        var slots =  inventorySlots;
        var items = this.items;
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
        item.UpdateText("");
        item.OnUpdateText = null;

        inventorySlots[slot].transform.Find("Active").gameObject.SetActive(false);
        item?.OnUnequip(stats, slot);
    }

    public void AddConsumable(Consumable item)
    {
        AddConsumableServerRPC(item.ID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddConsumableServerRPC(string itemID)
    {
        AddConsumableClientRPC(itemID);
    }

    [ClientRpc]
    private void AddConsumableClientRPC(string itemID)
    {
        var item = (ItemRegistry.Instance as ItemRegistry).GetByID(itemID) as Consumable;
        item?.OnBuy(stats);
    }

    public void AddItem(Item item)
    {
        int index;
        var items = this.items;
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
        var item = (ItemRegistry.Instance as ItemRegistry).GetByID(itemID);
        items[slot] = item;
        if (IsLocalPlayer)
            inventorySlots[slot].Icon = item.icon;
        else
            playerItemShow?.SetItemInSlot(item.icon, slot);
        item?.OnEquip(stats, slot);
        inventorySlots[slot].transform.Find("Active").gameObject.SetActive(item.onUse != null);
        if (inventorySlots[slot] is UITextIconBar)
            item.OnUpdateText = (string text) => { inventorySlots[slot].Text = text; };
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
        var items =  this.items;
        var inventorySlots =  this.inventorySlots;
        var oldItem = items[src];
        items[src] = items[dest];
        items[dest] = oldItem;
        if (IsLocalPlayer)
        {
            inventorySlots[src].UpdateBar(0f);
            inventorySlots[dest].UpdateBar(0f);
            inventorySlots[src].Icon = items[src] != null ? items[src].icon : emptySlot;
            inventorySlots[dest].Icon = items[dest] != null ? items[dest].icon : emptySlot;
            if (inventorySlots[src] is UITextIconBar)
                if (items[src] != null)
                    items[src].OnUpdateText = (string text) => { (inventorySlots[src] as UITextIconBar).Text = text; };
                else
                    (inventorySlots[src] as UITextIconBar).Text = "";
            if (inventorySlots[dest] is UITextIconBar && items[dest] != null)
                if (items[dest] != null)
                    items[dest].OnUpdateText = (string text) => { (inventorySlots[dest] as UITextIconBar).Text = text; };
                else
                    (inventorySlots[dest] as UITextIconBar).Text = "";
            inventorySlots[src].transform.Find("Active").gameObject.SetActive(items[src] != null && items[src].onUse != null);
            inventorySlots[dest].transform.Find("Active").gameObject.SetActive(items[dest] != null && items[dest].onUse != null);
        }
        else
        {
            playerItemShow?.SetItemInSlot(items[src] != null ? items[src].icon : emptySlot, src);
            playerItemShow?.SetItemInSlot(items[dest] != null ? items[dest].icon : emptySlot, dest);
        }
    }
}
