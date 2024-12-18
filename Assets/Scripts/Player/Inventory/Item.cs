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
    public delegate void ItemFunction(Item item, CharacterStats stats, int slot);

    public CharacterType type;
    public ItemFunction onUse;
    public ItemFunction onUpdate;
    public ItemFunction onEquip;
    public ItemFunction onUnequip;
    public StatBlock stats;
    public Sprite icon;
    public string description;
    public int cost;
    public float cooldown;
    public bool multiple;
    public List<string> sameItems = new List<string>();
    [HideInInspector] public Dictionary<string, object> variables = new Dictionary<string, object>();

    private float timer;

    public float CurrentCooldownDelta
    {
        get
        {
            if (cooldown == 0)
                return 0;
            return timer / cooldown;
        }
    }

    public bool CanUse { get => timer <= 0; }

    public string Description
    {
        get
        {
            return DescriptionCreator.Generate(description, variables);
        }
    }

    public Item(Item item) : this(item.name)
    {
        this.id = item.id;
        this.type = item.type;
        this.onUse = item.onUse;
        this.onUpdate = item.onUpdate;
        this.onEquip = item.onEquip;
        this.stats = item.stats;
        this.icon = item.icon;
        this.description = item.description;
        this.cost = item.cost;
        this.cooldown = item.cooldown;
        this.variables = item.variables;
        this.timer = item.timer;
        this.multiple = item.multiple;
        this.sameItems = item.sameItems;
        variables["Cooldown"] = cooldown;
    }

    public Item(string name)
    {
        this.name = name;
        id = GetIDFromName(name);
        currentIndex++;
    }

    public void OnEquip(CharacterStats stats, int slot)
    {
        if (stats != null)
        {
            if (this.stats != null)
                stats.stats.Add(this.stats);
        }
        onEquip?.Invoke(this, stats, slot);
    }

    public void OnUnequip(CharacterStats stats, int slot)
    {
        if (stats != null)
        {
            if (this.stats != null)
                stats.stats.Remove(this.stats);
        }
        onUnequip?.Invoke(this, stats, slot);
    }

    public void Use(CharacterStats stats, int slot)
    {
        onUse?.Invoke(this, stats, slot);
    }

    public void StartCooldown()
    {
        if (cooldown > 0)
            timer = cooldown;
    }

    public static string GetIDFromName(string name)
    {
        return name.ToLower().Replace(" ", "_");
    }

    public void Update(CharacterStats stats, int slot)
    {
        onUpdate?.Invoke(this, stats, slot);
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
    }
}
