using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DescriptionCreator;

public class CardRegistry : Registry<Card>
{
    private Dictionary<string, Card> cards = new Dictionary<string, Card>();

    protected override void Create()
    {
        AddCard("Damage I", "Adds {Value} to Damage", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 10, color = "red" } } }, (Card card, CharacterStats stats) =>
        {
            stats.stats.damage.ChangeValueAdd += (ref int value,int old) => value += (int)card.variables["Value"].value;
        });
        AddCard("Special Damage I", "Adds {Value} to Special Damage", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 10, color = "blue" } } }, (Card card, CharacterStats stats) =>
        {
            stats.stats.specialDamage.ChangeValueAdd += (ref int value, int old) => value += (int)card.variables["Value"].value;
        });
        AddCard("Health I", "Adds {Value} to Health", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 100, color="green" } } }, (Card card, CharacterStats stats) =>
        {
            stats.stats.health.ChangeValueAdd += (ref int value, int old) => value += (int)card.variables["Value"].value;
        });
        AddCard("Damage Reduction I", "Adds {Value}% to Damage Reduction", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 5, color = "grey" } } }, (Card card, CharacterStats stats) =>
        {
            stats.stats.damageReduction.ChangeValueAdd += (ref int value, int old) => value += (int)card.variables["Value"].value;
        });
        AddCard("Speed I", "Adds {Value} to Speed", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 10, color = "yellow" } } }, (Card card, CharacterStats stats) =>
        {
            stats.stats.speed.ChangeValueAdd += (ref int value, int old) => value += (int)card.variables["Value"].value;
        });
        AddCard("Attack Speed I", "Adds {Value}% to Attack Speed", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 5, color = "orange" } } }, (Card card, CharacterStats stats) =>
        {
            stats.stats.attackSpeed.ChangeValueAdd += (ref int value, int old) => value += (int)card.variables["Value"].value;
        });
    }
    public Card AddCard(string name, string description, Card.CardFunction onSelect = null)
    {
        Card card = new Card(name,description);
        card.onSelect = onSelect;
        //card.icon = Resources.Load<Sprite>("Effects/" + iconName);
        cards.Add(card.ID, card);
        return card;
    }

    public Card AddCard(string name, string description, Dictionary<string, DescriptionCreator.Variable> variables, Card.CardFunction onSelect = null)
    {
        Card card = AddCard(name, description, onSelect);
        card.variables = variables;
        return card;
    }

    public override Card[] GetAll()
    {
        return cards.Values.ToArray();
    }

    public override Card GetByID(string id)
    {
        return cards[id];
    }
}
