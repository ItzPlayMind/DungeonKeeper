using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Item;

public class ItemRegistry : MonoBehaviour
{
    [System.Serializable]
    private class ItemSetting
    {
        public string name;
        [Multiline] public string description;
        public Sprite icon;
        public StatBlock statBlock;
        public int cost;
        public float cooldown;
    }

    public static ItemRegistry Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    //[SerializeField] private List<ItemSetting> itemSettings = new List<ItemSetting>();

    private Dictionary<string, Item> items = new Dictionary<string, Item>();

    private void Start()
    {

        AddItemWithVariables("HP Potion", "hp_potion", "On use gain {HP} Health", null, 200, 0, new Dictionary<string, object>() { { "HP", 100 } }, null, (Item item, CharacterStats stats, int slot) =>
        {
            stats.Heal((int)item.variables["HP"]);
            stats.GetComponent<Inventory>().RemoveItem(slot);
        });

        AddItem("Torch", "torch_1", "On use place a torch", null, 200, 0, null, (Item item, CharacterStats stats, int slot) =>
        {
            var mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
            var dir = (mouseWorldPos - stats.transform.position);
            dir.z = 0;
            Vector2 pos = Vector2.zero;
            if (dir.magnitude >= 1.5f)
                pos = stats.transform.position + dir.normalized * 1.5f;
            else
                pos = mouseWorldPos;
            GameManager.instance.SetTorch(pos);
            stats.GetComponent<Inventory>().RemoveItem(slot);
        });

        AddItem("Knights Sandles", "boots_01", "", new StatBlock(10,0,10,0,0), 500);
        AddItem("Magical Sandles", "boots_02", "", new StatBlock(0, 10, 10, 0, 0), 500);
        AddItem("Giant Boots", "leather_boots_01", "", new StatBlock(0, 0, 10, 50, 0), 500);
        AddItem("Plated Boots", "leather_boots_02", "", new StatBlock(0, 0, 10, 0, 5), 500);

        AddItemWithVariables("Knights Chestplate", "iron_armor", "On beeing hit the damager takes {BaseDamage} + {MaxHealthPerc}% Max HP damage.", new StatBlock(0, 0, 0, 150, 10), 1500, 0,
            new Dictionary<string, object>() { { "BaseDamage", 10 }, { "MaxHealthPerc", 10 } },(Item item, CharacterStats stats, int slot) =>
        {
            stats.OnTakeDamage += (ulong damager, int damage) =>
            {
                NetworkManager.Singleton.SpawnManager.SpawnedObjects[damager].GetComponent<CharacterStats>().TakeDamage((int)item.variables["BaseDamage"] +(int)(stats.stats.health.Value* ((float)item.variables["BaseDamage"]/100)),Vector2.zero,stats);
            };
        });

        AddItemWithVariables("Lifeline", "leather_armor", "After not beeing in combat for {HitTime} seconds, start regenerating 5% Max HP every {Time} seconds.", new StatBlock(0, 0, 0, 200, 0), 1500, 0,
            new Dictionary<string, object>() { { "Timer", 1f}, { "HitTimer", 0f }, { "Time", 2f }, { "HitTime", 10f } },  (item, stats, _) =>
        {
            stats.OnTakeDamage += (_, _) =>
            {
                item.variables["HitTimer"] = item.variables["HitTime"];
                item.variables["Timer"] = item.variables["Time"];
            };
        }, null, (item, stats, _) =>
        {
            if ((float)item.variables["HitTimer"] > 0f)
            {
                item.variables["HitTimer"] = (float)item.variables["HitTimer"] - Time.deltaTime;
            }
            else
            {
                if (stats.Health < stats.stats.health.Value)
                {
                    item.variables["Timer"] = (float)item.variables["Timer"] - Time.deltaTime;
                    if((float)item.variables["Timer"] <= 0)
                    {
                        stats.Heal((int)(stats.stats.health.Value / 20f));
                        item.variables["Timer"] = item.variables["Time"];
                    }
                }
            }
        });

        AddItemWithVariables("Miners Ring", "ring_02", "Increase own light range by {LightMult}x", new StatBlock(0, 15, 5, 50, 0), 1200, 0,
            new Dictionary<string, object>() { { "LightMult", 2f } }, (item,stats,_) =>
        {
            stats.GetComponentInChildren<Light2D>().pointLightOuterRadius *= (float)item.variables["LightMult"];
        });

        AddItemWithVariables("Bloodlords Blade", "sword_02", "Gain {LifeSteal}% Lifesteal", new StatBlock(20, 0, 10, 50, 0), 1700, 0,
            new Dictionary<string, object>() { { "LifeSteal", 15f } }, (item, stats, _) =>
        {
            stats.GetComponent<PlayerController>().OnAttack += (_,_,damage) =>
            {
                stats.Heal((int)(damage * ((float)item.variables["LifeSteal"]/100f)));
            };
        });

        AddItemWithVariables("Last Stand", "arm_guard", "Convert {HealthPerc}% of damage taken into a charge\r\nOn use heal for the amount of charge built up\r\nCharges fall of after {FallOfTimer} seconds",
            new StatBlock(5, 0, 0, 300, 5), 2000, 0,
            new Dictionary<string, object>() { { "HealthPerc", 15f }, { "FallOfTimer", 10f }, { "DamageTaken", 0}, { "Timer", 0f } }, (item, stats, _) =>
        {
            stats.OnTakeDamage += (ulong damager, int damage) =>
            {
                item.variables["DamageTaken"] = (int)((int)item.variables["DamageTaken"] + damage * ((float)item.variables["HealthPerc"] / 100f));
                item.variables["Timer"] = item.variables["FallOfTimer"];
            };
        }, (item, stats, _) =>
        {
            stats.Heal((int)item.variables["DamageTaken"]);
            item.variables["DamageTaken"] = 0;
            item.variables["Timer"] = 0f;
            item.StartCooldown();
        }, (item, stats, _) =>
        {
            if ((float)item.variables["Timer"] > 0)
            {
                item.variables["Timer"] = (float)item.variables["Timer"] - Time.deltaTime;
                if ((float)item.variables["Timer"] <= 0)
                {
                    item.variables["DamageTaken"] = 0;
                }
            }
        });

        AddItem("Battlemage Spear", "spear_01", "On use reset current special cooldown", new StatBlock(0, 20, 0, 50, 0), 2000, 0,
            null, (item, stats, _) =>
        {
            stats.GetComponent<AbstractSpecial>().SetCooldown(0);
            item.StartCooldown();
        });

        AddItem("Wind Sigil", "circlet", "On use dash towards the targeted position", new StatBlock(5, 5, 5, 50, 0), 1200, 20,
            null, (item, stats, _) =>
            {
                var mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
                var dir = (mouseWorldPos - stats.transform.position).normalized;
                var rb = stats.GetComponent<Rigidbody2D>();
                rb.velocity = Vector2.zero;
                rb.AddForce(dir * 250, ForceMode2D.Impulse);
                item.StartCooldown();
            });
    }

