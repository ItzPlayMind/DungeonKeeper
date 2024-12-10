using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour
{
    [SerializeField] private Button iconButtonPrefab;
    [SerializeField] private Transform shopTransform;
    [SerializeField] private TMPro.TextMeshProUGUI cashText;

    private void Start()
    {
        var inventory = GetComponentInParent<Inventory>();
        inventory.OnCashChange((value) =>
        {
            cashText.text = "Cash: " + value;
        });
        var allItems = ItemRegistry.Instance.GetItems();
        foreach (var item in allItems)
        {
            var iconButton = Instantiate(iconButtonPrefab, shopTransform);
            iconButton.transform.GetChild(0).GetComponent<Image>().sprite = item.icon;
            var hoverEvent = iconButton.GetComponent<HoverEvent>();
            hoverEvent.onPointerEnter += () => ItemHoverOver.Show(item);
            hoverEvent.onPointerExit += () => ItemHoverOver.Hide();

            var buttonListener = new Button.ButtonClickedEvent();
            buttonListener.AddListener(() =>
            {
                if (inventory.Cash >= item.cost && inventory.CanAddItem)
                {
                    inventory.RemoveCash(item.cost);
                    inventory.AddItem(item);
                }
            });
            iconButton.onClick = buttonListener;
        }
    }

    public void Toggle()
    {
        var panel = transform.GetChild(0);
        panel.gameObject.SetActive(!panel.gameObject.activeSelf);
    }
}
