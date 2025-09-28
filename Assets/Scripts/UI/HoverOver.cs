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

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        gfx = transform.GetChild(0)?.gameObject;
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Hide();
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
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
        if (gfx.activeSelf)
        {
            gfx.transform.position = InputManager.Instance.MousePosition;
            if (gfx.transform.position.y > Screen.height / 2)
                (gfx.transform as RectTransform).pivot = new Vector2(-0.02f, 1.03f);
            else
                (gfx.transform as RectTransform).pivot = new Vector2(-0.02f, 0.03f);
        }
    }
}
