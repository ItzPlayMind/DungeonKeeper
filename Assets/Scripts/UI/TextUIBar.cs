using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextUIBar : UIBar
{
    [SerializeField] private TMPro.TextMeshProUGUI text;

    public string Text { get => text.text; set => text.text = value; }
}