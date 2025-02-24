using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeHoverOver : HoverOver<AbstractSpecial.Upgrade>
{

    [SerializeField] private TMPro.TextMeshProUGUI nameText;
    [SerializeField] private TMPro.TextMeshProUGUI unlockedText;
    [SerializeField] private TMPro.TextMeshProUGUI descriptionText;

    protected override void _Show(AbstractSpecial.Upgrade upgrade)
    {
        nameText.text = upgrade.Name;
        unlockedText.text = upgrade.Unlocked ? "<color=green>Unlocked</color>" : "<color=red>Locked</color>";
        descriptionText.text = upgrade.Description;
    }

}
