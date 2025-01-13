using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityHoverOver : HoverOver<AbstractSpecial>
{
    [SerializeField] private TMPro.TextMeshProUGUI nameText;
    [SerializeField] private TMPro.TextMeshProUGUI cooldownText;
    [SerializeField] private TMPro.TextMeshProUGUI descriptionText;

    protected override void _Show(AbstractSpecial ability)
    {
        nameText.text = ability.Name;
        cooldownText.text = ability.Cooldown + " sec";
        descriptionText.text = ability.Description(InputManager.Instance.PlayerDetailsHold);
    }
}
