using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICheckbox : MonoBehaviour
{
    [SerializeField] private Sprite checkedSprite;
    [SerializeField] private Sprite uncheckedSprite;

    private bool currentValue;
    [SerializeField] private Image spriteRenderer;
    public void SetChecked(bool value)
    {
        currentValue = value;
        spriteRenderer.sprite = value ? checkedSprite : uncheckedSprite;
    }

    public void Toggle()
    {
        SetChecked(!currentValue);
    }
}
