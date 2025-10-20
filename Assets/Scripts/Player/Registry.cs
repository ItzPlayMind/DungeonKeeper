using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public abstract class Registry<T> : MonoBehaviour, IEditorRegistry
{
    public static Registry<T> Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public abstract T[] GetAll();

    public abstract T GetByID(string id);

    public void AddToAction(Item item, Func<PlayerAttack.ActionDelegate> getAction, Action<PlayerAttack.ActionDelegate> setAction, PlayerAttack.ActionDelegate a)
    {
        setAction(getAction() + a);
        item.onUnequip += (Item item, CharacterStats stats, int slot) =>
        {
            setAction(getAction() - a);
        };
    }
    public void AddToAction(Item item, Func<AbstractSpecial.ActionDelegate> getAction, Action<AbstractSpecial.ActionDelegate> setAction, AbstractSpecial.ActionDelegate a)
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

    public void AddToAction(Item item, Func<CharacterStats.ServerDamageDelegate> getAction, Action<CharacterStats.ServerDamageDelegate> setAction, CharacterStats.ServerDamageDelegate a)
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

    public void AddToAction<D>(Item item, Func<StatBlock.Stat<D>.OnStatChange> getAction, Action<StatBlock.Stat<D>.OnStatChange> setAction, StatBlock.Stat<D>.OnStatChange a)
    {
        setAction(getAction() + a);
        item.onUnequip += (Item item, CharacterStats stats, int slot) =>
        {
            setAction(getAction() - a);
        };
    }

    public void AddToAction<D>(Effect effect, Func<StatBlock.Stat<D>.OnStatChange> getAction, Action<StatBlock.Stat<D>.OnStatChange> setAction, StatBlock.Stat<D>.OnStatChange a)
    {
        setAction(getAction() + a);
        effect.onEnd += (Effect effect, CharacterStats stats) =>
        {
            setAction(getAction() - a);
        };
    }
    public void AddToAction(Effect effect, Func<CharacterStats.HealDelegate> getAction, Action<CharacterStats.HealDelegate> setAction, CharacterStats.HealDelegate a)
    {
        setAction(getAction() + a);
        effect.onEnd += (Effect effect, CharacterStats stats) =>
        {
            setAction(getAction() - a);
        };
    }
    public void AddToAction(Effect effect, Func<CharacterStats.DamageDelegate> getAction, Action<CharacterStats.DamageDelegate> setAction, CharacterStats.DamageDelegate a)
    {
        setAction(getAction() + a);
        effect.onEnd += (Effect effect, CharacterStats stats) =>
        {
            setAction(getAction() - a);
        };
    }
    public void AddToAction(Effect effect, Func<CharacterStats.ServerDamageDelegate> getAction, Action<CharacterStats.ServerDamageDelegate> setAction, CharacterStats.ServerDamageDelegate a)
    {
        setAction(getAction() + a);
        effect.onEnd += (Effect effect, CharacterStats stats) =>
        {
            setAction(getAction() - a);
        };
    }
    public void AddToAction(Effect effect, Func<CharacterStats.DeathDelegate> getAction, Action<CharacterStats.DeathDelegate> setAction, CharacterStats.DeathDelegate a)
    {
        setAction(getAction() + a);
        effect.onEnd += (Effect effect, CharacterStats stats) =>
        {
            setAction(getAction() - a);
        };
    }
    public void AddToAction(Effect effect, Func<PlayerAttack.ActionDelegate> getAction, Action<PlayerAttack.ActionDelegate> setAction, PlayerAttack.ActionDelegate a)
    {
        setAction(getAction() + a);
        effect.onEnd += (Effect effect, CharacterStats stats) =>
        {
            setAction(getAction() - a);
        };
    }

    public void AddToAction(Effect effect, Func<System.Action> getAction, Action<System.Action> setAction, System.Action a)
    {
        setAction(getAction() + a);
        effect.onEnd += (Effect effect, CharacterStats stats) =>
        {
            setAction(getAction() - a);
        };
    }

    private void Start()
    {
        Create();
    }
    protected abstract void Create();

    public virtual void ExportToJSON()
    {
        throw new NotImplementedException();
    }
}
