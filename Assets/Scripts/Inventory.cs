using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Inventory : NetworkBehaviour
{
    [SerializeField] private NetworkVariable<int> cash = new NetworkVariable<int>(0);
    [SerializeField] private List<Item> items = new List<Item>();
    public int Cash { get => cash.Value; }

    public void AddCash(int cash) => this.cash.Value += cash;
    public void RemoveCash(int cash) => this.cash.Value -= cash;

    private CharacterStats stats;
    private void Start()
    {
        stats = GetComponent<CharacterStats>();
    }

    public void AddItem(Item item)
    {
        items.Add(item);
        item.OnEquip(stats);
    }
    public void RemoveItem(Item item) { items.Remove(item); }

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
}
