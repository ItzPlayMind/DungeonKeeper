using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EffectRegistry : Registry<Effect>
{
    private Dictionary<string, Effect> effects = new Dictionary<string, Effect>();

    protected override void Create()
    {
        AddEffect("Slow", "slow", Effect.EffectType.Debuff, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            AddToAction(effect, () => stats.stats.speed.ChangeValueMult, (value) => stats.stats.speed.ChangeValueMult = value, (ref int speed, int oldSpeed) =>
                {
                    speed = (int)(speed * (1-(effect.amount / 100f)));
                });
        });
        AddEffect("Bleed", "bleed", Effect.EffectType.Debuff, new Dictionary<string, object>() { { "Timer", 1f } }, null, (Effect effect, CharacterStats stats) =>
        {if (!stats.IsOwner) return;
            if ((float)effect.variables["Timer"] <= 0f)
            {
                stats.TakeDamage((int)effect.amount, Vector2.zero, effect.applier);
                effect.variables["Timer"] = 1f;
            }
            else
                effect.variables["Timer"] = (float)effect.variables["Timer"] - Time.deltaTime;
        });
        AddEffect("Rejuvenation", "rejuvenation", Effect.EffectType.Buff, new Dictionary<string, object>() { { "Timer", 1f } }, null, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            if ((float)effect.variables["Timer"] <= 0f)
            {
                stats.Heal((int)(effect.amount/effect.duration),effect.applier);
                effect.variables["Timer"] = 1f;
            }
            else
                effect.variables["Timer"] = (float)effect.variables["Timer"] - Time.deltaTime;
        });
        AddEffect("Curse", "curse", Effect.EffectType.Debuff, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            AddToAction(effect, () => stats.OnClientHeal, (value) => stats.OnClientHeal = value, (ref int heal) =>
            {
                heal = (int)(heal * (1 - (float)effect.amount / 100f));
            });
        });
        AddEffect("Timewarped", "timewarped", Effect.EffectType.Debuff, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            effect.variables["DecayFactor"] = effect.amount / effect.duration;
            AddToAction(effect, () => stats.stats.speed.ChangeValueAdd, (value) => stats.stats.speed.ChangeValueAdd = value, (ref int speed, int old) =>
            {
                speed -= (int)effect.amount;
            });
            AddToAction(effect, () => stats.stats.attackSpeed.ChangeValueAdd, (value) => stats.stats.attackSpeed.ChangeValueAdd = value, (ref int speed, int old) =>
            {
                speed -= (int)effect.amount;
            });
        }, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            effect.amount -= (float)effect.variables["DecayFactor"] * Time.deltaTime;
        });
        AddEffect("Ethereal", "ethereal", Effect.EffectType.Buff, (Effect effect, CharacterStats stats) =>
        {
            //var movement = stats.GetComponent<PlayerMovement>();
            var attack = stats.GetComponent<PlayerAttack>();
            var special = stats.GetComponent<AbstractSpecial>();
            var controller = stats.GetComponent<PlayerController>();
            //var animator = controller.GFX.GetComponent<Animator>();
            effect.onEnd += (Effect effect, CharacterStats stats) =>
            {
                //movement.enabled = true;
                attack.enabled = true;
                stats.enabled = true;
                special.enabled = true;
                controller.GFX.color = Color.white;
                //animator.enabled = true;
            };
            //movement.enabled = false;
            attack.enabled = false;
            stats.enabled = false;
            special.enabled = false;
            controller.GFX.color = new Color(1,1,1,0.2f);
            //animator.enabled = false;
        }, null);
        AddEffect("Invisible", "invisible", Effect.EffectType.Buff, (Effect effect, CharacterStats stats) =>
        {
            var controller = stats.GetComponent<PlayerController>();
            var attack = stats.GetComponent<PlayerAttack>();
            var effectManager = stats.GetComponent<EffectManager>();
            controller.GFX.color = new Color(1f, 1f, 1f, stats.IsLocalPlayer ? 25/255f : 0f);
            AddToAction(effect, () => stats.OnClientTakeDamage, (value) => stats.OnClientTakeDamage = value, (ulong damager, int damage) =>
            {
                effectManager.EndEffect(effect.ID);
            });
            AddToAction(effect, () => attack.OnAttackPress, (value) => attack.OnAttackPress = value, () =>
            {
                effectManager.EndEffect(effect.ID);
            });
            effect.onEnd += (Effect effect, CharacterStats stats) =>
            {
                controller.GFX.color = Color.white;
            };
        }, null);
        AddEffect("Lit", "lit", Effect.EffectType.Debuff, (Effect effect, CharacterStats stats) =>
        {
            var light = stats.GetComponentInChildren<Light2D>();
            if (light.enabled) return;
            float oldLight = light.pointLightOuterRadius;
            light.enabled = true;
            light.pointLightOuterRadius = effect.amount;
            effect.onEnd += (Effect effect, CharacterStats stats) =>
            {
                light.pointLightOuterRadius = oldLight;
                light.enabled = false;
            };
        });
        AddEffect("Flames", "flames", Effect.EffectType.Debuff, new Dictionary<string, object>() { { "Timer", 1f } }, (Effect effect, CharacterStats stats) =>
        {
            
        }, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            if ((float)effect.variables["Timer"] <= 0f)
            {
                stats.TakeDamage((int)effect.amount, Vector2.zero, effect.applier);
                effect.variables["Timer"] = 1f;
            }
            else
                effect.variables["Timer"] = (float)effect.variables["Timer"] - Time.deltaTime;
        });
        AddEffect("Potion", "potion", Effect.EffectType.Buff, new Dictionary<string, object>() { { "Timer", 1f } }, null, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            if ((float)effect.variables["Timer"] <= 0f)
            {
                stats.Heal((int)effect.amount,stats);
                effect.variables["Timer"] = 1f;
            }
            else
                effect.variables["Timer"] = (float)effect.variables["Timer"] - Time.deltaTime;
        });
        AddEffect("Frenzy", "frenzy", Effect.EffectType.Buff, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            AddToAction(effect, () => stats.stats.speed.ChangeValueMult, (value) => stats.stats.speed.ChangeValueMult = value, (ref int speed, int oldSpeed) =>
            {
                speed = (int)(speed * (1 + (effect.amount / 100f)));
            });
            AddToAction(effect, () => stats.stats.attackSpeed.ChangeValueAdd, (value) => stats.stats.speed.ChangeValueAdd = value, (ref int speed, int oldSpeed) =>
            {
                speed += (int)effect.amount;
            }); 
        });
        AddEffect("Windy", "windy", Effect.EffectType.Buff, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            AddToAction(effect, () => stats.stats.speed.ChangeValueMult, (value) => stats.stats.speed.ChangeValueMult = value, (ref int speed, int oldSpeed) =>
            {
                speed = (int)(speed * (1 + (effect.amount / 100f)));
            });
        });
        AddEffect("Stunned", "stunned", Effect.EffectType.Debuff, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            stats.GetComponent<PlayerMovement>().enabled = false;
            stats.GetComponent<PlayerAttack>().enabled = false;
            effect.onEnd += (Effect effect, CharacterStats stats2) =>
            {
                stats.GetComponent<PlayerAttack>().enabled = true;
                stats2.GetComponent<PlayerMovement>().enabled = true;
            };
        });
        AddEffect("Dazzing Strike", "dazzing_strike", Effect.EffectType.Buff, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            var attack = stats.GetComponent<PlayerAttack>();
            var effectManager = stats.GetComponent<EffectManager>();
            AddToAction(effect, () => attack.OnAttack, (value) => attack.OnAttack = value, (ulong target, ulong user, ref int amount) =>
            {
                var targetManager = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target].GetComponent<EffectManager>();
                if(targetManager != null)
                {
                    targetManager.AddEffect("stunned", (int)effect.amount, 1, stats);
                    effectManager.EndEffect(effect.ID);
                }
            });
        });
        AddEffect("Rallied", "rallied", Effect.EffectType.Buff, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            AddToAction(effect, () => stats.stats.attackSpeed.ChangeValueAdd, (value) => stats.stats.speed.ChangeValueAdd = value, (ref int speed, int oldSpeed) =>
            {
                speed += (int)effect.amount;
            });
            AddToAction(effect, () => stats.stats.damage.ChangeValueAdd, (value) => stats.stats.damage.ChangeValueAdd = value, (ref int speed, int oldSpeed) =>
            {
                speed += (int)effect.amount;
            });
            AddToAction(effect, () => stats.stats.specialDamage.ChangeValueAdd, (value) => stats.stats.specialDamage.ChangeValueAdd = value, (ref int speed, int oldSpeed) =>
            {
                speed += (int)effect.amount;
            });
        });
        AddEffect("Blocking", "blocking", Effect.EffectType.Buff, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            effect.variables["DecayFactor"] = effect.amount / effect.duration;
            AddToAction(effect, () => stats.stats.damageReduction.ChangeValueAdd, (value) => stats.stats.damageReduction.ChangeValueAdd = value, (ref int speed, int old) =>
            {
                speed += (int)effect.amount;
            });
        }, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            effect.amount -= (float)effect.variables["DecayFactor"] * Time.deltaTime;
        });
        AddEffect("Slimy", "slimy", Effect.EffectType.Buff, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            AddToAction(effect, () => stats.stats.attackSpeed.ChangeValueAdd, (value) => stats.stats.attackSpeed.ChangeValueAdd = value, (ref int speed, int oldSpeed) =>
            {
                speed += (int)effect.amount;
            });
            AddToAction(effect, () => stats.stats.speed.ChangeValueMult, (value) => stats.stats.speed.ChangeValueMult = value, (ref int speed, int oldSpeed) =>
            {
                speed = (int)(speed * (1+effect.amount/100f));
            });
        });
        AddEffect("Wanted", "wanted", Effect.EffectType.Debuff, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsServer) return;
            AddToAction(effect, () => stats.OnServerTakeDamage, (value) => stats.OnServerTakeDamage = value, (ulong damager, ref int damage) =>
            {
                damage = damage + (int)(damage*(effect.amount/100f));
            });
            AddToAction(effect, () => stats.OnServerDeath, (value) => stats.OnServerDeath = value, (ulong killer) =>
            {
                var player = NetworkManager.Singleton.SpawnManager.SpawnedObjects[killer];
                var targetInventory = player.GetComponent<Inventory>();
                if(targetInventory != null)
                {
                    targetInventory.AddCash((int)(stats.stats.health.Value * (effect.amount/100f)));
                }
            });
        });
        AddEffect("Silenced",  "silenced", Effect.EffectType.Debuff, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            stats.GetComponent<AbstractSpecial>().enabled = false;
            effect.onEnd += (Effect effect, CharacterStats stats2) =>
            {
                stats.GetComponent<AbstractSpecial>().enabled = true;
            };
        });
    }

    public Effect AddEffect(string name, string iconName, Effect.EffectType type, Effect.EffectFunction onStart = null, Effect.EffectFunction onUpdate = null)
    {
        Effect effect = new Effect(name, type);
        effect.onStart = onStart;
        effect.onUpdate = onUpdate;
        effect.icon = Resources.Load<Sprite>("Effects/" + iconName);
        effects.Add(effect.ID, effect);
        return effect;
    }

    public Effect AddEffect(string name, string iconName, Effect.EffectType type, Dictionary<string, object> variables, Effect.EffectFunction onStart = null, Effect.EffectFunction onUpdate = null)
    {
        Effect effect = AddEffect(name, iconName, type, onStart, onUpdate);
        effect.variables = variables;
        return effect;
    }

    public override Effect[] GetAll()
    {
        return effects.Values.ToList().ToArray();
    }

    public override Effect GetByID(string id)
    {
        return new Effect(effects[id]);
    }

    public Effect CreateEffect(string id, float duration, float amount)
    {
        var effect = GetByID(id);
        if (effect == null) return null;
        effect.amount = amount;
        effect.duration = duration;
        return effect;
    }
}
