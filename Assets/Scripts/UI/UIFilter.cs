using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIFilter : MonoBehaviour
{
    [SerializeField] private Image graphic;
    public System.Action<bool> onValueChanged;
    public bool Value { get; private set; }
    public void Toggle(bool value)
    {
        Value = value;
        graphic.color = !Value ? Color.white : Color.grey;
        onValueChanged?.Invoke(Value);
    }
}
