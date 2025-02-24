using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UIButton iconButtonPrefab;
    [SerializeField] private Transform[] shopTransforms;
    [SerializeField] private TMPro.TextMeshProUGUI cashText;
    [SerializeField] private Transform otherPlayerItems;
    [SerializeField] private Button otherPlayerButton;
    [SerializeField] private PlayerItemShow playerItemShowPrefab;
    [SerializeField] private Transform playerItemShowParent;
    private GameObject panel;

    private Inventory inventory;

    public bool IsActive { get => panel.activeSelf; }

    private Dictionary<string, Button> itemButtons = new Dictionary<string, Button>();

    public static ShopPanel Instance;

    private bool canShop = false;

    public void SetInstanceToThis()
    {
        Instance = this;
    }

    public void SetAbleToShop(bool value)
    {
        canShop = value;
    }

    private void Start()
    {
        panel = transform.GetChild(0).gameObject;
        inventory = GetComponentInParent<Inventory>();
        inventory.OnCashChange((value) =>
        {
            cashText.text = "Cash: " + value;
        });
        var allItems = ItemRegistry.Instance.GetAll();
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
                if (!canShop) return;
                if (inventory.Cash >= item.cost && inventory.CanAddItem())
                {
                    inventory.RemoveCash(item.cost);
                    inventory.AddItem(item);
                    if(!item.multiple)
                        iconButton.interactable = false;
                    foreach (var item1 in item.sameItems)
                        itemButtons[item1].interactable = false;
                }
            });
            var buttonRightListener = new Button.ButtonClickedEvent();
            buttonRightListener.AddListener(() =>
            {
                if (!canShop) return;
                if (inventory.Cash >= item.cost && inventory.CanAddItem(true))
                {
                    inventory.RemoveCash(item.cost);
                    inventory.AddItemToTeamFromPlayer(item);
                    /*if (!item.multiple)
                        iconButton.interactable = false;
                    foreach (var item1 in item.sameItems)
                        itemButtons[item1].interactable = false;*/
                }
            });
            itemButtons.Add(item.ID, iconButton);
            iconButton.onClick = buttonListener;
            iconButton.onRightClick = buttonRightListener;
        }
    }

    public void SellItem(int slot)
    {
        if (!canShop) return;
        var item = inventory.GetItem(slot);
        if (item == null) return;
        inventory.AddCash((int)(item.cost * 0.8f));
        inventory.RemoveItem(slot);
        itemButtons[item.ID].interactable = true;
        foreach (var item1 in item.sameItems)
            itemButtons[item1].interactable = true;
    }

    public void AddItemFromTeamToPlayer(int slot) 
    {
        if (!inventory.CanAddItem()) return;
        var item = inventory.GetItem(slot,true);
        if (item == null) return;
        inventory.AddItem(item);
        inventory.RemoveItemFromTeamFromPlayer(slot);
    }

    public void ToggleOtherPlayerItems()
    {
        otherPlayerItems.gameObject.SetActive(!otherPlayerItems.gameObject.activeSelf);
        otherPlayerButton.transform.localScale = new Vector3(otherPlayerItems.gameObject.activeSelf ? 1 : -1, 1, 1);
    }

    public void Toggle()
    {
        panel.SetActive(!panel.activeSelf);
        if (!panel.activeSelf)
            ItemHoverOver.Hide();
    }

    public PlayerItemShow CreatePlayerItemsShow(string name)
    {
        var itemShow = Instantiate(playerItemShowPrefab, playerItemShowParent);
        itemShow.SetName(name);
        return itemShow;
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
