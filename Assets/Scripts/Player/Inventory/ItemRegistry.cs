using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Item;

public class ItemRegistry : MonoBehaviour
{

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

        var potion = AddItemWithVariables("HP Potion", "hp_potion", CharacterType.None, "On use gain {HP} Health", null, 100, 0, new Dictionary<string, object>() { { "HP", 100 } }, null, (Item item, CharacterStats stats, int slot) =>
        {
            stats.Heal((int)item.variables["HP"]);
            stats.GetComponent<Inventory>().RemoveItem(slot);
        });
        potion.multiple = true;

        var torch = AddItem("Torch", "torch_1", CharacterType.None, "On use place a torch", null, 50, 0, null, (Item item, CharacterStats stats, int slot) =>
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
        torch.multiple = true;

        List<Item> boots = new List<Item>();
        var boot = AddItem("Knights Sandles", "boots_01", CharacterType.Damage, "", new StatBlock(10, 0, 10, 0, 0), 500);
        boots.Add(boot);
        boot = AddItem("Magical Sandles", "boots_02", CharacterType.Support, "", new StatBlock(0, 10, 10, 0, 0), 500);
        boots.Add(boot);
        boot = AddItem("Giant Boots", "leather_boots_01", CharacterType.Tank, "", new StatBlock(0, 0, 10, 50, 0), 500);
        boots.Add(boot);
        boot = AddItem("Plated Boots", "leather_boots_02", CharacterType.Tank, "", new StatBlock(0, 0, 10, 0, 5), 500);
        boots.Add(boot);

        foreach (var item in boots)
        {
            item.sameItems.AddRange(boots.FindAll(x => x.ID != item.ID).Select(x => x.ID));
        }

        AddItemWithVariables("Knights Chestplate", "iron_armor", CharacterType.Tank, "On beeing hit the damager takes {BaseDamage} + {MaxHealthPerc}% Max HP damage.", new StatBlock(0, 0, 0, 150, 10), 1500, 0,
            new Dictionary<string, object>() { { "BaseDamage", 10 }, { "MaxHealthPerc", 2 } }, (Item item, CharacterStats stats, int slot) =>
        {
            AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (ulong damager, int damage) =>
            {
                NetworkManager.Singleton.SpawnManager.SpawnedObjects[damager].GetComponent<CharacterStats>().TakeDamage((int)item.variables["BaseDamage"] + (int)(stats.stats.health.Value * ((int)item.variables["BaseDamage"] / 100f)), Vector2.zero, stats);
            });
        });

