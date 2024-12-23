using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect
{
    public string Name { get; private set; }
    public float duration;
    public float amount;
    public string ID { get; private set; }

    public delegate void EffectFunction(Effect effect, CharacterStats stats);
    public Sprite icon;
    public UIIconBar activeIcon;
    public EffectFunction onStart;
    public EffectFunction onEnd;
    public EffectFunction onUpdate;
    public CharacterStats applier;
    [HideInInspector] public Dictionary<string, object> variables = new Dictionary<string, object>();

    private float currentTime;

    public Effect(string name)
    {
        Name = name;
        ID = GetIDFromName(name);
    }

    public Effect(Effect effect) : this(effect.Name)
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
        currentTime = duration;
        onStart?.Invoke(this, stats);
    }

    public void End(CharacterStats stats)
    {
        GameObject.Destroy(activeIcon.gameObject);
        onEnd?.Invoke(this, stats);
    }

    public void Update(CharacterStats stats)
    {
        onUpdate?.Invoke(this, stats);
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                End(stats);
            }
        }
        if (activeIcon != null)
            activeIcon.UpdateBar(1-(currentTime / duration));
    }

    public static string GetIDFromName(string name)
    {
        return name.ToLower().Replace(" ", "_");
    }
}
