using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

[System.Serializable]
public class Item
{
    public static int currentIndex = 0;
    [SerializeField] private string name;
    [SerializeField] private string id;
    public string ID { get => id; }
    public string Name { get => name; }
    public System.Action<PlayerController> OnUse;
    public System.Action<PlayerController> OnUpdate;
    public StatBlock stats;
    public Sprite icon;
    public int cost;

    public Item(string name)
    {
        this.name = name;
        id = GetIDFromName(name);
        currentIndex++;
    }

    public void OnEquip(CharacterStats stats)
    {
        if(stats != null)
            stats.stats.Add(this.stats);
    }

    public static string GetIDFromName(string name)
    {
        return name.ToLower().Replace(" ", "_");
    }
}
