using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    protected override void Create()
    {
        items.Clear();
        var potion = AddItemWithVariables("hp_potion", new Dictionary<string, Variable>() { { "HP", new Variable() { value = 20 } }, { "Duration", new Variable() { value = 10 } } }, null, (Item item, CharacterStats stats, int slot) =>
        {
            stats.GetComponent<EffectManager>()?.AddEffect("potion", (int)item.variables["Duration"].value, (int)item.variables["HP"].value, stats);
            stats.GetComponent<Inventory>().RemoveItem(slot);
        });
        potion.multiple = true;

        AddConsumable("runestone_i", (Item item, CharacterStats stats, int slot) =>
        {
            if (!stats.IsLocalPlayer) return;
            stats.GetComponent<AbstractSpecial>().UnlockUpgrade(0);
        });
        AddConsumable("runestone_ii", (Item item, CharacterStats stats, int slot) =>
        {
            if (!stats.IsLocalPlayer) return;
            stats.GetComponent<AbstractSpecial>().UnlockUpgrade(1);
        });
        AddConsumable("runestone_iii", (Item item, CharacterStats stats, int slot) =>
        {
            if (!stats.IsLocalPlayer) return;
            stats.GetComponent<AbstractSpecial>().UnlockUpgrade(2);
        });
        var fateScroll = AddConsumable("scroll_of_fate", (Item item, CharacterStats stats, int slot) =>
        {
            if (!stats.IsLocalPlayer) return;
            ShopPanel.Instance.Toggle();
            (GameManager.instance as ArenaGameManager).CardSelection.gameObject.SetActive(true);
        });
        fateScroll.multiple = true;

        List<Item> boots = new List<Item>();
        var boot = AddItem("knights_sandles");
        boots.Add(boot);
        boot = AddItem("magical_sandles");
        boots.Add(boot);
        boot = AddItem("giant_boots");
        boots.Add(boot);
        boot = AddItem("plated_boots");
        boots.Add(boot);

        foreach (var item in boots)
        {
            item.sameItems.AddRange(boots.FindAll(x => x.ID != item.ID).Select(x => x.ID));
        }

        AddItemWithVariables("knights_chestplate", new Dictionary<string, Variable>() { { "BaseDamage", new Variable() { value = 10 } }, { "MaxHealthPerc", new Variable() { value = 2 } }, { "Damage", new Variable() { value = 10, color = "green" } } }, (Item item, CharacterStats stats, int slot) =>
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

        AddItemWithVariables("void_chestplate", new Dictionary<string, Variable>() { { "HPThreshold", new Variable() { value = 2f } } }, (Item item, CharacterStats stats, int slot) =>
            {
                AddToAction(item, () => stats.OnServerTakeDamage, (value) => stats.OnServerTakeDamage = value, (ulong damager, ref int damage) =>
                {
                    if (damage <= stats.stats.health.Value * ((float)item.variables["HPThreshold"].value / 100f))
                    {
                        damage = 0;
                    }
                });
            }, null, null);

        AddItemWithVariables("lifeline", new Dictionary<string, Variable>() { { "Timer", new Variable() { value = 1f } }, { "HealAmount", new Variable() { value = 2.5f, color = "green" } }, { "Time", new Variable() { value = 2f } } }, (item, stats, _) =>
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
                    stats.Heal((int)(stats.stats.health.Value * ((float)item.variables["HealAmount"].value / 100f)), stats);
                    item.variables["Timer"].value = item.variables["Time"].value;
                }
            }
        });

        List<Item> healItems = new List<Item>();

        var blood_blade = AddItemWithVariables("bloodlords_blade",
            new Dictionary<string, Variable>() { { "LifeSteal", new Variable() { value = 5f, color = "red" } } }, (item, stats, _) =>
        {
            var controller = stats.GetComponent<PlayerAttack>();
            AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong _, ulong _, ref int damage) =>
            {
                stats.Heal((int)(damage * ((float)item.variables["LifeSteal"].value / 100f)), stats);
            });
        });

        var blood_spear = AddItemWithVariables("bloodlords_spear", new Dictionary<string, Variable>() { { "CurrentHealthPerc", new Variable() { value = 5f, color = "green" } } }, (item, stats, _) =>
            {
                var controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
                {
                    var targetStats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<PlayerStats>();
                    if (targetStats == null) return;
                    var additionalDamage = (int)(targetStats.Health * ((float)item.variables["CurrentHealthPerc"].value / 100f));
                    damage += additionalDamage;
                    stats.Heal(additionalDamage,stats);
                });
            });


        var necromancyBook = AddItemWithVariables("book_of_necromancy", new Dictionary<string, Variable>() { { "HealAmount", new Variable() { value = 10f, color = "blue" } } },
           (item, stats, _) =>
           {
               var controller = stats.GetComponent<AbstractSpecial>();
               AddToAction(item, () => controller.OnTargetHit, (value) => controller.OnTargetHit = value, (ulong target, ulong user, ref int amount) =>
               {
                   stats.Heal((int)(amount * ((float)item.variables["HealAmount"].value / 100f)), stats);
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

        AddItemWithVariables("last_stand", new Dictionary<string, Variable>() { { "HealthPerc", new Variable() { value = 15f, color = "green" } }, { "FallOfTimer", new Variable() { value = 10f } }, { "DamageTaken", new Variable() { value = 0 } }, { "Timer", new Variable() { value = 0f } } }, (item, stats, _) =>
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
            stats.Heal((int)item.variables["DamageTaken"].value, stats);
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

        AddItemWithVariables("wind_sigil", new Dictionary<string, Variable>() { { "SpeedMult", new Variable() { value = 15f, color = "yellow" } }, { "MoveDuration", new Variable() { value = 3f, color = "white" } }, { "SlowDuration", new Variable() { value = 3, color = "white" } }, { "Timer", new Variable() { value = 0f, color = "white" } } },
            (item, stats, _) =>
            {
                var controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong damager, ref int damage) =>
                {
                    if ((float)item.variables["Timer"].value >= (float)item.variables["MoveDuration"].value)
                    {
                        var effectManager = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<EffectManager>();
                        if (effectManager != null)
                        {
                            effectManager.AddEffect("slow", (int)item.variables["SlowDuration"].value, (int)(float)item.variables["SpeedMult"].value, stats);
                            item.variables["Timer"].value = 0f;
                        }
                    }
                });
                AddToAction(item, () => stats.stats.speed.ChangeValueMult, (value) => stats.stats.speed.ChangeValueMult = value, (ref int speed, int oldSpeed) =>
                {
                    if ((float)item.variables["Timer"].value >= (float)item.variables["MoveDuration"].value)
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
                        item.variables["Timer"].value = Mathf.Min((float)item.variables["Timer"].value + Time.deltaTime, 3.5f);
                    else
                        item.variables["Timer"].value = Mathf.Max((float)item.variables["Timer"].value - Time.deltaTime * 2f, 0);
                }

            });

        AddItemWithVariables("jesters_dagger", new Dictionary<string, Variable>() { { "InvisibleDuration", new Variable() { value = 3, color = "white" } } },
           null, (item, stats, _) =>
           {
               var mouseWorldPos = Camera.main.ScreenToWorldPoint(InputManager.Instance.MousePosition);
               var dir = (mouseWorldPos - stats.transform.position);
               float distance = dir.magnitude;
               dir = dir.normalized;
               RaycastHit2D[] hits = Physics2D.RaycastAll(stats.transform.position, dir, Math.Min(distance, Mathf.Sqrt(5)));
               bool hitEnvironment = false;
               Vector2 point = Vector2.zero;
               foreach (var hit in hits)
               {
                   if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Environment"))
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

        AddItemWithVariables("future_orb", new Dictionary<string, Variable>() { { "DamagePercent", new Variable() { value = 20f, color = "blue" } }, { "Stacks", new Variable() { value = 0 } } },
           (item, stats, _) =>
           {
               var controller = stats.GetComponent<AbstractSpecial>();
               AddToAction(item, () => controller.OnTargetHit, (value) => controller.OnTargetHit = value, (ulong target, ulong user, ref int amount) =>
               {
                   if (!item.CanUse) return;
                   item.variables["Stacks"].value = (int)((int)item.variables["Stacks"].value + amount * ((float)item.variables["DamagePercent"].value / 100f));

                   var stats = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<CharacterStats>();
                   if (stats != null)
                   {
                       if (stats.Health <= (int)item.variables["Stacks"].value)
                       {
                           amount = 10000;
                           item.variables["Stacks"].value = 0;
                           item.StartCooldown();
                       }
                   }
                   item.UpdateText(item.variables["Stacks"].value.ToString());
               });
           });

        AddItemWithVariables("wanted_poster", new Dictionary<string, Variable>() { { "WantedDuration", new Variable() { value = 10, color = "white" } }, { "DamagePercent", new Variable() { value = 15, color = "white" } } }, null,
           (item, stats, _) =>
           {
               if (!item.CanUse) return;
               var controller = stats.GetComponent<PlayerController>();
               var targetStats = controller.HoveredStats;
               if (targetStats == null) return;
               if (controller.TeamController.HasSameTeam(stats.gameObject)) return;
               targetStats.GetComponent<EffectManager>()?.AddEffect("wanted", (int)item.variables["WantedDuration"].value, (int)item.variables["DamagePercent"].value, stats);
               item.StartCooldown();
           });

        AddItemWithVariables("pendant_of_time", new Dictionary<string, Variable>() { { "DazzingStrikeDuration", new Variable() { value = 10, color = "white" } }, { "DazzingStrikeAmount", new Variable() { value = 2, color = "white" } } }, null,
           (item, stats, _) =>
           {
               if (!item.CanUse) return;
               var controller = stats.GetComponent<PlayerController>();
               var targetStats = controller.HoveredStats;
               if (targetStats == null) return;
               if (!controller.TeamController.HasSameTeam(targetStats.gameObject)) return;
               targetStats.GetComponent<EffectManager>()?.AddEffect("dazzing_strike", (int)item.variables["DazzingStrikeDuration"].value, (int)item.variables["DazzingStrikeAmount"].value, stats);
               item.StartCooldown();
           });

        AddItemWithVariables("guillotine", new Dictionary<string, Variable>() { { "ExecutePerc", new Variable() { value = 5f, color = "green" } } }, (item, stats, _) =>
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

        AddItemWithVariables("bolstering_gloves", new Dictionary<string, Variable>() { { "DamageReductionPerc", new Variable() { value = 30, color = "grey" } }, { "AttackSpeed", new Variable() { value = 30, color = "yellow" } } }, (item, stats, _) =>
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


        AddItemWithVariables("battlemage_spear", new Dictionary<string, Variable>() { { "Duration", new Variable() { value = 5 } }, { "CooldownReduce", new Variable() { value = 0.1f } }, { "MinCooldown", new Variable() { value = 10 } } }, (item, stats, _) =>
        {
            var controller = stats.GetComponent<PlayerController>();
            AddToAction(item, () => controller.OnHeal, (value) => controller.OnHeal = value, (ulong target, ulong user, ref int amount) =>
            {
                if(item.cooldown > (int)item.variables["MinCooldown"].value)
                {
                    item.cooldown = item.cooldown - (float)item.variables["CooldownReduce"].value;
                }
            });
        }, 
        (item, stats, _) =>
        {
            if (!item.CanUse) return;
            var controller = stats.GetComponent<PlayerController>();
            if (controller == null) return;
            var targetStats = controller.HoveredStats;
            if (targetStats == null) return;
            if (controller.TeamController.HasSameTeam(stats.gameObject)) return;

            controller.HoveredStats.GetComponent<EffectManager>()?.AddEffect("silenced", (int)item.variables["Duration"].value, 1, stats);
            item.StartCooldown();
        });

        AddItemWithVariables("sharing_leaf",
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

        AddItemWithVariables("druids_wand",
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

        AddItemWithVariables("rejuvenation_branch",
           new Dictionary<string, Variable>() { { "Duration", new Variable() { value = 10, color = "green" } } }, (item, stats, _) =>
           {
               var controller = stats.GetComponent<PlayerController>();
               AddToAction(item, () => controller.OnHeal, (value) => controller.OnHeal = value, (ulong target, ulong user, ref int amount) =>
               {
                   var effectManager = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<EffectManager>();
                   if (effectManager == null) return;
                   effectManager.AddEffect("rejuvenation", (int)item.variables["Duration"].value, amount, stats);
               });
           });

        AddItemWithVariables("mask_of_happiness",
           new Dictionary<string, Variable>() { { "HealIncrease", new Variable() { value = 25f } }, { "HPThreshold", new Variable() { value = 80f, color = "green" } }, { "HPHealIncrease", new Variable() { value = 50f } } }, (item, stats, _) =>
           {
               var controller = stats.GetComponent<PlayerController>();
               AddToAction(item, () => controller.OnHeal, (value) => controller.OnHeal = value, (ulong target, ulong user, ref int amount) =>
               {
                   var healIncrease = 1f;
                   if (stats.Health >= stats.stats.health.Value * ((float)item.variables["HPThreshold"].value / 100f))
                   {
                       healIncrease = 1 + ((float)item.variables["HPHealIncrease"].value / 100f);
                   }
                   else
                   {
                       healIncrease = 1 + ((float)item.variables["HealIncrease"].value / 100f);
                   }
                   amount = (int)(amount * healIncrease);
               });
           });

        AddItemWithVariables("void_wand", new Dictionary<string, Variable>() { { "StackPerSecond", new Variable() { value = 2, color = "white" } }, { "MaxStacks", new Variable() { value = 200, color = "white" } }, { "Stacks", new Variable() { value = 0, color = "white" } }, { "Timer", new Variable() { value = 0f } } }, (item, stats, _) =>
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

        AddItemWithVariables("scroll_of_sacrifice", new Dictionary<string, Variable>() { { "Sacrifice", new Variable() { value = 50f, color = "green" } } }, null, (item, stats, _) =>
           {
               var controller = stats.GetComponent<PlayerController>();
               var targetStats = controller.HoveredStats;
               if (targetStats == null) return;
               if (!controller.TeamController.HasSameTeam(targetStats.gameObject)) return;
               int amount = (int)(stats.Health * ((float)item.variables["Sacrifice"].value / 100f));
               if (stats.stats.health.Value - amount <= 0) return;
               stats.TakeDamage(amount, Vector2.zero, stats);
               controller.Heal(targetStats, amount);
               item.StartCooldown();
           });

        AddItemWithVariables("magic_blade", new Dictionary<string, Variable>() { { "BaseDamage", new Variable() { value = 50 } }, { "Damage", new Variable() { value = 50, color = "red" } }, { "DamageScaling", new Variable() { value = 20f, color = "red" } }, { "Triggered", new Variable() { value = false } } }, (item, stats, _) =>
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
        AddItemWithVariables("undying_helmet", new Dictionary<string, Variable>() { { "Timer", new Variable() { value = 0f } },
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
                           stats.Heal((int)(stats.stats.health.Value * (float)item.variables["HP"].value / 100f), stats);
                           if (stats.Health < stats.stats.health.Value * ((float)item.variables["HPThreshold"].value / 100f))
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
        AddItemWithVariables("first_strike_dagger", new Dictionary<string, Variable>() { { "MaxHP", new Variable() { value = 5f, color = "green" } } }, (item, stats, _) =>
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

        AddItemWithVariables("magic-infused_glove", new Dictionary<string, Variable>() { { "Ratio", new Variable() { value = 50f, color = "red" } } }, (item, stats, _) =>
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

        var ring = AddItem("cleansing_ring", null, (item, stats, _) =>
            {
                if (!item.CanUse) return;
                var controller = stats.GetComponent<PlayerController>();
                if (controller == null) return;
                var targetStats = controller.HoveredStats;
                if (targetStats == null) return;
                Effect.EffectType type = !controller.TeamController.HasSameTeam(targetStats.gameObject) ? Effect.EffectType.Buff : Effect.EffectType.Debuff;
                targetStats.GetComponent<EffectManager>()?.EndAllEffects((effect) => effect.Type == type);
                item.StartCooldown();
            });

        AddItemWithVariables("bleeding_scythe", new Dictionary<string, Variable>() { { "BleedTime", new Variable() { value = 5 } }, { "BaseDamage", new Variable() { value = 5 } }, { "Damage", new Variable() { value = 5, color = "red" } }, { "DamageScaling", new Variable() { value = 10f, color = "red" } } }, (item, stats, _) =>
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

        AddItemWithVariables("cursed_shield", new Dictionary<string, Variable>() { { "Duration", new Variable() { value = 3 } }, { "Amount", new Variable() { value = 30, color = "purple" } } }, (item, stats, _) =>
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
        AddItemWithVariables("cursed_lantern", new Dictionary<string, Variable>() { { "Duration", new Variable() { value = 3 } }, { "Amount", new Variable() { value = 30, color = "purple" } } }, (item, stats, _) =>
            {
                PlayerAttack controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
                {
                    var targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target];
                    targetObject.GetComponent<EffectManager>()?.AddEffect("curse", (int)item.variables["Duration"].value, (int)item.variables["Amount"].value, stats);
                });
            });
        AddItemWithVariables("cursed_book", new Dictionary<string, Variable>() { { "Duration", new Variable() { value = 3 } }, { "Amount", new Variable() { value = 100, color = "purple" } } }, null, (item, stats, _) =>
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
        AddItemWithVariables("mantle_of_aura", new Dictionary<string, Variable>() { { "Amount", new Variable() { value = 5, color = "yellow" } }, { "HPAmount", new Variable() { value = 50, color = "green" } }, { "CurrentAura", new Variable() { value = 0 } } }, (item, stats, _) =>
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
        AddItemWithVariables("magical_pouch", new Dictionary<string, Variable>() { { "Amount", new Variable() { value = 50, color = "blue" } } }, (item, stats, _) =>
            {
                AddToAction(item, () => stats.stats.resource.ChangeValueMult, (value) => stats.stats.resource.ChangeValueMult = value, (ref int resource, int old) =>
                {
                    resource += Math.Max(1, (int)(resource * ((int)item.variables["Amount"].value / 100f)));
                });
            });
        AddItemWithVariables("timewarp_necklace", new Dictionary<string, Variable>() { { "Duration", new Variable() { value = 3 } }, { "Amount", new Variable() { value = 25, color = "yellow" } } }, (item, stats, _) =>
            {
                PlayerAttack controller = stats.GetComponent<PlayerAttack>();
                AddToAction(item, () => controller.OnAttack, (value) => controller.OnAttack = value, (ulong target, ulong _, ref int damage) =>
                {
                    var targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target];
                    targetObject.GetComponent<EffectManager>()?.AddEffect("timewarped", (int)item.variables["Duration"].value, (int)item.variables["Amount"].value, stats);
                });
            });
        AddItemWithVariables("absorption_shield", new Dictionary<string, Variable>() { { "HPAmount", new Variable() { value = 50, color = "green" } }, { "HealAmount", new Variable() { value = 2, color = "green" } }, { "Stacks", new Variable() { value = 0 } } }, (item, stats, _) =>
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
                        stats.Heal((int)(stats.stats.health.Value * ((int)item.variables["HealAmount"].value / 100f)), stats);
                        item.UpdateText(item.variables["Stacks"].value.ToString());
                        item.StartCooldown();
                    }
                });
            });
        AddItemWithVariables("highmetal_anvil", new Dictionary<string, Variable>() { { "DamageReduction", new Variable() { value = 2, color = "grey" } }, { "MaxDamageReduction", new Variable() { value = 3, color = "grey" } }, { "StackAmount", new Variable() { value = 10 } }, { "Stacks", new Variable() { value = 0 } } }, (item, stats, _) =>
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
        AddItemWithVariables("ethereal_ring", new Dictionary<string, Variable>() { { "Duration", new Variable() { value = 3 } } }, null,
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

    public Consumable AddConsumable(string id, ItemFunction onBuy = null)
    {
        TextAsset asset = Resources.Load<TextAsset>("Items/Stats/" + id);
        ItemJSONDTO itemDTO = JsonUtility.FromJson<ItemJSONDTO>(asset.text);
        Consumable item = new Consumable(itemDTO.name);
        item.type = itemDTO.type;
        item.description = itemDTO.description;
        item.stats = new StatBlock(
            itemDTO.stats.damage,
            itemDTO.stats.specialDamage,
            itemDTO.stats.attackSpeed,
            itemDTO.stats.speed,
            itemDTO.stats.health,
            itemDTO.stats.damageReduction,
            itemDTO.stats.resource
        );
        item.icon = Resources.Load<Sprite>("Items/Icons/" + id);
        item.cost = itemDTO.cost;
        item.onBuy = onBuy;
        items.Add(item.ID, item);
        return item;
    }

    public Item AddItem(string id, ItemFunction onEquip = null, ItemFunction onUse = null, ItemFunction onUpdate = null)
    {
        TextAsset asset = Resources.Load<TextAsset>("Items/Stats/" + id);
        ItemJSONDTO itemDTO = JsonUtility.FromJson<ItemJSONDTO>(asset.text);
        Item item = new Item(itemDTO.name);
        item.type = itemDTO.type;
        item.onUse = onUse;
        item.onUpdate = onUpdate;
        item.onEquip = onEquip;
        item.description = itemDTO.description;
        item.cooldown = itemDTO.cooldown;
        item.stats = new StatBlock(
            itemDTO.stats.damage,
            itemDTO.stats.specialDamage,
            itemDTO.stats.attackSpeed,
            itemDTO.stats.speed,
            itemDTO.stats.health,
            itemDTO.stats.damageReduction,
            itemDTO.stats.resource
        );
        item.icon = Resources.Load<Sprite>("Items/Icons/" + id);
        item.cost = itemDTO.cost;
        items.Add(item.ID, item);
        return item;
    }

    public Item AddItemWithVariables(string id, Dictionary<string, Variable> variables = null, ItemFunction onEquip = null, ItemFunction onUse = null, ItemFunction onUpdate = null)
    {
        var item = AddItem(id, onEquip, onUse, onUpdate);
        item.variables = variables;
        return item;
    }

    public override Item[] GetAll()
    {
        var items = this.items.Values.ToList();
        items.Sort((x, y) => x.cost.CompareTo(y.cost));
        return items.Select(x =>
        {
            if (x is Consumable)
                return new Consumable(x as Consumable);
            return new Item(x);
        }).ToArray();
    }

    [System.Serializable]
    private class ItemJSONDTO
    {
        public string name;
        public string id;
        public CharacterType type;
        public StatBlock.StatBlockJSONDTO stats;
        public string description;
        public int cost;
        public float cooldown;

        public ItemJSONDTO(Item item)
        {
            this.name = item.Name;
            this.id = item.ID;
            this.type = item.type;
            this.stats = new StatBlock.StatBlockJSONDTO();
            if (item.stats != null)
            {
                this.stats.damage = item.stats.damage.BaseValue;
                this.stats.specialDamage = item.stats.specialDamage.BaseValue;
                this.stats.attackSpeed = item.stats.attackSpeed.BaseValue;
                this.stats.speed = item.stats.speed.BaseValue;
                this.stats.health = item.stats.health.BaseValue;
                this.stats.damageReduction = item.stats.damageReduction.BaseValue;
                this.stats.resource = item.stats.resource.BaseValue;
            }
            this.description = item.description;
            this.cost = item.cost;
            this.cooldown = item.cooldown;
        }
    }

    public override void ExportToJSON()
    {
        items.Clear();
        string saveFolderPath = Path.Combine("E:\\GitHub Projects\\DungeonKeeper\\Assets\\Resources", "Items\\Stats");
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }
        Create();
        foreach (Item item in items.Values)
        {
            string fileName = $"{item.ID}.json"; // Unique file name
            string filePath = Path.Combine(saveFolderPath, fileName);
            var dto = new ItemJSONDTO(item);
            string json = JsonUtility.ToJson(dto, true); // Pretty JSON
            File.WriteAllText(filePath, json);
        }
        Debug.Log("All items saved individually to: " + saveFolderPath);
    }
}
