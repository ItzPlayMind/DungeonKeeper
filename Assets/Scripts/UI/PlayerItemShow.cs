using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerItemShow : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI nameText;
    [SerializeField] private UIIconBar[] items;

    public void SetName(string name) => nameText.text = name;

    public void SetItemInSlot(Sprite item, int slot) => items[slot].Icon = item;
}
