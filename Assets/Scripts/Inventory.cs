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
    [SerializeField] private NetworkVariable<int> cash = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private UIIconBar[] inventorySlots = new UIIconBar[INVENTORY_SIZE];
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Sprite emptySlot;
    private Item[] items;
    public int Cash { get => cash.Value; }


    public void OnCashChange(System.Action<int> callback)
    {
        cash.OnValueChanged += (_,value)=>callback?.Invoke(value);
    }

    public void AddCash(int cash) {
        if (IsOwner)
        {
            this.cash.Value += cash;
        }
    }
    public void RemoveCash(int cash) { if (IsOwner) this.cash.Value -= cash; }

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
        if (!IsLocalPlayer)
            return;
        stats = GetComponent<CharacterStats>();
        items = new Item[INVENTORY_SIZE];
    }

    public bool CanAddItem { 
        get
        {
            for (int i = 0; i < items.Length; i++)
                if (items[i] == null) return true;
            return false;
        }
    }

    public void AddItem(Item item)
    {
        int index;
        for (index = 0; index < INVENTORY_SIZE; index++)
        {
            if (items[index] == null)
                break;
        };
        inventorySlots[index].Icon = item.icon;
        items[index] = item;
        item?.OnEquip(stats,index);
    }

    public void RemoveItem(int slot)
    {
        inventorySlots[slot].Icon = emptySlot;
        items[slot] = null;
    }

    private float goldTimer = 1f;
    private void Update()
    {
        if (!IsLocalPlayer) return;
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
}
