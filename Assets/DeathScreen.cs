using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DeathScreen : MonoBehaviour
{
    [SerializeField] private Volume deathScreenEffect;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private TMPro.TextMeshProUGUI deathTimer;

    public void Show()
    {
        deathTimer.text = "Progress: 0%";
        deathScreen.SetActive(true);
    }

    public void UpdateText(string text)
    {
        deathTimer.text = text;
    }

    public void Hide()
    {
        deathScreen.SetActive(false);
    }

    private void Update()
    {
        if (deathScreen.activeSelf && deathScreenEffect.weight < 1)
            deathScreenEffect.weight += Time.deltaTime;
        if (!deathScreen.activeSelf && deathScreenEffect.weight > 0)
            deathScreenEffect.weight -= Time.deltaTime;
    }
}