    public Item GetItemById(string id)
    {
        return new Item(items[id]);
    }

    public Item AddItem(string name, string spritePath, string description = "", StatBlock stats = null,
        int cost = 0, float cooldown = 0, ItemFunction onEquip = null, ItemFunction onUse = null, ItemFunction onUpdate = null)
    {
        Item item = new Item(name);
        item.onUse = onUse;
        item.onUpdate = onUpdate;
        item.onEquip = onEquip;
        item.description = description;
        item.cooldown = cooldown;
        item.stats = stats;
        item.icon = Resources.Load<Sprite>("Equipment/" + spritePath);
        item.cost = cost;
        items.Add(item.ID, item);
        return item;
    }

    public Item AddItemWithVariables(string name, string spritePath, string description = "", StatBlock stats = null,
        int cost = 0, float cooldown = 0, Dictionary<string,object> variables = null, ItemFunction onEquip = null, ItemFunction onUse = null, ItemFunction onUpdate = null)
    {
        var item = AddItem(name, spritePath, description, stats, cost, cooldown, onEquip, onUse, onUpdate);
        item.variables = variables;
        return item;
    }

    public Item[] GetItems()
    {
        var items = this.items.Values.ToList();
        items.Sort((x,y) => x.cost.CompareTo(y.cost));
        return items.Select(x=>new Item(x)).ToArray();
    }
}
