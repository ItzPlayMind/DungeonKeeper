using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class EffectRegistry : Registry<Effect>
{
    private Dictionary<string, Effect> effects = new Dictionary<string, Effect>();

    private void Start()
    {
        AddEffect("Slow", "slow", (Effect effect, CharacterStats stats) =>
        {
        AddToAction(effect, () => stats.stats.speed.ChangeValueMult, (value) => stats.stats.speed.ChangeValueMult = value, (ref int speed, int oldSpeed) =>
            {
                speed *= (int)(effect.amount/100f);
            });
        });
        AddEffect("Bleed", "bleed", new Dictionary<string, object>() { { "Timer", 1f} }, null, (Effect effect, CharacterStats stats) =>
        {
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
            AddToAction(effect, () => stats.OnClientHeal, (value) => stats.OnClientHeal = value, (ref int heal) =>
            {
                heal = (int)(heal * (1-(float)effect.amount/100f));
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
