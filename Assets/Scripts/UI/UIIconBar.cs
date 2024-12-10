using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIIconBar : UIBar
{
    [SerializeField] private Image icon;

    public Sprite Icon { get => icon.sprite; set => icon.sprite = value; }
}
