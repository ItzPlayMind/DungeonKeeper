using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemHoverOver : MonoBehaviour
{
    private static ItemHoverOver Instance;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    [SerializeField] private TMPro.TextMeshProUGUI nameText;
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
    [SerializeField] private TMPro.TextMeshProUGUI descriptionText;
    [SerializeField] private RectTransform description;

    public static void Show(Item item)
    {
        Instance.gameObject.SetActive(true);
        Instance._Show(item);
    }

    public static void Hide()
    {
        Instance.gameObject.SetActive(false);
    }

    private void _Show(Item item)
    {
        nameText.text = item.Name;
        if(item.stats != null) {
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
        }
        else
        {
            damageIcon.SetActive(false);
            specialDamageIcon.SetActive(false);
            healthIcon.SetActive(false);
            damageReductionIcon.SetActive(false);
            movementSpeedIcon.SetActive(false);
        }
        int activeChildren = 0;
        for (int i = 0; i < statBlock.childCount; i++)
        {
            if (statBlock.GetChild(i).gameObject.activeSelf)
                activeChildren++;
        }
        var height = Mathf.Ceil(activeChildren / 2f) * 30;
        statBlock.sizeDelta = new Vector2(statBlock.sizeDelta.x,height);
        descriptionText.text = item.Description;
        description.offsetMax = new Vector2(description.offsetMax.x, -(20+15+height));
        if (!string.IsNullOrEmpty(item.Description)) {
            height += 200;
        }
        description.gameObject.SetActive(!string.IsNullOrEmpty(item.Description));
        var rectTransform = (transform as RectTransform);
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, (20 + 15 + height+30));
    }

    private void Update()
    {
        if(gameObject.activeSelf)
        {
            transform.position = InputManager.Instance.MousePosition;
        }
    }
}