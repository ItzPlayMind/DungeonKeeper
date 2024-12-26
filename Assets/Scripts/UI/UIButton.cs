using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButton : Button
{
    public UnityEngine.Events.UnityEvent onRightClick = new UnityEngine.Events.UnityEvent();
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Invoke the left click event
            base.OnPointerClick(eventData);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Invoke the right click event
            onRightClick.Invoke();
        }
    }
}
