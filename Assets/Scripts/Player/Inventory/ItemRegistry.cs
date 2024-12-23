using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;
using static Item;

public class ItemRegistry : Registry<Item>
{
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
        var boot = AddItem("Knights Sandles", "boots_01", CharacterType.Damage, "", new StatBlock(20, 0, 10, 0, 0), 500);
        boots.Add(boot);
        boot = AddItem("Magical Sandles", "boots_02", CharacterType.Damage, "", new StatBlock(0, 20, 10, 0, 0), 500);
        boots.Add(boot);
        boot = AddItem("Giant Boots", "leather_boots_01", CharacterType.Tank, "", new StatBlock(0, 0, 10, 50, 0), 500);
        boots.Add(boot);
        boot = AddItem("Plated Boots", "leather_boots_02", CharacterType.Tank, "", new StatBlock(0, 0, 10, 0, 5), 500);
        boots.Add(boot);

        foreach (var item in boots)
        {
            item.sameItems.AddRange(boots.FindAll(x => x.ID != item.ID).Select(x => x.ID));
        }

        AddItemWithVariables("Knights Chestplate", "iron_armor", CharacterType.Tank, "On beeing hit the damager takes {BaseDamage} + {MaxHealthPerc}% Max HP damage. Can only accure once every {Cooldown} seconds", new StatBlock(0, 0, 0, 150, 10), 1500, 1,
            new Dictionary<string, object>() { { "BaseDamage", 10 }, { "MaxHealthPerc", 2 } }, (Item item, CharacterStats stats, int slot) =>
        {
            AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (ulong damager, int damage) =>
            {
                if (item.CanUse)
                {
                    NetworkManager.Singleton.SpawnManager.SpawnedObjects[damager].GetComponent<CharacterStats>().TakeDamage((int)item.variables["BaseDamage"] + (int)(stats.stats.health.Value * ((int)item.variables["BaseDamage"] / 100f)), Vector2.zero, stats);
                    item.StartCooldown();
                }
            });
        });

        AddItemWithVariables("Lifeline", "leather_armor", CharacterType.Tank, "After not beeing in combat for 10 seconds, start regenerating 5% Max HP every {Time} seconds.", new StatBlock(0, 0, 0, 200, 0), 1500, GameManager.instance.OUT_OF_COMBAT_TIME,
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

        var blood_blade = AddItemWithVariables("Bloodlords Blade", "sword_02", CharacterType.Damage, "Gain {LifeSteal}% Lifesteal", new StatBlock(40, 0, 10, 50, 0), 1700, 0,
            new Dictionary<string, object>() { { "LifeSteal", 15f } }, (item, stats, _) =>
        {
            var controller = stats.GetComponent<PlayerAttack>();
            AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong _, ulong _, ref int damage) =>
            {
                stats.Heal((int)(damage * ((float)item.variables["LifeSteal"] / 100f)));
            });
        });

        var blood_spear = AddItemWithVariables("Bloodlords Spear", "spear_02", CharacterType.Damage, "Attacks deal an additional {CurrentHealthPerc}% current health damage. Heal for that amount. Only works on players", new StatBlock(35, 0, 10, 50, 0), 1700, 0,
            new Dictionary<string, object>() { { "CurrentHealthPerc", 5f } }, (item, stats, _) =>
            {
                var controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
                {
                    var targetStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<PlayerStats>();
                    if (targetStats == null) return;
                    var additionalDamage = (int)(targetStats.Health * ((float)item.variables["CurrentHealthPerc"] / 100f));
                    damage += additionalDamage;
                    stats.Heal(additionalDamage);
                });
            });

        blood_blade.sameItems.Add(blood_spear.ID);
        blood_spear.sameItems.Add(blood_blade.ID);

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

        AddItem("Wind Sigil", "circlet", CharacterType.Damage, "On use dash towards the targeted position", new StatBlock(10, 10, 5, 50, 0), 1200, 20,
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
            new StatBlock(50, 0, 5, 50, 0), 2000, 10,
            new Dictionary<string, object>() { { "ExecutePerc", 5f } }, (item, stats, _) =>
            {
                var controller = stats.GetComponent<PlayerAttack>();
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
            new StatBlock(40, 0, 5, 0, 0), 1600, 20,
            new Dictionary<string, object>() { { "DamageReductionPerc", 30f } }, (item, stats, _) =>
            {
                AddToAction(item, () => stats.stats.damageReduction.ChangeValueAdd, (value) => stats.stats.damageReduction.ChangeValueAdd = value, (ref float value, float _) =>
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
               var controller = stats.GetComponent<PlayerController>();
               var targetStats = controller.HoveredStats;
               if (targetStats == null) return;
               int amount = stats.Health / 2;
               if (stats.stats.health.Value - amount <= 0) return;
               stats.TakeDamage(amount, Vector2.zero, stats);
               controller.Heal(targetStats, amount);
               item.StartCooldown();
           });

        AddItemWithVariables("Magic Blade", "sword_01", CharacterType.Damage, "The next attack after a special deals extra {Damage} damage. Can only accure once every {Cooldown} seconds",
           new StatBlock(30, 20, 0, 0, 0), 1200, 2,
           new Dictionary<string, object>() { { "Damage", 50 }, { "Triggered", false } }, (item, stats, _) =>
           {
               AbstractSpecial special = stats.GetComponent<AbstractSpecial>();
               PlayerAttack controller = stats.GetComponent<PlayerAttack>();
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
        AddItemWithVariables("Undying Helmet", "headgear_01", CharacterType.Tank, "When falling below {Threshold}% HP regenerate {HP}% HP every {Time} seconds until at full health",
           new StatBlock(0, 0, 0, 150, 10), 2000, 90,
           new Dictionary<string, object>() { { "Timer", 0f }, { "Time", 0.2f }, { "Triggered", false }, { "Threshold", 10f }, { "HP", 10f } }, null, null, (item, stats, _) =>
           {
               if ((bool)item.variables["Triggered"])
               {
                   if ((float)item.variables["Timer"] > 0)
                   {
                       item.variables["Timer"] = (float)item.variables["Timer"] - Time.deltaTime;
                       if ((float)item.variables["Timer"] <= 0)
                       {
                           stats.Heal((int)(stats.stats.health.Value * (float)item.variables["HP"] / 100f));
                           if (stats.Health < stats.stats.health.Value)
                               item.variables["Timer"] = (float)item.variables["Time"];
                           else
                               item.variables["Triggered"] = false;
                       }
                   }
               }
               if (!item.CanUse) return;
               if (stats.Health < stats.stats.health.Value * ((float)item.variables["Threshold"] / 100f))
               {
                   item.variables["Timer"] = (float)item.variables["Time"];
                   item.variables["Triggered"] = true;
                   item.StartCooldown();
               }
           });
        AddItemWithVariables("First Strike Dagger", "stone_sword", CharacterType.Damage, "When not taking damage for {Cooldown} seconds, the next attack deals an additional {MaxHP}% Max HP damage",
           new StatBlock(30, 0, 0, 0, 0), 1600, 10,
           new Dictionary<string, object>() { { "MaxHP", 5f } }, (item, stats, _) =>
           {
               PlayerAttack controller = stats.GetComponent<PlayerAttack>();
               AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (_, _) =>
               {
                   item.StartCooldown();
               });
               AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
               {
                   if (item.CanUse)
                   {
                       var targetStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<CharacterStats>();
                       if (targetStats != null && !targetStats.IsDead)
                       {
                           targetStats.TakeDamage((int)(targetStats.stats.health.Value * ((float)item.variables["MaxHP"] / 100f)), Vector2.zero, stats);
                           item.StartCooldown();
                       }
                   }
               });
           });

        AddItemWithVariables("Magic-Infused Glove", "glove_02", CharacterType.Damage, "Converts {Ratio}% of damage to special damage.",
           new StatBlock(0, 30, 0, 0, 0), 1600, 0,
           new Dictionary<string, object>() { { "Ratio", 50f } }, (item, stats, _) =>
           {
               PlayerController controller = stats.GetComponent<PlayerController>();
               AddToAction(item, () => stats.stats.specialDamage.ChangeValueAdd, (value) => stats.stats.specialDamage.ChangeValueAdd = value, (ref int damage, int old) =>
               {
                   damage += (int)(stats.stats.damage.Value * ((float)item.variables["Ratio"] / 100f));
               });
           });

        var box = AddItem("Magical Box", "wooden_box", CharacterType.Support, "On use place a torch",
           new StatBlock(0, 15, 5, 0, 0), 1400, 10, null, (item, stats, _) =>
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
               item.StartCooldown();
           });

        var ring = AddItemWithVariables("Miners Ring", "ring_02", CharacterType.Support, "Increase own light range by {LightMult}x", new StatBlock(0, 15, 5, 50, 0), 1200, 0,
            new Dictionary<string, object>() { { "LightMult", 2f } }, (item, stats, _) =>
            {
                var light = stats.GetComponentInChildren<Light2D>();
                light.pointLightOuterRadius *= (float)item.variables["LightMult"];
                item.onUnequip += (_, _, _) =>
                {
                    light.pointLightOuterRadius /= (float)item.variables["LightMult"];
                };
            });
        ring.sameItems.Add(box.ID);
        box.sameItems.Add(ring.ID);

        AddItemWithVariables("Bleeding Scythe", "hi_quality_scethe", CharacterType.Damage, "Applies Bleeding for {BleedTime} seconds on attack. The Bleed deals {Damage} every 1 second. Can only happen every {Cooldown} seconds.", new StatBlock(30, 0, 5, 25, 0), 1400, 2,
            new Dictionary<string, object>() { { "BleedTime", 5 }, { "Damage", 5 } }, (item, stats, _) =>
            {
                PlayerAttack controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
                {
                    if (item.CanUse)
                    {
                        var targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target];
                        targetObject.GetComponent<EffectManager>().AddEffect("bleed", (int)item.variables["BleedTime"], (int)item.variables["Damage"], stats);
                        item.StartCooldown();
                    }
                });
            });

        AddItemWithVariables("Cursed Shield", "shield_02", CharacterType.Tank, "On taking damage curse the target for {Duration} seconds, reducing healing by {Amount}%", new StatBlock(0, 0, 0, 50, 5), 1600, 0,
            new Dictionary<string, object>() { { "Duration", 3 }, { "Amount", 30 } }, (item, stats, _) =>
            {
                AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (ulong damagerId, int damage) =>
                {
                    var targetStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[damagerId].GetComponent<PlayerStats>();
                    if (targetStats != null)
                    {
                        var effects = targetStats.GetComponent<EffectManager>();
                        effects.AddEffect("curse", (int)item.variables["Duration"], (int)item.variables["Amount"], stats);
                    }
                });
            });
        AddItemWithVariables("Cursed Cloth", "fablic_clothe", CharacterType.Damage, "On dealing damage curse the target for {Duration} seconds, reducing healing by {Amount}%", new StatBlock(25, 0, 0, 50, 0), 1600, 0,
            new Dictionary<string, object>() { { "Duration", 3 }, { "Amount", 30 } }, (item, stats, _) =>
            {
                PlayerAttack controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
                {
                    var targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target];
                    targetObject.GetComponent<EffectManager>().AddEffect("curse", (int)item.variables["Duration"], (int)item.variables["Amount"], stats);
                });
            });
        AddItemWithVariables("Cursed Book", "book", CharacterType.Support, "On use curse the target for {Duration} seconds, reducing healing by {Amount}%", new StatBlock(0, 15, 5, 50, 0), 1600, 30,
            new Dictionary<string, object>() { { "Duration", 3 }, { "Amount", 100 } }, null, (item, stats, _) =>
            {
                var controller = stats.GetComponent<PlayerController>();
                if (controller == null) return;
                if (controller.HoveredStats == null) return;
                if (item.CanUse)
                {
                    controller.HoveredStats.GetComponent<EffectManager>().AddEffect("curse", (int)item.variables["Duration"], (int)item.variables["Amount"], stats);
                    item.StartCooldown();
                }
            });
    }

    public override Item GetByID(string id)
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

    public override Item[] GetAll()
    {
        var items = this.items.Values.ToList();
        items.Sort((x, y) => x.cost.CompareTo(y.cost));
        return items.Select(x => new Item(x)).ToArray();
    }
}
