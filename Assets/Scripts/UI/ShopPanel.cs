using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.AI;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UIButton iconButtonPrefab;
    [SerializeField] private RectTransform itemSelectionPanel;
    [SerializeField] private Transform[] shopTransforms;
    [SerializeField] private UIFilter[] filters;
    [SerializeField] private Transform otherPlayerItems;
    [SerializeField] private Button otherPlayerButton;
    [SerializeField] private PlayerItemShow playerItemShowPrefab;
    [SerializeField] private Transform playerItemShowParent;
    private GameObject panel;

    //private Inventory inventory;

    public bool IsActive { get => panel.activeSelf; }

    private Dictionary<string, Button> itemButtons = new Dictionary<string, Button>();

    public static ShopPanel Instance;

    private string search = "";

    public void OnSearchChange(string search)
    {
        this.search = search;
        UpdateFiltersAndSearch();
    }

    public void UpdateFiltersAndSearch()
    {
        bool isFilterActive = filters.Any(x => x.Value);
        List<Item> shownItems = new List<Item>();
        foreach (var key in itemButtons.Keys)
        {
            bool hasStat = true;
            var item = ItemRegistry.Instance.GetByID(key);
            if (isFilterActive)
            {
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
            itemButtons[key].gameObject.SetActive((key.Contains(search) && hasStat) || item.type == CharacterType.None);
            if (key.Contains(search) && hasStat)
                shownItems.Add(item);
        }
        var items = shownItems.ToArray();
        SetSizeForAllCategories(items);
    }

    private void SetSizeForAllCategories(Item[] items)
    {
        var size = itemSelectionPanel.sizeDelta;
        size.y = 200;
        itemSelectionPanel.sizeDelta = size;
        SetAndAddSizeForCategory(CharacterType.Tank, items);
        SetAndAddSizeForCategory(CharacterType.Damage, items);
        SetAndAddSizeForCategory(CharacterType.Support, items);
    }

    public void SetInstanceToThis()
    {
        Instance = this;
    }

    private void SetAndAddSizeForCategory(CharacterType type, Item[] items)
    {
        Vector2 contentSize = itemSelectionPanel.sizeDelta;
        int itemCount = items.Count(x => x.type == type);
        int characterLinesForSection = 0;
        if(itemCount > 0)
            characterLinesForSection = (int)Mathf.Ceil((itemCount * (75 + 15)) / (1202)) + 1;
        var category = (shopTransforms[(int)type].parent as RectTransform);
        if (itemCount == 0)
        {
            category.gameObject.SetActive(false);
        }
        else
        {
            category.gameObject.SetActive(true);
            Vector2 size = category.sizeDelta;
            size.y = characterLinesForSection * (75 + 15) + 50 + 15;
            category.sizeDelta = size;
            contentSize.y += size.y - 50;
        }
        itemSelectionPanel.sizeDelta = contentSize;
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
        //inventory = GetComponentInParent<Inventory>();
        var allItems = ItemRegistry.Instance.GetAll();
        SetSizeForAllCategories(allItems);
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
                var inventory = PlayerController.LocalPlayer.Inventory;
                if (inventory.Cash >= item.cost && (inventory.CanAddItem() || item is Consumable))
                {
                    inventory.RemoveCash(item.cost);
                    if (item is Consumable)
                        inventory.AddConsumable(item as Consumable);
                    else
                        inventory.AddItem(item);
                    if (!item.multiple)
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
        var inventory = PlayerController.LocalPlayer.Inventory;
        var item = inventory.GetItem(slot);
        if (item == null) return;
        inventory.AddCash((int)(item.cost * 0.8f));
        inventory.RemoveItem(slot);
        itemButtons[item.ID].interactable = true;
        foreach (var item1 in item.sameItems)
            itemButtons[item1].interactable = true;
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
