using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Card
{
    public string Name { get; private set; }
    public string ID { get; private set; }
    public string Description { get {
            var vars = new Dictionary<string, DescriptionCreator.Variable>();
            foreach (var item in variables)
            {
                vars.Add(item.Key.ToLower(), item.Value);
            }
            return DescriptionCreator.Generate(description, vars); 
        } }

    private string description;

    public delegate void CardFunction(Card card, CharacterStats stats);
    public Sprite icon;
    public UIIconBar activeIcon;
    public CardFunction onSelect;
    [HideInInspector] public Dictionary<string, DescriptionCreator.Variable> variables = new Dictionary<string, DescriptionCreator.Variable>();

    public Card(string name, string description)
    {
        Name = name;
        ID = GetIDFromName(name);
        this.description = description;
    }

    public Card(Card card) : this(card.Name,card.Description)
    {
        ID = card.ID;
        this.onSelect = card.onSelect;
        this.variables = card.variables;
        this.icon = card.icon;
    }

    public void Select(CharacterStats stats)
    {
        onSelect?.Invoke(this, stats);
    }

    public static string GetIDFromName(string name)
    {
        return name.ToLower().Replace(" ", "_");
    }
}
