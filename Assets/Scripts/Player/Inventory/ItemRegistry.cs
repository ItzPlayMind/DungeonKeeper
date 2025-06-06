using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;
using static DescriptionCreator;
using static Item;

public class ItemRegistry : Registry<Item>
{
    private Dictionary<string, Item> items = new Dictionary<string, Item>();

    private void Start()
    {
        var potion = AddItemWithVariables("HP Potion", "hp_potion", CharacterType.None, "On use heal {HP} Health every second for {Duration} seconds.", null, 100, 0, new Dictionary<string, Variable>() { { "HP", new Variable() { value = 20 } }, { "Duration", new Variable() { value = 10 } } }, null, (Item item, CharacterStats stats, int slot) =>
        {
            stats.GetComponent<EffectManager>()?.AddEffect("potion", (int)item.variables["Duration"].value, (int)item.variables["HP"].value, stats);
            stats.GetComponent<Inventory>().RemoveItem(slot);
        });
        potion.multiple = true;

        /*var torch = AddItem("Torch", "torch", CharacterType.None, "On use place a torch", null, 50, 0, null, (Item item, CharacterStats stats, int slot) =>
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
        torch.multiple = true;*/

        AddConsumable("Runestone I", "runestone_1", CharacterType.None, "On buy unlocks the first upgrade of the special", null, 3000, (Item item, CharacterStats stats, int slot) =>
        {
            if (!stats.IsLocalPlayer) return;
            stats.GetComponent<AbstractSpecial>().UnlockUpgrade(0);
        });
        AddConsumable("Runestone II", "runestone_2", CharacterType.None, "On buy unlocks the second upgrade of the special", null, 4000, (Item item, CharacterStats stats, int slot) =>
        {
            if (!stats.IsLocalPlayer) return;
            stats.GetComponent<AbstractSpecial>().UnlockUpgrade(1);
        });
        AddConsumable("Runestone III", "runestone_3", CharacterType.None, "On buy unlocks the third upgrade of the special", null, 5000, (Item item, CharacterStats stats, int slot) =>
        {
            if (!stats.IsLocalPlayer) return;
            stats.GetComponent<AbstractSpecial>().UnlockUpgrade(2);
        });
        var fateScroll = AddConsumable("Scroll of Fate", "scroll_of_fate", CharacterType.None, "On buy gain 3 random Fates, 1 can be chosen", null, 2000, (Item item, CharacterStats stats, int slot) =>
        {
            if (!stats.IsLocalPlayer) return;
            ShopPanel.Instance.Toggle();
            (GameManager.instance as ArenaGameManager).CardSelection.gameObject.SetActive(true);
        });
        fateScroll.multiple = true;

        List<Item> boots = new List<Item>();
        var boot = AddItem("Knights Sandles", "knights_sandles", CharacterType.Damage, "", new StatBlock(15, 0, 0, 10, 0, 0), 500);
        boots.Add(boot);
        boot = AddItem("Magical Sandles", "magical_sandles", CharacterType.Damage, "", new StatBlock(0, 15, 0, 10, 0, 0), 500);
        boots.Add(boot);
        boot = AddItem("Giant Boots", "giant_boots", CharacterType.Tank, "", new StatBlock(0, 0, 0, 10, 50, 0), 500);
        boots.Add(boot);
        boot = AddItem("Plated Boots", "plated_boots", CharacterType.Tank, "", new StatBlock(0, 0, 0, 10, 0, 5), 500);
        boots.Add(boot);

        foreach (var item in boots)
        {
            item.sameItems.AddRange(boots.FindAll(x => x.ID != item.ID).Select(x => x.ID));
        }

        AddItemWithVariables("Knights Chestplate", "iron_armor", CharacterType.Tank, "On beeing hit the damager takes {Damage} damage. Can only accure once every {Cooldown} seconds", new StatBlock(0, 0, 0, 0, 250, 10), 1500, 1,
            new Dictionary<string, Variable>() { { "BaseDamage", new Variable() { value = 10 } }, { "MaxHealthPerc", new Variable() { value = 2 } }, { "Damage", new Variable() { value = 10, color = "green" } } }, (Item item, CharacterStats stats, int slot) =>
        {
            AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (ulong damager, int damage) =>
            {
                if (item.CanUse)
                {
                    NetworkManager.Singleton.SpawnManager.SpawnedObjects[damager].GetComponent<CharacterStats>().TakeDamage((int)item.variables["Damage"].value, Vector2.zero, stats);
                    item.StartCooldown();
                }
            });
        }, null, (Item item, CharacterStats stats, int slot) =>
        {
            item.variables["Damage"].value = (int)item.variables["BaseDamage"].value + (int)(stats.stats.health.Value * ((int)item.variables["MaxHealthPerc"].value / 100f));
        });

