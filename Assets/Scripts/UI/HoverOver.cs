using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public abstract class HoverOver<T> : MonoBehaviour
{
    private static HoverOver<T> Instance;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public static void Show(T item)
    {
        Instance.gameObject.SetActive(true);
        Instance._Show(item);
    }

    public static void Hide()
    {
        Instance.gameObject.SetActive(false);
    }

    protected abstract void _Show(T item);


    private void Update()
    {
        if (gameObject.activeSelf)
        {
            transform.position = InputManager.Instance.MousePosition;
            if (transform.position.y > Screen.height / 2)
                (transform as RectTransform).pivot = new Vector2(-0.02f, 1.03f);
            else
                (transform as RectTransform).pivot = new Vector2(-0.02f, 0.03f);
        }
    }
}
