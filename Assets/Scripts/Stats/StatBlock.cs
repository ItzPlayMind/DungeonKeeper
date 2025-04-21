using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using System.Numerics;

[System.Serializable]
public class StatBlock
{
    [System.Serializable]
    public class Stat<T>
    {
        [SerializeField] private T baseValue;
        public T BaseValue { get => baseValue; }
        public delegate void OnStatChange(ref T value, T originalValue);

        private OnStatChange _ChangeValueAdd;
        private OnStatChange _ChangeValueMult;
        public OnStatChange ChangeValueAdd
        {
            get => _ChangeValueAdd; set
            {
                _ChangeValueAdd = value;
                OnChangeValue?.Invoke();
            }
        }
        public OnStatChange ChangeValueMult
        {
            get => _ChangeValueMult; set
            {
                _ChangeValueMult = value;
                OnChangeValue?.Invoke();
            }
        }
        internal OnStatChange ConstraintValue;

        public System.Action OnChangeValue;

        public Stat(T baseValue)
        {
            this.baseValue = baseValue;
        }

        public T Value
        {
            get
            {
                T newValue = baseValue;
                ChangeValueAdd?.Invoke(ref newValue, baseValue);
                ChangeValueMult?.Invoke(ref newValue, baseValue);
                ConstraintValue?.Invoke(ref newValue, baseValue);
                return newValue;
            }
            set => this.baseValue = value;
        }

        public static void Add(Stat<int> stat, int value)
        {
            stat.ChangeValueAdd += (ref int current, int old) => { current += value; };
        }

        public static void Mult(Stat<int> stat, int value)
        {
            stat.ChangeValueMult += (ref int current, int old) => { current *= value; };
        }

        public static void Add(Stat<double> stat, double value)
        {
            stat.ChangeValueAdd += (ref double current, double old) => { current += value; };
        }

        public static void Mult(Stat<double> stat, double value)
        {
            stat.ChangeValueMult += (ref double current, double old) => { current *= value; };
        }

        public static void Add(Stat<float> stat, float value)
        {
            stat.ChangeValueAdd += (ref float current, float old) => { current += value; };
        }

        public static void Mult(Stat<float> stat, float value)
        {
            stat.ChangeValueMult += (ref float current, float old) => { current *= value; };
        }
    }

    public Stat<int> damage = new Stat<int>(80);
    public Stat<int> specialDamage = new Stat<int>(0);
    public Stat<int> attackSpeed = new Stat<int>(0);
    public Stat<int> speed = new Stat<int>(0);
    public Stat<int> health = new Stat<int>(2000);
    public Stat<int> damageReduction = new Stat<int>(0);
    public Stat<int> resource = new Stat<int>(0);
    public System.Action OnValuesChange;

    public StatBlock(int damage, int specialDamage, int attackSpeed, int speed, int health, int damageReduction, int resource = 0)
    {
        this.damage = new Stat<int>(damage);
        this.specialDamage = new Stat<int>(specialDamage);
        this.attackSpeed = new Stat<int>(attackSpeed);
        this.speed = new Stat<int>(speed);
        this.health = new Stat<int>(health);
        this.damageReduction = new Stat<int>(damageReduction);
        this.resource = new Stat<int>(resource);

        this.damageReduction.ConstraintValue += (ref int value, int old) => { value = Mathf.Clamp(value, 0, 100); };
        this.damage.ConstraintValue += (ref int value, int old) => { value = Mathf.Max(value, 0); };
        this.specialDamage.ConstraintValue += (ref int value, int old) => { value = Mathf.Max(value, 0); };
        this.health.ConstraintValue += (ref int value, int old) => { value = Mathf.Max(value, 0); };
        this.speed.ConstraintValue += (ref int value, int old) => { value = Mathf.Max(value, 0); };
        this.attackSpeed.ConstraintValue += (ref int value, int old) => { value = Mathf.Clamp(value, 0, 100); };
        this.resource.ConstraintValue += (ref int value, int old) => { value = Mathf.Max(value, 0); };
    }

    public void Add(StatBlock stats)
    {
        damage.ChangeValueAdd += (ref int value, int _) => value += stats.damage.Value;
        specialDamage.ChangeValueAdd += (ref int value, int _) => value += stats.specialDamage.Value;
        speed.ChangeValueAdd += (ref int value, int _) => value += stats.speed.Value;
        attackSpeed.ChangeValueAdd += (ref int value, int _) => value += stats.attackSpeed.Value;
        health.ChangeValueAdd += (ref int value, int _) => value += stats.health.Value;
        damageReduction.ChangeValueAdd += (ref int value, int _) => value += stats.damageReduction.Value;
        resource.ChangeValueAdd += (ref int value, int _) => value += stats.resource.Value;
        OnValuesChange?.Invoke();
    }


    public void Remove(StatBlock stats)
    {
        damage.ChangeValueAdd += (ref int value, int _) => value -= stats.damage.Value;
        specialDamage.ChangeValueAdd += (ref int value, int _) => value -= stats.specialDamage.Value;
        speed.ChangeValueAdd += (ref int value, int _) => value -= stats.speed.Value; 
        attackSpeed.ChangeValueAdd += (ref int value, int _) => value -= stats.attackSpeed.Value;
        health.ChangeValueAdd += (ref int value, int _) => value -= stats.health.Value;
        damageReduction.ChangeValueAdd += (ref int value, int _) => value -= stats.damageReduction.Value;
        resource.ChangeValueAdd += (ref int value, int _) => value -= stats.resource.Value;
        OnValuesChange?.Invoke();
    }
}
