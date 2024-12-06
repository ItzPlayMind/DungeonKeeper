using System.Collections;
using System.Collections.Generic;
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
}
