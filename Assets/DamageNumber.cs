using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI text;

    public void Destroy()
    {
        Destroy(gameObject);
    }

    public void setNumber(int number, Color color)
    {
        text.color = color;
        text.text = number.ToString();
    }
}
