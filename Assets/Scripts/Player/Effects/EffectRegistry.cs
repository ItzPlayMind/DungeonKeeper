using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EffectRegistry : Registry<Effect>
{
    private Dictionary<string, Effect> effects = new Dictionary<string, Effect>();

    private void Start()
    {
        AddEffect("Slow", "slow", (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            AddToAction(effect, () => stats.stats.speed.ChangeValueMult, (value) => stats.stats.speed.ChangeValueMult = value, (ref int speed, int oldSpeed) =>
                {
                    speed = (int)(speed * (1-(effect.amount / 100f)));
                });
        });
        AddEffect("Bleed", "bleed", new Dictionary<string, object>() { { "Timer", 1f } }, null, (Effect effect, CharacterStats stats) =>
        {if (!stats.IsOwner) return;
            if ((float)effect.variables["Timer"] <= 0f)
            {
                stats.TakeDamage((int)effect.amount, Vector2.zero, effect.applier);
                effect.variables["Timer"] = 1f;
            }
            else
                effect.variables["Timer"] = (float)effect.variables["Timer"] - Time.deltaTime;
        });
        AddEffect("Curse", "curse", (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            AddToAction(effect, () => stats.OnClientHeal, (value) => stats.OnClientHeal = value, (ref int heal) =>
            {
                heal = (int)(heal * (1 - (float)effect.amount / 100f));
            });
        });
        AddEffect("Timewarped", "timewarped", (Effect effect, CharacterStats stats) =>
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
        AddEffect("Ethereal", "ethereal", (Effect effect, CharacterStats stats) =>
        {
            var movement = stats.GetComponent<PlayerMovement>();
            var attack = stats.GetComponent<PlayerAttack>();
            var controller = stats.GetComponent<PlayerController>();
            var animator = controller.GFX.GetComponent<Animator>();
            effect.onEnd += (Effect effect, CharacterStats stats) =>
            {
                movement.enabled = true;
                attack.enabled = true;
                stats.enabled = true;
                controller.GFX.color = Color.white;
                animator.enabled = true;
            };
            movement.enabled = false;
            attack.enabled = false;
            stats.enabled = false; 
            controller.GFX.color = Color.grey;
            animator.enabled = false;
        }, null);
        AddEffect("Lit", "lit", (Effect effect, CharacterStats stats) =>
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
        AddEffect("Flames", "flames", new Dictionary<string, object>() { { "Timer", 1f } }, (Effect effect, CharacterStats stats) =>
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
        AddEffect("Potion", "potion", new Dictionary<string, object>() { { "Timer", 1f } }, null, (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            if ((float)effect.variables["Timer"] <= 0f)
            {
                stats.Heal((int)effect.amount);
                effect.variables["Timer"] = 1f;
            }
            else
                effect.variables["Timer"] = (float)effect.variables["Timer"] - Time.deltaTime;
        });
        AddEffect("Frenzy", "frenzy", (Effect effect, CharacterStats stats) =>
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
        AddEffect("Windy", "windy", (Effect effect, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            AddToAction(effect, () => stats.stats.speed.ChangeValueMult, (value) => stats.stats.speed.ChangeValueMult = value, (ref int speed, int oldSpeed) =>
            {
                speed = (int)(speed * (1 + (effect.amount / 100f)));
            });
        });
        AddEffect("Stunned", "stunned", (Effect effect, CharacterStats stats) =>
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
        AddEffect("Rallied", "rallied", (Effect effect, CharacterStats stats) =>
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
        AddEffect("Blocking", "blocking", (Effect effect, CharacterStats stats) =>
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
        AddEffect("Slimy", "slimy", (Effect effect, CharacterStats stats) =>
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
    }

    public Effect AddEffect(string name, string iconName, Effect.EffectFunction onStart = null, Effect.EffectFunction onUpdate = null)
    {
        Effect effect = new Effect(name);
        effect.onStart = onStart;
        effect.onUpdate = onUpdate;
        effect.icon = Resources.Load<Sprite>("Effects/" + iconName);
        effects.Add(effect.ID, effect);
        return effect;
    }

    public Effect AddEffect(string name, string iconName, Dictionary<string, object> variables, Effect.EffectFunction onStart = null, Effect.EffectFunction onUpdate = null)
    {
        Effect effect = AddEffect(name, iconName, onStart, onUpdate);
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
