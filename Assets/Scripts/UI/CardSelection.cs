using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardSelection : MonoBehaviour
{
    [SerializeField] private CardSelector[] cardSelectors;
    private void OnEnable()
    {
        var cards = CardRegistry.Instance.GetAll().ToList();
        for (int i = 0; i < 3; i++)
        {
            var index = Random.Range(0, cards.Count);
            cardSelectors[i].Setup(cards[index]);
            cards.RemoveAt(index);
        }
    }

    public void PickRandom()
    {
        cardSelectors[Random.Range(0, cardSelectors.Length)].Select();
    }
}
