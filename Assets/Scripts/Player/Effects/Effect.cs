using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect
{
    public string Name { get; private set; }
    public float duration;
    public float amount;
    public EffectType Type { get; private set; }
    public string ID { get; private set; }

    public delegate void EffectFunction(Effect effect, CharacterStats stats);
    public Sprite icon;
    public UIIconBar activeIcon;
    public EffectFunction onStart;
    public EffectFunction onEnd;
    public EffectFunction onUpdate;
    public CharacterStats applier;
    [HideInInspector] public Dictionary<string, object> variables = new Dictionary<string, object>();

    private float remainingTime;

    public enum EffectType
    {
        Buff, Debuff
    }

    public Effect(string name, EffectType type)
    {
        Name = name;
        ID = GetIDFromName(name);
        Type = type;
    }

    public Effect(Effect effect) : this(effect.Name, effect.Type)
    {
        ID = effect.ID;
        this.onStart = effect.onStart;
        this.onEnd = effect.onEnd;
        this.onUpdate = effect.onUpdate;
        this.variables = effect.variables;
        this.applier = effect.applier;
        this.icon = effect.icon;
    }

    public void Start(CharacterStats stats)
    {
        remainingTime = duration;
        onStart?.Invoke(this, stats);
    }

    public void End(CharacterStats stats)
    {
        if(activeIcon != null)
            GameObject.Destroy(activeIcon.gameObject);
        onEnd?.Invoke(this, stats);
    }

    public void Update(CharacterStats stats)
    {
        onUpdate?.Invoke(this, stats);
        if (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0)
            {
                End(stats);
            }
        }
        if (activeIcon != null)
            activeIcon.UpdateBar(1-(remainingTime / duration));
    }

    public static string GetIDFromName(string name)
    {
        return name.ToLower().Replace(" ", "_");
    }
}
