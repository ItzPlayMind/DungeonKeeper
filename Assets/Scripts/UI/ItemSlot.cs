using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IDropHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private ShopPanel shopPanel;
    [SerializeField] private Image iconDrag;
    [SerializeField] private int slot;
    [SerializeField] private bool team;
    private static int selectedSlot;
    private static bool selectedIsTeam;
    private Inventory inventory;

    private void Start()
    {
        inventory = GetComponentInParent<Inventory>();
    }

    public void SellItem()
    {
        shopPanel.SellItem(slot);
    }

    public void AddItemToPlayer()
    {
        shopPanel.AddItemFromTeamToPlayer(slot);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        var item = inventory.GetItem(slot,team);
        if (item == null) return;
        selectedSlot = slot;
        selectedIsTeam = team;
        iconDrag.sprite = item.icon;
        iconDrag.gameObject.SetActive(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        iconDrag.transform.position = eventData.position;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (selectedIsTeam != team) return;
        if (!team)
            inventory.SwapItems(selectedSlot, slot, team);
        else
            inventory.SwapItemsForTeamFromPlayer(selectedSlot, slot);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        iconDrag.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        InputManager.Instance.SetIsOverUI(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputManager.Instance.SetIsOverUI(false);

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var item = inventory.GetItem(slot,team);
        if(item == null) return;
        ItemHoverOver.Show(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemHoverOver.Hide();
    }

}
