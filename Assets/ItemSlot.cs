using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IDropHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private ShopPanel shopPanel;
    [SerializeField] private Image iconDrag;
    [SerializeField] private int slot;
    private static int selectedSlot;
    private Inventory inventory;

    private void Start()
    {
        inventory = GetComponentInParent<Inventory>();
    }

    public void SellItem()
    {
        shopPanel.SellItem(slot);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        var item = inventory.GetItem(slot);
        if (item == null) return;
        selectedSlot = slot;
        iconDrag.sprite = item.icon;
        iconDrag.gameObject.SetActive(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        iconDrag.transform.position = eventData.position;
    }

    public void OnDrop(PointerEventData eventData)
    {
        inventory.SwapItems(selectedSlot, slot);
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
}
