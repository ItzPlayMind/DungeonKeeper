using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DescriptionCreator;

public class CardRegistry : Registry<Card>
{
    private Dictionary<string, Card> cards = new Dictionary<string, Card>();

    private void Start()
    {
        AddCard("Damage +", "Adds {Value} to Damage", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 10 } } }, (Card card, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            stats.stats.damage.ChangeValueAdd += (ref int value,int old) => value += (int)card.variables["Value"].value;
        });
        AddCard("Special Damage +", "Adds {Value} to Special Damage", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 10 } } }, (Card card, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            stats.stats.specialDamage.ChangeValueAdd += (ref int value, int old) => value += (int)card.variables["Value"].value;
        });
        AddCard("Health +", "Adds {Value} to Health", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 100 } } }, (Card card, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            stats.stats.health.ChangeValueAdd += (ref int value, int old) => value += (int)card.variables["Value"].value;
        });
        AddCard("Damage Reduction +", "Adds {Value}% to Damage Reduction", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 5 } } }, (Card card, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            stats.stats.damageReduction.ChangeValueAdd += (ref int value, int old) => value += (int)card.variables["Value"].value;
        });
        AddCard("Speed +", "Adds {Value} to Speed", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 10 } } }, (Card card, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            stats.stats.speed.ChangeValueAdd += (ref int value, int old) => value += (int)card.variables["Value"].value;
        });
        AddCard("Attack Speed +", "Adds {Value}% to Attack Speed", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 5 } } }, (Card card, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            stats.stats.attackSpeed.ChangeValueAdd += (ref int value, int old) => value += (int)card.variables["Value"].value;
        });
        AddCard("Heavy Brawler", "Gain {Value}% Health as Attack Damage", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 0.1f } } }, (Card card, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            stats.stats.attackSpeed.ChangeValueAdd += (ref int value, int old) => value += (int)(stats.stats.health.Value* (float)card.variables["Value"].value);
        });
        AddCard("On The Edge", "While below {HPThreshold}% health, heal for {Value}% damage dealt", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 10 } }, { "HPThreshold", new Variable() { value = 10 } } }, (Card card, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            var attack = stats.GetComponent<PlayerAttack>();
            if(attack != null)
            {
                attack.OnAttack += (ulong target, ulong user, ref int amount) =>
                {
                    if (stats.Health <= stats.stats.health.Value * (int)((int)card.variables["HPThreshold"].value/100f))
                    {
                        stats.Heal(amount*(int)((int)card.variables["Value"].value/100f));
                    }
                };
            }
        });
        AddCard("Perfectionist", "While above {HPThreshold}% HP, gain {Value}% Attack Speed", new Dictionary<string, Variable>() { { "Value", new Variable() { value = 10 } }, { "HPThreshold", new Variable() { value = 10 } } }, (Card card, CharacterStats stats) =>
        {
            if (!stats.IsOwner) return;
            stats.stats.attackSpeed.ChangeValueAdd += (ref int value, int old) =>
            {
                if(stats.Health >= stats.stats.health.Value * (int)((int)card.variables["HPThreshold"].value / 100f))
                {
                    value += (int)card.variables["Value"].value;
                }
            };
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
