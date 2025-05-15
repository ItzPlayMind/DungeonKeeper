using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Consumable : Item
{
    public ItemFunction onBuy;


    public Consumable(string name) : base(name) { }
    public Consumable(Consumable item) : base(item)
    {
        this.onBuy = item.onBuy;
    }

    public void OnBuy(CharacterStats stats)
    {
        onBuy?.Invoke(this, stats, -1);
    }
}
