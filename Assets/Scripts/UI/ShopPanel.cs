using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UIButton iconButtonPrefab;
    [SerializeField] private Transform[] shopTransforms;
    [SerializeField] private UIFilter[] filters;
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

    private string search = "";

    public void OnSearchChange(string search)
    {
        this.search = search;
        UpdateFiltersAndSearch();
    }

    public void UpdateFiltersAndSearch()
    {
        bool isFilterActive = filters.Any(x => x.Value);
        foreach (var key in itemButtons.Keys)
        {
            bool hasStat = true;
            if (isFilterActive)
            {
                var item = ItemRegistry.Instance.GetByID(key);
                if (item.stats != null)
                {
                    if (filters[0].Value && item.stats.damage.BaseValue <= 0)
                        hasStat = false;
                    if (filters[1].Value && item.stats.specialDamage.BaseValue <= 0)
                        hasStat = false;
                    if (filters[2].Value && item.stats.speed.BaseValue <= 0)
                        hasStat = false;
                    if (filters[3].Value && item.stats.attackSpeed.BaseValue <= 0)
                        hasStat = false;
                    if (filters[4].Value && item.stats.health.BaseValue <= 0)
                        hasStat = false;
                    if (filters[5].Value && item.stats.damageReduction.BaseValue <= 0)
                        hasStat = false;
                }
                else
                    hasStat = false;
            }
            itemButtons[key].gameObject.SetActive(key.Contains(search) && hasStat);
        }
    }

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
        foreach (var item in filters)
        {
            item.onValueChanged += (value) =>
            {
                UpdateFiltersAndSearch();
            };
        }
        inventory = GetComponentInParent<Inventory>();
        inventory.OnCashChange((value) =>
        {
            cashText.text = value+"";
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
        if (!canShop && !panel.activeSelf) return;
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
