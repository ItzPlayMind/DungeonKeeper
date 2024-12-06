using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBar : MonoBehaviour
{
    [SerializeField] Image fill;

    float newValue;

    public void UpdateBar(float value)
    {
        newValue = value;
    }

    private void Update()
    {
        if(newValue != fill.fillAmount)
        {
            fill.fillAmount = Mathf.Lerp(newValue,fill.fillAmount, 0.1f);
        }
    }
}
