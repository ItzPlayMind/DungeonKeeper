using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITextIcon : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI text;
    [SerializeField] private Image icon;

    public Sprite Icon { get => icon.sprite; set => icon.sprite = value; }
    public string Text { get => text.text; set => text.text = value; }
}
