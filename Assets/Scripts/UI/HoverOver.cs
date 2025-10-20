using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class HoverOver<T> : MonoBehaviour
{
    protected static HoverOver<T> Instance;
    private GameObject gfx;

    private Canvas canvas;
    private RectTransform rectTransform;
    private RectTransform canvasRectTransform;
    private Camera canvasCamera;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        gfx = transform.GetChild(0)?.gameObject;
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Hide();
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;

        canvas = GetComponent<Canvas>();
        canvasRectTransform = GetComponent<RectTransform>();
        rectTransform = gfx.GetComponent<RectTransform>();
        canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        Instance.gfx?.SetActive(false);
    }

    public static void Show(T item)
    {
        Instance.gfx.SetActive(true);
        Instance.gfx.transform.position = InputManager.Instance.MousePosition;
        Instance._Show(item);
    }

    public static void Hide()
    {
        Instance.gfx.SetActive(false);
    }

    protected abstract void _Show(T item);


    private void Update()
    {
        if (gfx == null || !gfx.activeSelf) return;
        Vector2 mousePos = InputManager.Instance.MousePosition;

        // Convert to local point within canvas
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            mousePos,
            canvasCamera,
            out localPoint
        );
        localPoint += canvasRectTransform.rect.size/2;
        localPoint.y = canvasRectTransform.rect.size.y - localPoint.y;
        localPoint.y = -localPoint.y;
        Vector2 uiSize = rectTransform.rect.size;
        localPoint.y = Mathf.Min(localPoint.y, -uiSize.y);

        rectTransform.anchoredPosition = localPoint;
    }
}
