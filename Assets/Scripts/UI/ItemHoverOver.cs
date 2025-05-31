using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemHoverOver : HoverOver<Item>
{
    [SerializeField] private Transform buttons;
    [SerializeField] private UITextIcon buttonPrefab;
    [SerializeField] private TMPro.TextMeshProUGUI nameText;
    [SerializeField] private TMPro.TextMeshProUGUI cashText;
    [SerializeField] private RectTransform statBlock;
    [SerializeField] private TMPro.TextMeshProUGUI damageText;
    [SerializeField] private GameObject damageIcon;
    [SerializeField] private TMPro.TextMeshProUGUI specialDamageText;
    [SerializeField] private GameObject specialDamageIcon;
    [SerializeField] private TMPro.TextMeshProUGUI healthText;
    [SerializeField] private GameObject healthIcon;
    [SerializeField] private TMPro.TextMeshProUGUI damageReductionText;
    [SerializeField] private GameObject damageReductionIcon;
    [SerializeField] private TMPro.TextMeshProUGUI movementSpeedText;
    [SerializeField] private GameObject movementSpeedIcon;
    [SerializeField] private TMPro.TextMeshProUGUI attackSpeedText;
    [SerializeField] private GameObject attackSpeedIcon;
    [SerializeField] private TMPro.TextMeshProUGUI cooldownText;
    [SerializeField] private TMPro.TextMeshProUGUI descriptionText;
    [SerializeField] private RectTransform description;

    public class InputButton
    {
        public InputAction action;
        public string text;

        public InputButton(InputAction action, string text)
        {
            this.action = action;
            this.text = text;
        }
    }

    public static void Show(Item item, params InputButton[] buttons)
    {
        HoverOver<Item>.Show(item);
        foreach (var button in buttons)
            (Instance as ItemHoverOver).AddButton(button);
    }

    public void AddButton(InputButton button)
    {
        var b = Instantiate(buttonPrefab, buttons);
        b.Text = button.text;
        b.Icon = InputManager.GetIconForKey(button.action);
    }

    public void RemoveAllButtons()
    {
        for (int i = 0; i < buttons.childCount; i++)
        {
            Destroy(buttons.GetChild(i).gameObject);
        }
    }

    protected override void _Show(Item item)
    {
        RemoveAllButtons();
        nameText.text = item.Name;
        cashText.text = item.cost.ToString();
        if (item.stats != null) {
            damageText.text = item.stats.damage.Value.ToString();
            damageIcon.SetActive(item.stats.damage.Value > 0);
            specialDamageText.text =  item.stats.specialDamage.Value.ToString();
            specialDamageIcon.SetActive(item.stats.specialDamage.Value > 0);
            healthText.text = item.stats.health.Value.ToString();
            healthIcon.SetActive(item.stats.health.Value > 0);
            damageReductionText.text =  item.stats.damageReduction.Value.ToString();
            damageReductionIcon.SetActive(item.stats.damageReduction.Value > 0);
            movementSpeedText.text =  item.stats.speed.Value.ToString();
            movementSpeedIcon.SetActive(item.stats.speed.Value > 0);
            attackSpeedText.text = item.stats.attackSpeed.Value.ToString();
            attackSpeedIcon.SetActive(item.stats.attackSpeed.Value > 0);
        }
        else
        {
            damageIcon.SetActive(false);
            specialDamageIcon.SetActive(false);
            healthIcon.SetActive(false);
            damageReductionIcon.SetActive(false);
            movementSpeedIcon.SetActive(false);
            attackSpeedIcon.SetActive(false);
        }
        int activeChildren = 0;
        for (int i = 0; i < statBlock.childCount; i++)
        {
            if (statBlock.GetChild(i).gameObject.activeSelf)
                activeChildren++;
        }
        var height = Mathf.Ceil(activeChildren / 2f) * 35;
        statBlock.sizeDelta = new Vector2(statBlock.sizeDelta.x,height);
        descriptionText.text = item.Description; 
        cooldownText.text = item.cooldown > 0 ? item.cooldown + " sec" : "";
        description.offsetMax = new Vector2(description.offsetMax.x, -(40+40+15+height));
        if (!string.IsNullOrEmpty(item.Description)) {
            height += 200;
        }
        description.gameObject.SetActive(!string.IsNullOrEmpty(item.Description));
        var rectTransform = (transform as RectTransform);
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, (20 + 20 + 15 + height+30));
    }
}
