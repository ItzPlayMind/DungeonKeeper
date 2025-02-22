using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardSelector : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;

    private Card card;

    public void Setup(Card card)
    {
        this.card = card;
        title.text = card.Name;
        description.text = card.Description;
    }

    public void Select()
    {
        (ArenaGameManager.instance as ArenaGameManager).AddCardToPlayer(card);
        transform.parent.gameObject.SetActive(false);
    }
}