        AddItemWithVariables("Lifeline", "leather_armor", CharacterType.Tank, "After not beeing in combat for 10 seconds, start regenerating {HealAmount}% Max HP every {Time} seconds.", new StatBlock(0, 0, 0, 0, 300, 0), 1500, GameManager.OUT_OF_COMBAT_TIME,
            new Dictionary<string, Variable>() { { "Timer", new Variable() { value = 1f } }, { "HealAmount", new Variable() { value = 2.5f, color = "green" } }, { "Time", new Variable() { value = 2f } } }, (item, stats, _) =>
        {
            AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (_, _) =>
            {
                item.StartCooldown();
            });
        }, null, (item, stats, _) =>
        {
            if (stats.Health < stats.stats.health.Value && item.CanUse)
            {
                item.variables["Timer"].value = (float)item.variables["Timer"].value - Time.deltaTime;
                if ((float)item.variables["Timer"].value <= 0)
                {
                    stats.Heal((int)(stats.stats.health.Value * ((float)item.variables["HealAmount"].value / 100f)));
                    item.variables["Timer"].value = item.variables["Time"].value;
                }
            }
        });

        List<Item> healItems = new List<Item>();

        var blood_blade = AddItemWithVariables("Bloodlords Blade", "sword_02", CharacterType.Damage, "Heal for {LifeSteal}% of normal damage dealt", new StatBlock(20, 0, 0, 0, 150, 0), 2200, 0,
            new Dictionary<string, Variable>() { { "LifeSteal", new Variable() { value = 5f, color = "red" } } }, (item, stats, _) =>
        {
            var controller = stats.GetComponent<PlayerAttack>();
            AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong _, ulong _, ref int damage) =>
            {
                stats.Heal((int)(damage * ((float)item.variables["LifeSteal"].value / 100f)));
            });
        });

        var blood_spear = AddItemWithVariables("Bloodlords Spear", "spear_02", CharacterType.Damage, "Attacks deal an additional {CurrentHealthPerc}% current health damage. Heal for that amount", new StatBlock(30, 0, 10, 0, 150, 0), 2200, 0,
            new Dictionary<string, Variable>() { { "CurrentHealthPerc", new Variable() { value = 5f, color = "green" } } }, (item, stats, _) =>
            {
                var controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
                {
                    var targetStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<PlayerStats>();
                    if (targetStats == null) return;
                    var additionalDamage = (int)(targetStats.Health * ((float)item.variables["CurrentHealthPerc"].value / 100f));
                    damage += additionalDamage;
                    stats.Heal(additionalDamage);
                });
            });


        var necromancyBook = AddItemWithVariables("Book of Necromancy", "necromancy_book", CharacterType.Damage, "Heal for {HealAmount}% of the damage dealt by the special", new StatBlock(0, 20, 0, 0, 150, 0), 2200, 0,
           new Dictionary<string, Variable>() { { "HealAmount", new Variable() { value = 10f, color = "blue" } } },
           (item, stats, _) =>
           {
               var controller = stats.GetComponent<AbstractSpecial>();
               AddToAction(item, () => controller.OnTargetHit, (value) => controller.OnTargetHit = value, (ulong target, ulong user, ref int amount) =>
               {
                   stats.Heal((int)(amount * ((float)item.variables["HealAmount"].value / 100f)));
               });
           });

        healItems.Add(necromancyBook);
        healItems.Add(blood_spear);
        healItems.Add(blood_blade);
        foreach (var item in healItems)
        {
            foreach (var item2 in healItems)
            {
                item.sameItems.Add(item2.ID);
            }
        }

        AddItemWithVariables("Last Stand", "arm_guard", CharacterType.Tank, "Convert {HealthPerc}% of damage taken into a charge\r\nOn use heal for the amount of charge built up\r\nCharges fall of after {FallOfTimer} seconds",
            new StatBlock(5, 0, 0, 0, 300, 10), 2000, 30,
            new Dictionary<string, Variable>() { { "HealthPerc", new Variable() { value = 15f, color = "green" } }, { "FallOfTimer", new Variable() { value = 10f } }, { "DamageTaken", new Variable() { value = 0 } }, { "Timer", new Variable() { value = 0f } } }, (item, stats, _) =>
        {
            AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (ulong damager, int damage) =>
            {
                item.variables["DamageTaken"].value = (int)((int)item.variables["DamageTaken"].value + damage * ((float)item.variables["HealthPerc"].value / 100f));
                item.UpdateText(item.variables["DamageTaken"].value.ToString());
                item.variables["Timer"].value = item.variables["FallOfTimer"].value;
            });
        }, (item, stats, _) =>
        {
            if ((int)item.variables["DamageTaken"].value <= 0) return;
            stats.Heal((int)item.variables["DamageTaken"].value);
            item.variables["DamageTaken"].value = 0;
            item.UpdateText("");
            item.variables["Timer"].value = 0f;
            item.StartCooldown();
        }, (item, stats, _) =>
        {
            if ((float)item.variables["Timer"].value > 0)
            {
                item.variables["Timer"].value = (float)item.variables["Timer"].value - Time.deltaTime;
                if ((float)item.variables["Timer"].value <= 0)
                {
                    item.variables["DamageTaken"].value = 0;
                    item.UpdateText("");
                }
            }
        });

        AddItemWithVariables("Wind Sigil", "circlet", CharacterType.Damage, "On use dash towards the targeted position. Moving for {MoveDuration} seconds adds {SpeedMult}% speed. The next attack on a target will slow it by {SpeedMult}% for {SlowDuration} seconds.", new StatBlock(10, 10, 10, 5, 50, 0), 2200, 20,
            new Dictionary<string, Variable>() { { "SpeedMult", new Variable() { value = 15f, color = "yellow" } }, { "MoveDuration", new Variable() { value = 3f, color = "white" } }, { "SlowDuration", new Variable() { value = 3, color = "white" } }, { "Timer", new Variable() { value = 0f, color = "white" } } },
            (item, stats, _) =>
            {
                var controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong damager, ref int damage) =>
                {
                    if ((float)item.variables["Timer"].value >= (float)item.variables["MoveDuration"].value)
                    {
                        var effectManager = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<EffectManager>();
                        if(effectManager != null)
                        {
                            effectManager.AddEffect("slow", (int)item.variables["SlowDuration"].value, (int)(float)item.variables["SpeedMult"].value, stats);
                            item.variables["Timer"].value = 0f;
                        }
                    }
                });
                AddToAction(item, () => stats.stats.speed.ChangeValueMult, (value) => stats.stats.speed.ChangeValueMult = value, (ref int speed, int oldSpeed) =>
                {
                    if((float)item.variables["Timer"].value >= (float)item.variables["MoveDuration"].value)
                    {
                        speed = (int)(speed * (1 + ((float)item.variables["SpeedMult"].value / 100f)));
                    }
                });
            }, (item, stats, _) =>
            {
                var mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
                var dir = (mouseWorldPos - stats.transform.position).normalized;
                var rb = stats.GetComponent<Rigidbody2D>();
                rb.velocity = Vector2.zero;
                rb.AddForce(dir * 250, ForceMode2D.Impulse);
                item.StartCooldown();
            }, (item, stats, _) =>
            {
                var rb = stats.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    if (rb.velocity != Vector2.zero)
                        item.variables["Timer"].value = Mathf.Min((float)item.variables["Timer"].value + Time.deltaTime,3.5f);
                    else
                        item.variables["Timer"].value = Mathf.Max((float)item.variables["Timer"].value - Time.deltaTime*2f,0);
                }
                
            });

        AddItemWithVariables("Jesters Dagger", "dagger_01", CharacterType.Damage, "On use blink to the targeted position. Afterwards become invisible for {InvisibleDuration} seconds", new StatBlock(20, 0, 15, 5, 50, 0), 2100, 40,
           new Dictionary<string, Variable>() { { "InvisibleDuration", new Variable() { value = 30, color = "white" } } },
           null, (item, stats, _) =>
           {
               var mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
               var dir = (mouseWorldPos - stats.transform.position);
               float distance = dir.magnitude;
               dir = dir.normalized;
               RaycastHit2D[] hits = Physics2D.RaycastAll(stats.transform.position, dir, Math.Min(distance,Mathf.Sqrt(5)));
               bool hitEnvironment = false;
               Vector2 point = Vector2.zero;
               foreach (var hit in hits)
               {
                   if(hit.collider.gameObject.layer == LayerMask.NameToLayer("Environment"))
                   {
                       hitEnvironment = true;
                       point = hit.point;
                   }
               }
               if (hitEnvironment)
                   stats.transform.position = point;
               else
                   stats.transform.position = stats.transform.position + dir * 5f;
               stats.GetComponent<EffectManager>()?.AddEffect("invisible", (int)item.variables["InvisibleDuration"].value, 1, stats);
               item.StartCooldown();
           });

        AddItemWithVariables("Future Orb", "crystal_orb", CharacterType.Damage, "Store {DamagePercent}% of damage dealt by special. Hitting a target with a special below the stored amount instantly kills them", new StatBlock(0, 25, 0, 0, 100, 3), 2400, 20,
           new Dictionary<string, Variable>() { { "DamagePercent", new Variable() { value = 20f, color = "blue" } }, { "Stacks", new Variable() { value = 0 } } },
           (item, stats, _) =>
           {
               var controller = stats.GetComponent<AbstractSpecial>();
               AddToAction(item, () => controller.OnTargetHit, (value) => controller.OnTargetHit = value, (ulong target, ulong user, ref int amount) =>
               {
                   if (!item.CanUse) return;
                   item.variables["Stacks"].value = (int)((int)item.variables["Stacks"].value + amount *((float)item.variables["DamagePercent"].value/100f));

                   var stats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<CharacterStats>();
                   if(stats != null)
                   {
                       if(stats.Health <= (int)item.variables["Stacks"].value)
                       {
                           amount = 10000;
                           item.variables["Stacks"].value = 0;
                           item.StartCooldown();
                       }
                   }
                   item.UpdateText(item.variables["Stacks"].value.ToString());
               });
           });

        AddItemWithVariables("Wanted Poster", "wanted_poster", CharacterType.Damage, "Mark a target as wanted for {WantedDuration} seconds. Wanted targets take {DamagePercent}% more damage and killing them rewards an additional {DamagePercent}% of targets HP as gold", new StatBlock(25, 0, 10, 0, 0, 0), 2500, 60,
           new Dictionary<string, Variable>() { { "WantedDuration", new Variable() { value = 10, color = "white" } }, { "DamagePercent", new Variable() { value = 10, color = "white" } } }, null,
           (item, stats, _) =>
           {
               if (!item.CanUse) return;
               var controller = stats.GetComponent<PlayerController>();
               var targetStats = controller.HoveredStats;
               if (targetStats == null) return;
               if (targetStats.gameObject.layer == stats.gameObject.layer) return;
               targetStats.GetComponent<EffectManager>()?.AddEffect("wanted", (int)item.variables["WantedDuration"].value, (int)item.variables["DamagePercent"].value, stats);
               item.StartCooldown();
           });

        AddItemWithVariables("Guillotine", "guillotine", CharacterType.Damage, "Executes players below {ExecutePerc}% of their Maximum Health. Can only accure once every {Cooldown} seconds",
            new StatBlock(25, 0, 10, 5, 0, 0), 2000, 10,
            new Dictionary<string, Variable>() { { "ExecutePerc", new Variable() { value = 5f, color = "green" } } }, (item, stats, _) =>
            {
                var controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong damager, ref int damage) =>
                {
                    if (!item.CanUse) return;
                    var targetStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<PlayerStats>();
                    if (targetStats == null) return;
                    if (targetStats.Health - damage < targetStats.stats.health.Value * ((float)item.variables["ExecutePerc"].value / 100f))
                    {
                        damage = 10000;
                        item.StartCooldown();
                    }
                });
            });

        AddItemWithVariables("Bolstering Gloves", "glove_01", CharacterType.Damage, "Gain {DamageReductionPerc}% Damage Reduction and {AttackSpeed}% Attack Speed until the first hit. Can only accure once every {Cooldown} seconds",
            new StatBlock(35, 0, 0, 5, 0, 0), 1600, 20,
            new Dictionary<string, Variable>() { { "DamageReductionPerc", new Variable() { value = 30, color = "grey" } }, { "AttackSpeed", new Variable() { value = 30, color = "yellow" } } }, (item, stats, _) =>
            {
                AddToAction(item, () => stats.stats.damageReduction.ChangeValueAdd, (value) => stats.stats.damageReduction.ChangeValueAdd = value, (ref int value, int _) =>
                {
                    if (item.CanUse) value += (int)item.variables["DamageReductionPerc"].value;
                });
                AddToAction(item, () => stats.stats.attackSpeed.ChangeValueAdd, (value) => stats.stats.attackSpeed.ChangeValueAdd = value, (ref int value, int _) =>
                {
                    if (item.CanUse) value += (int)item.variables["AttackSpeed"].value;
                });
                AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (_, _) =>
                {
                    if (item.CanUse)
                        item.StartCooldown();
                });
            });


        var spear = AddItem("Battlemage Spear", "spear_01", CharacterType.Support, "On use reset current special cooldown", new StatBlock(0, 20, 0, 0, 50, 0), 2000, 40,
            null, (item, stats, _) =>
            {
                stats.GetComponent<AbstractSpecial>().SetCooldown(0);
                item.StartCooldown();
            });

        var glove = AddItemWithVariables("Sharing Leaf", "leaf", CharacterType.Support, "On heal reduce the cooldown of the special by {CooldownReduce} seconds. Can only accure once every {Cooldown} seconds",
           new StatBlock(0, 25, 0, 0, 50, 0), 2000, 2,
           new Dictionary<string, Variable>() { { "CooldownReduce", new Variable() { value = 2 } } }, (item, stats, _) =>
           {
               var controller = stats.GetComponent<PlayerController>();
               AddToAction(item, () => controller.OnHeal, (value) => controller.OnHeal = value, (ulong target, ulong user, ref int amount) =>
               {
                   if (item.CanUse)
                   {
                       controller.GetComponent<AbstractSpecial>().ReduceCooldown((int)item.variables["CooldownReduce"].value);
                       item.StartCooldown();
                   }
               });
           });
        spear.sameItems.Add(glove.ID);
        glove.sameItems.Add(spear.ID);

        AddItemWithVariables("Druids Wand", "wand_02", CharacterType.Support, "Targets below {Threshold}% HP gain an additional {Addition}% healing",
           new StatBlock(0, 25, 0, 5, 0, 0), 1400, 0,
           new Dictionary<string, Variable>() { { "Threshold", new Variable() { value = 50f, color = "green" } }, { "Addition", new Variable() { value = 20f, color = "green" } } }, (item, stats, _) =>
           {
               var controller = stats.GetComponent<PlayerController>();
               AddToAction(item, () => controller.OnHeal, (value) => controller.OnHeal = value, (ulong target, ulong user, ref int amount) =>
               {
                   var stats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<CharacterStats>();
                   if (stats == null) return;
                   if (stats.Health < stats.stats.health.Value * ((float)item.variables["Threshold"].value / 100f))
                       amount += (int)(amount * ((float)item.variables["Threshold"].value / 100f));
               });
           });

        AddItemWithVariables("Void Wand", "wand_01", CharacterType.Damage, "While not using the special, gain {StackPerSecond} Stacks per second, up to {MaxStacks}. The next Special hit deals an additional damage equal to the amount of stacks",
           new StatBlock(0, 30, 0, 10, 0, 0), 2200, 3,
           new Dictionary<string, Variable>() { { "StackPerSecond", new Variable() { value = 2, color = "white" } }, { "MaxStacks", new Variable() { value = 200, color = "white" } }, { "Stacks", new Variable() { value = 0, color = "white" } }, { "Timer", new Variable() { value = 0f } } }, (item, stats, _) =>
           {
               var controller = stats.GetComponent<AbstractSpecial>();
               
               AddToAction(item, () => controller.OnTargetHit, (value) => controller.OnTargetHit = value, (ulong target, ulong user, ref int amount) =>
               {
                   amount += (int)item.variables["Stacks"].value;
                   item.variables["Stacks"].value = 0;
                   item.UpdateText(item.variables["Stacks"].value.ToString());
                   item.StartCooldown();
               });
           }, null,
            (item, stats, _) =>
            {
                if ((int)item.variables["Stacks"].value < (int)item.variables["MaxStacks"].value && item.CanUse)
                {
                    item.variables["Timer"].value = (float)item.variables["Timer"].value - Time.deltaTime;
                    if ((float)item.variables["Timer"].value <= 0)
                    {
                        item.variables["Stacks"].value = (int)item.variables["Stacks"].value + (int)item.variables["StackPerSecond"].value;
                        item.UpdateText(item.variables["Stacks"].value.ToString());
                        item.variables["Timer"].value = 1f;
                    }
                }
            });

        AddItemWithVariables("Scroll of Sacrifice", "scroll", CharacterType.Support, "Target a teammate to sacrifice {Sacrifice}% of own HP and heal the target for the same amount",
           new StatBlock(0, 15, 0, 0, 100, 0), 1700, 60,
           new Dictionary<string, Variable>() { { "Sacrifice", new Variable() { value = 50f, color = "green" } } }, null, (item, stats, _) =>
           {
               var controller = stats.GetComponent<PlayerController>();
               var targetStats = controller.HoveredStats;
               if (targetStats == null) return;
               if (targetStats.gameObject.layer != stats.gameObject.layer) return;
               int amount = (int)(stats.Health * ((float)item.variables["Sacrifice"].value / 100f));
               if (stats.stats.health.Value - amount <= 0) return;
               stats.TakeDamage(amount, Vector2.zero, stats);
               controller.Heal(targetStats, amount);
               item.StartCooldown();
           });

        AddItemWithVariables("Magic Blade", "sword_01", CharacterType.Damage, "The next attack after a special deals extra {Damage} damage. Can only accure once every {Cooldown} seconds",
           new StatBlock(25, 20, 0, 0, 0, 0), 1200, 2,
           new Dictionary<string, Variable>() { { "BaseDamage", new Variable() { value = 50 } }, { "Damage", new Variable() { value = 50, color = "red" } }, { "DamageScaling", new Variable() { value = 20f, color = "red" } }, { "Triggered", new Variable() { value = false } } }, (item, stats, _) =>
           {
               AbstractSpecial special = stats.GetComponent<AbstractSpecial>();
               PlayerAttack controller = stats.GetComponent<PlayerAttack>();
               AddToAction(item, () => special.onSpecial, (AbstractSpecial.SpecialDelegate value) => special.onSpecial = value, () =>
               {
                   if (item.CanUse)
                   {
                       item.variables["Triggered"].value = true;
                       item.StartCooldown();
                   }
               });
               AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong _, ulong _, ref int damage) =>
               {
                   if ((bool)item.variables["Triggered"].value)
                   {
                       damage += (int)item.variables["Damage"].value;
                       item.variables["Triggered"].value = false;
                   }
               });
           }, null, (item, stats, _) =>
           {
               item.variables["Damage"].value = (int)((int)item.variables["BaseDamage"].value + stats.stats.damage.Value * ((float)item.variables["DamageScaling"].value / 100f));
           });
        AddItemWithVariables("Undying Helmet", "headgear_01", CharacterType.Tank, "When falling below {Threshold}% HP regenerate {HP}% HP every {Time} seconds until at {HPThreshold}% health. Can only happen every {Cooldown} seconds.",
           new StatBlock(0, 0, 0, 0, 200, 10), 2000, 90,
           new Dictionary<string, Variable>() { { "Timer", new Variable() { value = 0f } },
               { "Time", new Variable() { value = 0.2f } },
               { "Triggered", new Variable() { value = false } },
               { "Threshold", new Variable() { value = 10f, color = "green" } },
               { "HP", new Variable() { value = 10f, color = "green" } },
               { "HPThreshold", new Variable() { value = 70f, color = "green" } }}, null, null, (item, stats, _) =>
           {
               if ((bool)item.variables["Triggered"].value)
               {
                   if ((float)item.variables["Timer"].value > 0)
                   {
                       item.variables["Timer"].value = (float)item.variables["Timer"].value - Time.deltaTime;
                       if ((float)item.variables["Timer"].value <= 0)
                       {
                           stats.Heal((int)(stats.stats.health.Value * (float)item.variables["HP"].value / 100f));
                           if (stats.Health < stats.stats.health.Value * ((float)item.variables["HPThreshold"].value/100f))
                               item.variables["Timer"].value = (float)item.variables["Time"].value;
                           else
                               item.variables["Triggered"].value = false;
                       }
                   }
               }
               if (!item.CanUse) return;
               if (stats.Health < stats.stats.health.Value * ((float)item.variables["Threshold"].value / 100f))
               {
                   item.variables["Timer"].value = (float)item.variables["Time"].value;
                   item.variables["Triggered"].value = true;
                   item.StartCooldown();
               }
           });
        AddItemWithVariables("First Strike Dagger", "stone_sword", CharacterType.Damage, "When not taking damage for {Cooldown} seconds, the next attack deals an additional {MaxHP}% Max HP damage",
           new StatBlock(25, 0, 15, 0, 0, 0), 1600, 10,
           new Dictionary<string, Variable>() { { "MaxHP", new Variable() { value = 5f, color = "green" } } }, (item, stats, _) =>
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
                           targetStats.TakeDamage((int)(targetStats.stats.health.Value * ((float)item.variables["MaxHP"].value / 100f)), Vector2.zero, stats);
                           item.StartCooldown();
                       }
                   }
               });
           });

        AddItemWithVariables("Magic-Infused Glove", "glove_02", CharacterType.Damage, "Converts {Ratio}% of damage to special damage. Reduces damage by {Ratio}%",
           new StatBlock(0, 30, 0, 0, 0, 0), 1600, 0,
           new Dictionary<string, Variable>() { { "Ratio", new Variable() { value = 50f, color = "red" } } }, (item, stats, _) =>
           {
               PlayerController controller = stats.GetComponent<PlayerController>();
               AddToAction(item, () => stats.stats.specialDamage.ChangeValueAdd, (value) => stats.stats.specialDamage.ChangeValueAdd = value, (ref int damage, int old) =>
               {
                   damage += (int)(stats.stats.damage.Value * ((float)item.variables["Ratio"].value / 100f));
               });
               AddToAction(item, () => stats.stats.damage.ChangeValueMult, (value) => stats.stats.damage.ChangeValueMult = value, (ref int damage, int old) =>
               {
                   damage = (int)(damage * ((float)item.variables["Ratio"].value / 100f));
               });
           });

        /*var box = AddItem("Magical Box", "wooden_box", CharacterType.Support, "On use place a torch",
           new StatBlock(0, 15, 0, 5, 0, 0), 1400, 10, null, (item, stats, _) =>
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
           });*/

        var ring = AddItemWithVariables("Miners Ring", "ring_02", CharacterType.Support, "Increase own light range by {LightMult}x. Hitting a target applies lit to them for {LitDuration} seconds.", new StatBlock(0, 15, 0, 5, 50, 0), 1200,5,
            new Dictionary<string, Variable>() { { "LightMult", new Variable() { value = 2f } }, { "LitDuration", new Variable() { value = 5 } } }, (item, stats, _) =>
            {
                PlayerAttack controller = stats.GetComponent<PlayerAttack>();
                var light = stats.GetComponentInChildren<Light2D>();
                light.pointLightOuterRadius *= (float)item.variables["LightMult"].value;
                item.onUnequip += (_, _, _) =>
                {
                    light.pointLightOuterRadius /= (float)item.variables["LightMult"].value;
                };
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
                {
                    if (!item.CanUse) return;
                    var targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target];
                    if (targetObject.GetComponent<PlayerStats>() == null) return;
                    targetObject.GetComponent<EffectManager>()?.AddEffect("lit", (int)item.variables["LitDuration"].value, 3, stats);
                    item.StartCooldown();
                });
            });
        //ring.sameItems.Add(box.ID);
        //box.sameItems.Add(ring.ID);

        AddItemWithVariables("Bleeding Scythe", "scythe", CharacterType.Damage, "Applies Bleeding for {BleedTime} seconds on attack. The Bleed deals {Damage} every 1 second. Can only happen every {Cooldown} seconds.", new StatBlock(25, 0, 10, 5, 25, 0), 1400, 2,
            new Dictionary<string, Variable>() { { "BleedTime", new Variable() { value = 5 } }, { "BaseDamage", new Variable() { value = 5 } }, { "Damage", new Variable() { value = 5, color = "red" } }, { "DamageScaling", new Variable() { value = 10f, color = "red" } } }, (item, stats, _) =>
            {
                PlayerAttack controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
                {
                    if (item.CanUse)
                    {
                        var targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target];
                        targetObject.GetComponent<EffectManager>()?.AddEffect("bleed", (int)item.variables["BleedTime"].value, (int)item.variables["Damage"].value, stats);
                        item.StartCooldown();
                    }
                });
            }, null, (item, stats, _) =>
            {
                item.variables["Damage"].value = (int)((int)item.variables["BaseDamage"].value + stats.stats.damage.Value * ((float)item.variables["DamageScaling"].value / 100f));
            });

        AddItemWithVariables("Cursed Shield", "shield_02", CharacterType.Tank, "On taking damage curse the target for {Duration} seconds, reducing healing by {Amount}%", new StatBlock(0, 0, 0, 0, 150, 5), 1600, 0,
            new Dictionary<string, Variable>() { { "Duration", new Variable() { value = 3 } }, { "Amount", new Variable() { value = 30, color = "purple" } } }, (item, stats, _) =>
            {
                AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (ulong damagerId, int damage) =>
                {
                    var targetStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[damagerId].GetComponent<PlayerStats>();
                    if (targetStats != null)
                    {
                        var effects = targetStats.GetComponent<EffectManager>();
                        effects?.AddEffect("curse", (int)item.variables["Duration"].value, (int)item.variables["Amount"].value, stats);
                    }
                });
            });
        AddItemWithVariables("Cursed Lantern", "lantern", CharacterType.Damage, "On dealing damage curse the target for {Duration} seconds, reducing healing by {Amount}%", new StatBlock(20, 0, 10, 0, 50, 0), 1600, 0,
            new Dictionary<string, Variable>() { { "Duration", new Variable() { value = 3 } }, { "Amount", new Variable() { value = 30, color = "purple" } } }, (item, stats, _) =>
            {
                PlayerAttack controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
                {
                    var targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target];
                    targetObject.GetComponent<EffectManager>()?.AddEffect("curse", (int)item.variables["Duration"].value, (int)item.variables["Amount"].value, stats);
                });
            });
        AddItemWithVariables("Cursed Book", "book", CharacterType.Support, "On use curse the target for {Duration} seconds, reducing healing by {Amount}%", new StatBlock(0, 15, 0, 5, 50, 0), 1600, 30,
            new Dictionary<string, Variable>() { { "Duration", new Variable() { value = 3 } }, { "Amount", new Variable() { value = 100, color = "purple" } } }, null, (item, stats, _) =>
            {
                var controller = stats.GetComponent<PlayerController>();
                if (controller == null) return;
                if (controller.HoveredStats == null) return;
                if (item.CanUse)
                {
                    controller.HoveredStats.GetComponent<EffectManager>()?.AddEffect("curse", (int)item.variables["Duration"].value, (int)item.variables["Amount"].value, stats);
                    item.StartCooldown();
                }
            });
        AddItemWithVariables("Mantle of Aura", "mantle", CharacterType.Support, "On use swap between 3 different auras (Speed, Damage, Survivability). \nAura of Speed: Gain {Amount}% Attack Speed and Movement Speed.", new StatBlock(5, 5, 5, 5, 5, 5), 2000, 5,
            new Dictionary<string, Variable>() { { "Amount", new Variable() { value = 5, color = "yellow" } }, { "HPAmount", new Variable() { value = 50, color = "green" } }, { "CurrentAura", new Variable() { value = 0 } } }, (item, stats, _) =>
            {
                AddToAction(item, () => stats.stats.speed.ChangeValueAdd, (value) => stats.stats.speed.ChangeValueAdd = value, (ref int value, int _) =>
                {
                    if ((int)item.variables["CurrentAura"].value == 0)
                        value += (int)item.variables["Amount"].value;
                });
                AddToAction(item, () => stats.stats.attackSpeed.ChangeValueAdd, (value) => stats.stats.attackSpeed.ChangeValueAdd = value, (ref int value, int _) =>
                {
                    if ((int)item.variables["CurrentAura"].value == 0)
                        value += (int)item.variables["Amount"].value;
                });
                AddToAction(item, () => stats.stats.damage.ChangeValueAdd, (value) => stats.stats.damage.ChangeValueAdd = value, (ref int value, int _) =>
                {
                    if ((int)item.variables["CurrentAura"].value == 1)
                        value += (int)item.variables["Amount"].value;
                });
                AddToAction(item, () => stats.stats.specialDamage.ChangeValueAdd, (value) => stats.stats.specialDamage.ChangeValueAdd = value, (ref int value, int _) =>
                {
                    if ((int)item.variables["CurrentAura"].value == 1)
                        value += (int)item.variables["Amount"].value;
                });
                AddToAction(item, () => stats.stats.health.ChangeValueAdd, (value) => stats.stats.health.ChangeValueAdd = value, (ref int value, int _) =>
                {
                    if ((int)item.variables["CurrentAura"].value == 2)
                        value += (int)item.variables["HPAmount"].value;
                });
                AddToAction(item, () => stats.stats.damageReduction.ChangeValueAdd, (value) => stats.stats.damageReduction.ChangeValueAdd = value, (ref int value, int _) =>
                {
                    if ((int)item.variables["CurrentAura"].value == 2)
                        value += (int)item.variables["Amount"].value;
                });
            }, (item, stats, _) =>
            {
                if (!item.CanUse) return;
                item.variables["CurrentAura"].value = (((int)item.variables["CurrentAura"].value) + 1) % 3;
                switch ((int)item.variables["CurrentAura"].value)
                {
                    case 0:
                        item.variables["Amount"].color = "yellow";
                        item.description = "On use swap between 3 different auras (Speed, Damage, Survivability). \nAura of Speed: Gain {Amount}% Attack Speed and Movement Speed.";
                        break;
                    case 1:
                        item.variables["Amount"].color = "red";
                        item.description = "On use swap between 3 different auras (Speed, Damage, Survivability). \nAura of Damage: Gain {Amount} Damage and Special Damage.";
                        break;
                    case 2:
                        item.variables["Amount"].color = "grey";
                        item.description = "On use swap between 3 different auras (Speed, Damage, Survivability). \nAura of Tank: Gain {HPAmount} Max HP and {Amount}% Damage Reduction";
                        break;
                }
                item.StartCooldown();
            });
        AddItemWithVariables("Magical Pouch", "pouch", CharacterType.Support, "Increases maximum Resource by {Amount}%.", new StatBlock(0, 25, 0, 0, 25, 0), 1400, 0,
            new Dictionary<string, Variable>() { { "Amount", new Variable() { value = 50, color = "blue" } } }, (item, stats, _) =>
            {
                AddToAction(item, () => stats.stats.resource.ChangeValueMult, (value) => stats.stats.resource.ChangeValueMult = value, (ref int resource, int old) =>
                {
                    resource += Math.Max(1, (int)(resource * ((int)item.variables["Amount"].value / 100f)));
                });
            });
        AddItemWithVariables("Timewarp Necklace", "necklace_01", CharacterType.Damage, "On hit apply Timewarped to the target hit for {Duration} seconds, reducing Attack Speed and Speed by {Amount}, decaying over time. Can only happen every {Cooldown} seconds.", new StatBlock(20, 0, 15, 0, 50, 0), 1600, 5,
            new Dictionary<string, Variable>() { { "Duration", new Variable() { value = 3 } }, { "Amount", new Variable() { value = 25, color = "yellow" } } }, (item, stats, _) =>
            {
                PlayerAttack controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
                {
                    var targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target];
                    targetObject.GetComponent<EffectManager>()?.AddEffect("timewarped", (int)item.variables["Duration"].value, (int)item.variables["Amount"].value, stats);
                });
            });
        AddItemWithVariables("Absorption Shield", "shield_01", CharacterType.Tank, "On damaging a player gain {HPAmount} Maximum Health and heal for {HealAmount}% Maximum Health. Can only happen every {Cooldown} seconds.", new StatBlock(0, 0, 0, 0, 300, 5), 1600, 5,
            new Dictionary<string, Variable>() { { "HPAmount", new Variable() { value = 50, color = "green" } }, { "HealAmount", new Variable() { value = 2, color = "green" } }, { "Stacks", new Variable() { value = 0 } } }, (item, stats, _) =>
            {
                PlayerAttack controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => stats.stats.health.ChangeValueAdd, (value) => stats.stats.health.ChangeValueAdd = value, (ref int resource, int old) =>
                {
                    resource += (int)item.variables["Stacks"].value * (int)item.variables["HPAmount"].value;
                });
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
                {
                    if (!item.CanUse) return;
                    var targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target];
                    var playerStats = targetObject?.GetComponent<CharacterStats>();
                    if (playerStats != null)
                    {
                        item.variables["Stacks"].value = (int)item.variables["Stacks"].value + 1;
                        stats.Heal((int)(stats.stats.health.Value * ((int)item.variables["HealAmount"].value / 100f)));
                        item.UpdateText(item.variables["Stacks"].value.ToString());
                        item.StartCooldown();
                    }
                });
            });
        AddItemWithVariables("Highmetal Anvil", "anvil", CharacterType.Tank, "On taking damage gain {DamageReduction}% Damage Reduction stacking up to {StackAmount} times. At maximum stacks each Stack instead adds {MaxDamageReduction}% Damage Reduction. Stacks fall of after {Cooldown} seconds of not taking damage.", new StatBlock(0, 0, 0, 0, 250, 5), 1700, 5,
            new Dictionary<string, Variable>() { { "DamageReduction", new Variable() { value = 2, color = "grey" } }, { "MaxDamageReduction", new Variable() { value = 3, color = "grey" } }, { "StackAmount", new Variable() { value = 10 } }, { "Stacks", new Variable() { value = 0 } } }, (item, stats, _) =>
            {
                PlayerAttack controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => stats.stats.damageReduction.ChangeValueAdd, (value) => stats.stats.damageReduction.ChangeValueAdd = value, (ref int value, int _) =>
                {
                    int drValue = 0;
                    if ((int)item.variables["Stacks"].value == (int)item.variables["StackAmount"].value)
                        drValue = (int)item.variables["MaxDamageReduction"].value;
                    else
                        drValue = (int)item.variables["DamageReduction"].value;
                    value += (int)item.variables["Stacks"].value * drValue;
                });
                AddToAction(item, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (ulong damager, int damage) =>
                {
                    if ((int)item.variables["Stacks"].value < (int)item.variables["StackAmount"].value)
                    {
                        item.variables["Stacks"].value = (int)item.variables["Stacks"].value + 1;
                        item.UpdateText(item.variables["Stacks"].value.ToString());
                    }
                    item.StartCooldown();
                });
            }, null,
             (item, stats, _) =>
             {
                 if (item.CanUse && (int)item.variables["Stacks"].value > 0)
                 {
                     item.variables["Stacks"].value = 0;
                     item.UpdateText("");
                 }
             });
        AddItemWithVariables("Ethereal Ring", "ring_01", CharacterType.Damage, "On use change into ethereal form. While in this form no damage will be taken but movement and attacking are disabled. Change back to normal after {Duration} seconds.", new StatBlock(15, 15, 0, 0, 0, 5), 1700, 60,
            new Dictionary<string, Variable>() { { "Duration", new Variable() { value = 3 } }}, null,
            (item, stats, _) =>
            {
                if (item.CanUse)
                {
                    stats.GetComponent<EffectManager>().AddEffect("ethereal", (int)item.variables["Duration"].value, 1, stats);
                    item.StartCooldown();
                }
            });
    }

    public override Item GetByID(string id)
    {
        if (items[id] is Consumable)
            return new Consumable(items[id] as Consumable);
        return new Item(items[id]);
    }

    public Consumable AddConsumable(string name, string spritePath, CharacterType type = CharacterType.None, string description = "", StatBlock stats = null,
        int cost = 0, ItemFunction onBuy = null)
    {
        Consumable item = new Consumable(name);
        item.type = type;
        item.description = description;
        item.stats = stats;
        item.icon = Resources.Load<Sprite>("Items/" + spritePath);
        item.cost = cost;
        item.onBuy = onBuy;
        items.Add(item.ID, item);
        return item;
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
        item.icon = Resources.Load<Sprite>("Items/" + spritePath);
        item.cost = cost;
        items.Add(item.ID, item);
        return item;
    }

    public Item AddItemWithVariables(string name, string spritePath, CharacterType type = CharacterType.None, string description = "", StatBlock stats = null,
        int cost = 0, float cooldown = 0, Dictionary<string, Variable> variables = null, ItemFunction onEquip = null, ItemFunction onUse = null, ItemFunction onUpdate = null)
    {
        var item = AddItem(name, spritePath, type, description, stats, cost, cooldown, onEquip, onUse, onUpdate);
        item.variables = variables;
        return item;
    }

    public override Item[] GetAll()
    {
        var items = this.items.Values.ToList();
        items.Sort((x, y) => x.cost.CompareTo(y.cost));
        return items.Select(x => {
            if (x is Consumable)
                return new Consumable(x as Consumable);
            return new Item(x);
        }).ToArray();
    }
}
