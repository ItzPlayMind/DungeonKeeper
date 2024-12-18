using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Registry<T> : MonoBehaviour
{

    public static Registry<T> Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public abstract T[] GetAll();


    public abstract T GetByID(string id);

    public void AddToAction(Item item, Func<PlayerController.ActionDelegate> getAction, Action<PlayerController.ActionDelegate> setAction, PlayerController.ActionDelegate a)
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

    public void AddToAction(Item item, Func<CharacterStats.DeathDelegate> getAction, Action<CharacterStats.DeathDelegate> setAction, CharacterStats.DeathDelegate a)
    {
        setAction(getAction() + a);
        item.onUnequip += (Item item, CharacterStats stats, int slot) =>
        {
            setAction(getAction() - a);
        };
    }

    public void AddToAction<T>(Item item, Func<StatBlock.Stat<T>.OnStatChange> getAction, Action<StatBlock.Stat<T>.OnStatChange> setAction, StatBlock.Stat<T>.OnStatChange a)
    {
        setAction(getAction() + a);
        item.onUnequip += (Item item, CharacterStats stats, int slot) =>
        {
            setAction(getAction() - a);
        };
    }

    public void AddToAction<T>(Effect effect, Func<StatBlock.Stat<T>.OnStatChange> getAction, Action<StatBlock.Stat<T>.OnStatChange> setAction, StatBlock.Stat<T>.OnStatChange a)
    {
        setAction(getAction() + a);
        effect.onEnd += (Effect effect, CharacterStats stats) =>
        {
            setAction(getAction() - a);
        };
    }
}
