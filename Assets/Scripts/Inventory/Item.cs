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

    public ItemFunction onUse;
    public ItemFunction onUpdate;
    public ItemFunction onEquip;
    public StatBlock stats;
    public Sprite icon;
    public string description;
    public int cost;
    public float cooldown;
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
            var words = description.Split(" ");
            string text = "";
            foreach (var word in words)
            {
                if (word.IndexOf("{") < word.IndexOf("}"))
                {
                    int indexOfBegin = word.IndexOf("{");
                    int indexOfEnd = word.IndexOf("}");
                    var variableName = word.Substring(indexOfBegin + 1, indexOfEnd - indexOfBegin - 1);
                    text += "<color=red>" + variables[variableName] + word.Substring(indexOfEnd + 1) + "</color>";
                }
                else
                    text += word;
                text += " ";
            }
            return text.Trim();
        }
    }

    public Item(Item item) : this(item.name)
    {
        this.id = item.id;
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
            onEquip?.Invoke(this, stats, slot);
        }
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
