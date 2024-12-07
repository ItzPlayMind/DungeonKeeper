using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICheckbox : MonoBehaviour
{
    [SerializeField] private Sprite checkedSprite;
    [SerializeField] private Sprite uncheckedSprite;

    private bool currentValue;
    private Image spriteRenderer;
    private void Start()
    {
        spriteRenderer = GetComponent<Image>();
    }

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
