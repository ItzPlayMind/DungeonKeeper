using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

[System.Serializable]
public class StatBlock
{
    [System.Serializable]
    public class Stat<T>
    {
        [SerializeField] private T baseValue;
        public delegate void OnStatChange(ref T value, T originalValue);
        public OnStatChange ChangeValue;

        public Stat(T baseValue)
        {
            this.baseValue = baseValue;
        }

        public T Value
        {
            get
            {
                T newValue = baseValue;
                ChangeValue?.Invoke(ref newValue, baseValue);
                return newValue;
            }
            set => this.baseValue = value;
        }
    }
    public Stat<int> damage = new Stat<int>(80);
    public Stat<int> specialDamage = new Stat<int>(0);
    public Stat<Vector2> speed = new Stat<Vector2>(new Vector2(120,110));
    public Stat<int> health = new Stat<int>(2000);
    public Stat<float> damageReduction = new Stat<float>(0);

    public void Add(StatBlock stats)
    {
        damage.ChangeValue += (ref int value, int _) => { value += stats.damage.Value; Debug.Log("ADD TO BASE VALUE"); };
        specialDamage.ChangeValue += (ref int value, int _) => value += stats.specialDamage.Value;
        speed.ChangeValue += (ref Vector2 value, Vector2 _) => value += stats.speed.Value;
        health.ChangeValue += (ref int value, int _) => value += stats.health.Value;
        damageReduction.ChangeValue += (ref float value, float _) => value += stats.damageReduction.Value;
    }
}
