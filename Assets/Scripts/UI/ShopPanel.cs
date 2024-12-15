using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button iconButtonPrefab;
    [SerializeField] private Transform[] shopTransforms;
    [SerializeField] private TMPro.TextMeshProUGUI cashText;
    [SerializeField] private GameObject panel;

    private Inventory inventory;

    public bool IsActive { get => panel.activeSelf; }

    private Dictionary<string, Button> itemButtons = new Dictionary<string, Button>();

    private void Start()
    {
        panel = transform.GetChild(0).gameObject;
        inventory = GetComponentInParent<Inventory>();
        inventory.OnCashChange((value) =>
        {
            cashText.text = "Cash: " + value;
        });
        var allItems = ItemRegistry.Instance.GetItems();
        foreach (var item in allItems)
        {
            var iconButton = Instantiate(iconButtonPrefab, shopTransforms[(int)item.type]);
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
                    if(!item.multiple)
                        iconButton.interactable = false;
                    foreach (var item1 in item.sameItems)
                        itemButtons[item1].interactable = false;
                }
            });
            itemButtons.Add(item.ID, iconButton);
            iconButton.onClick = buttonListener;
        }
    }

    public void SellItem(int slot)
    {
        var item = inventory.GetItem(slot);
        if (item == null) return;
        inventory.AddCash((int)(item.cost * 0.8f));
        inventory.RemoveItem(slot);
        itemButtons[item.ID].interactable = true;
        foreach (var item1 in item.sameItems)
            itemButtons[item1].interactable = true;
    }

    public void Toggle()
    {
        panel.SetActive(!panel.activeSelf);
        if (!panel.activeSelf)
            ItemHoverOver.Hide();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        InputManager.Instance.SetIsOverUI(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InputManager.Instance.SetIsOverUI(false);
    }
}
