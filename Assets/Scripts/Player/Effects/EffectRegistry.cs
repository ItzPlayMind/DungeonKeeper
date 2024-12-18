using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EffectRegistry : Registry<Effect>
{
    private Dictionary<string, Effect> effects = new Dictionary<string, Effect>();

    private void Start()
    {
        AddEffect("Slow", (Effect effect, CharacterStats stats) =>
        {
            AddToAction(effect, () => stats.stats.speed.ChangeValue, (value) => stats.stats.speed.ChangeValue = value, (ref int speed, int oldSpeed) =>
            {
                speed -= (int)effect.amount;
            });
        });
        AddEffect("Bleed", new Dictionary<string, object>() { { "Timer", 1f} }, null, (Effect effect, CharacterStats stats) =>
        {
            if ((float)effect.variables["Timer"] <= 0f)
            {
                stats.TakeDamage((int)effect.amount, Vector2.zero, effect.applier);
                effect.variables["Timer"] = 1f;
            }
            else
                effect.variables["Timer"] = (float)effect.variables["Timer"] - Time.deltaTime;
        });
    }

    public void AddEffect(string name, Effect.EffectFunction onStart = null, Effect.EffectFunction onUpdate = null)
    {
        Effect effect = new Effect(name);
        effect.onStart = onStart;
        effect.onUpdate = onUpdate;
        effects.Add(effect.ID, effect);
    }

    public void AddEffect(string name, Dictionary<string, object> variables, Effect.EffectFunction onStart = null, Effect.EffectFunction onUpdate = null)
    {
        Effect effect = new Effect(name);
        effect.onStart = onStart;
        effect.onUpdate = onUpdate;
        effect.variables = variables;
        effects.Add(effect.ID, effect);
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
