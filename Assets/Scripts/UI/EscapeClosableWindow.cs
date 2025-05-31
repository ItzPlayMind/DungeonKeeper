using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public class EscapeClosableWindow : MonoBehaviour
{
    public void CloseOnEscape()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        InputManager.Instance.AddEscapeClosableWindow(this);
    }

    private void OnDisable()
    {
        InputManager.Instance.RemoveEscapeClosableWindow(this);
    }
}