        AddItemWithVariables("Lifeline", "leather_armor", CharacterType.Tank, "After not beeing in combat for 10 seconds, start regenerating 5% Max HP every {Time} seconds.", new StatBlock(0, 0, 0, 200, 0), 1500, 10f,
            new Dictionary<string, object>() { { "Timer", 1f }, { "Time", 2f } }, (item, stats, _) =>
        {
            AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (_, _) =>
            {
                item.StartCooldown();
            });
        }, null, (item, stats, _) =>
        {
            if (stats.Health < stats.stats.health.Value && item.CanUse)
            {
                item.variables["Timer"] = (float)item.variables["Timer"] - Time.deltaTime;
                if ((float)item.variables["Timer"] <= 0)
                {
                    stats.Heal((int)(stats.stats.health.Value / 20f));
                    item.variables["Timer"] = item.variables["Time"];
                }
            }
        });

        AddItemWithVariables("Miners Ring", "ring_02", CharacterType.Support, "Increase own light range by {LightMult}x", new StatBlock(0, 15, 5, 50, 0), 1200, 0,
            new Dictionary<string, object>() { { "LightMult", 2f } }, (item, stats, _) =>
        {
            var light = stats.GetComponentInChildren<Light2D>();
            light.pointLightOuterRadius *= (float)item.variables["LightMult"];
            item.onUnequip += (_, _, _) =>
            {
                light.pointLightOuterRadius /= (float)item.variables["LightMult"];
            };
        });

        AddItemWithVariables("Bloodlords Blade", "sword_02", CharacterType.Damage, "Gain {LifeSteal}% Lifesteal", new StatBlock(20, 0, 10, 50, 0), 1700, 0,
            new Dictionary<string, object>() { { "LifeSteal", 15f } }, (item, stats, _) =>
        {
            var controller = stats.GetComponent<PlayerController>();
            AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong _, ulong _, ref int damage) =>
            {
                stats.Heal((int)(damage * ((float)item.variables["LifeSteal"] / 100f)));
            });
        });

        AddItemWithVariables("Last Stand", "arm_guard", CharacterType.Tank, "Convert {HealthPerc}% of damage taken into a charge\r\nOn use heal for the amount of charge built up\r\nCharges fall of after {FallOfTimer} seconds",
            new StatBlock(5, 0, 0, 300, 5), 2000, 0,
            new Dictionary<string, object>() { { "HealthPerc", 15f }, { "FallOfTimer", 10f }, { "DamageTaken", 0 }, { "Timer", 0f } }, (item, stats, _) =>
        {
            AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (ulong damager, int damage) =>
            {
                item.variables["DamageTaken"] = (int)((int)item.variables["DamageTaken"] + damage * ((float)item.variables["HealthPerc"] / 100f));
                item.variables["Timer"] = item.variables["FallOfTimer"];
            });
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

        AddItem("Wind Sigil", "circlet", CharacterType.Damage, "On use dash towards the targeted position", new StatBlock(5, 5, 5, 50, 0), 1200, 20,
            null, (item, stats, _) =>
            {
                var mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
                var dir = (mouseWorldPos - stats.transform.position).normalized;
                var rb = stats.GetComponent<Rigidbody2D>();
                rb.velocity = Vector2.zero;
                rb.AddForce(dir * 250, ForceMode2D.Impulse);
                item.StartCooldown();
            });

        AddItemWithVariables("Guillotine", "hoe", CharacterType.Damage, "Executes players below {ExecutePerc}% of their Maximum Health. Can only accure once every {Cooldown} seconds",
            new StatBlock(25, 0, 5, 50, 0), 2000, 10,
            new Dictionary<string, object>() { { "ExecutePerc", 5f } }, (item, stats, _) =>
            {
                var controller = stats.GetComponent<PlayerController>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong damager, ref int damage) =>
                {
                    if (!item.CanUse) return;
                    var targetStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<PlayerStats>();
                    if (targetStats == null) return;
                    if (targetStats.Health - damage < targetStats.stats.health.Value * ((float)item.variables["ExecutePerc"] / 100f))
                    {
                        targetStats.TakeDamage(10000, Vector2.zero, stats);
                        item.StartCooldown();
                    }
                });
            });

        AddItemWithVariables("Bolstering Gloves", "glove_01", CharacterType.Damage, "Gain {DamageReductionPerc}% Damage Reduction on the first hit. Can only accure once every {Cooldown} seconds",
            new StatBlock(25, 0, 5, 0, 0), 1600, 20,
            new Dictionary<string, object>() { { "DamageReductionPerc", 30f } }, (item, stats, _) =>
            {
                AddToAction(item, () => stats.stats.damageReduction.ChangeValue, (value) => stats.stats.damageReduction.ChangeValue = value, (ref float value, float _) =>
                {
                    if (item.CanUse) value += (float)item.variables["DamageReductionPerc"];
                });
                AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (_, _) =>
                {
                    if (item.CanUse)
                        item.StartCooldown();
                });
            });


        var spear = AddItem("Battlemage Spear", "spear_01", CharacterType.Support, "On use reset current special cooldown", new StatBlock(0, 20, 0, 50, 0), 2000, 0,
            null, (item, stats, _) =>
            {
                stats.GetComponent<AbstractSpecial>().SetCooldown(0);
                item.StartCooldown();
            });

        var glove = AddItemWithVariables("Sharing Clover", "clover_leaf", CharacterType.Support, "On heal reduce the cooldown of the special by {CooldownReduce} seconds. Can only accure once every {Cooldown} seconds",
           new StatBlock(0, 25, 0, 50, 0), 2000, 2,
           new Dictionary<string, object>() { { "CooldownReduce", 1 } }, (item, stats, _) =>
           {
               var controller = stats.GetComponent<PlayerController>();
               AddToAction(item, () => controller.OnHeal, (value) => controller.OnHeal = value, (ulong target, ulong user, ref int amount) =>
               {
                   if (item.CanUse)
                   {
                       controller.GetComponent<AbstractSpecial>().ReduceCooldown(2);
                       item.StartCooldown();
                   }
               });
           });
        spear.sameItems.Add(glove.ID);
        glove.sameItems.Add(spear.ID);

        AddItemWithVariables("Druids Wand", "wand_01", CharacterType.Support, "Targets below {Threshold}% HP gain an additional {Addition}% healing",
           new StatBlock(0, 25, 5, 0, 0), 1400, 0,
           new Dictionary<string, object>() { { "Threshold", 50f }, { "Addition", 20f } }, (item, stats, _) =>
           {
               var controller = stats.GetComponent<PlayerController>();
               AddToAction(item, () => controller.OnHeal, (value) => controller.OnHeal = value, (ulong target, ulong user, ref int amount) =>
               {
                   var stats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<CharacterStats>();
                   if (stats == null) return;
                   if (stats.Health < stats.stats.health.Value * ((float)item.variables["Threshold"] / 100f))
                       amount += (int)(amount * ((float)item.variables["Threshold"] / 100f));
               });
           });

        AddItemWithVariables("Scroll of Sacrifice", "scroll_leather", CharacterType.Support, "Target a teammate to sacrifice {Sacrifice}% of own HP and heal the target for the same amount",
           new StatBlock(0, 15, 0, 100, 0), 1700, 60,
           new Dictionary<string, object>() { { "Sacrifice", 50f } }, null, (item, stats, _) =>
           {
               RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition), Vector2.zero);
               var controller = stats.GetComponent<PlayerController>();
               if (hit.transform != null)
               {
                   if (hit.transform.gameObject == stats.gameObject) return;
                   var targetStats = hit.transform.GetComponent<PlayerStats>();
                   if (targetStats == null) return;
                   int amount = stats.Health / 2;
                   if (stats.stats.health.Value - amount <= 0) return;
                   stats.TakeDamage(amount, Vector2.zero, stats);
                   controller.Heal(targetStats, amount);
                   item.StartCooldown();
               }
           });

        AddItemWithVariables("Magic Blade", "sword_01", CharacterType.Damage, "The next attack after a special deals extra {Damage} damage. Can only accure once every {Cooldown} seconds",
           new StatBlock(15, 0, 0, 0, 0), 1200, 2,
           new Dictionary<string, object>() { { "Damage", 50 }, { "Triggered", false } }, (item, stats, _) =>
           {
               AbstractSpecial special = stats.GetComponent<AbstractSpecial>();
               PlayerController controller = stats.GetComponent<PlayerController>();
               AddToAction(item, () => special.onSpecial, (AbstractSpecial.SpecialDelegate value) => special.onSpecial = value, () =>
               {
                   if (item.CanUse)
                   {
                       item.variables["Triggered"] = true;
                       item.StartCooldown();
                   }
               });
               AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong _, ulong _, ref int damage) =>
               {
                   if ((bool)item.variables["Triggered"])
                   {
                       damage += (int)item.variables["Damage"];
                       item.variables["Triggered"] = false;
                   }
               });
           });
    }

    public Item GetItemById(string id)
    {
        return new Item(items[id]);
    }

    public Item AddItem(string name, string spritePath, CharacterType type = CharacterType.None, string description = "", StatBlock stats = null,
        int cost = 0, float cooldown = 0, ItemFunction onEquip = null, ItemFunction onUse = null, ItemFunction onUpdate = null)
    {
        Item item = new Item(name);
        item.type = type;
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

    public Item AddItemWithVariables(string name, string spritePath, CharacterType type = CharacterType.None, string description = "", StatBlock stats = null,
        int cost = 0, float cooldown = 0, Dictionary<string, object> variables = null, ItemFunction onEquip = null, ItemFunction onUse = null, ItemFunction onUpdate = null)
    {
        var item = AddItem(name, spritePath, type, description, stats, cost, cooldown, onEquip, onUse, onUpdate);
        item.variables = variables;
        return item;
    }

    public Item[] GetItems()
    {
        var items = this.items.Values.ToList();
        items.Sort((x, y) => x.cost.CompareTo(y.cost));
        return items.Select(x => new Item(x)).ToArray();
    }

    public void AddToAction(Item item, Func<PlayerController.ActionDelegate> getAction, Action<PlayerController.ActionDelegate> setAction, PlayerController.ActionDelegate a)
    {
        setAction(getAction() + a);
        item.onUnequip += (Item item, CharacterStats stats, int slot) =>
        {
            setAction(getAction() - a);
        };
    }
    public void AddToAction(Item item, Func<AbstractSpecial.SpecialDelegate> getAction, Action<AbstractSpecial.SpecialDelegate> setAction, AbstractSpecial.SpecialDelegate a)
    {
        setAction(getAction() + a);
        item.onUnequip += (Item item, CharacterStats stats, int slot) =>
        {
            setAction(getAction() - a);
        };
    }

    public void AddToAction(Item item, Func<CharacterStats.DamageDelegate> getAction, Action<CharacterStats.DamageDelegate> setAction, CharacterStats.DamageDelegate a)
    {
        setAction(getAction() + a);
        item.onUnequip += (Item item, CharacterStats stats, int slot) =>
        {
            setAction(getAction() - a);
        };
    }

    public void AddToAction(Item item, Func<CharacterStats.DeathDelegate> getAction, Action<CharacterStats.DeathDelegate> setAction, CharacterStats.DeathDelegate a)
    {
        setAction(getAction() + a);
        item.onUnequip += (Item item, CharacterStats stats, int slot) =>
        {
            setAction(getAction() - a);
        };
    }

    public void AddToAction<T>(Item item, Func<StatBlock.Stat<T>.OnStatChange> getAction, Action<StatBlock.Stat<T>.OnStatChange> setAction, StatBlock.Stat<T>.OnStatChange a)
    {
        setAction(getAction() + a);
        item.onUnequip += (Item item, CharacterStats stats, int slot) =>
        {
            setAction(getAction() - a);
        };
    }
}
