using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class ItemRegistry : MonoBehaviour
{
    [System.Serializable]
    private class ItemSetting
    {
        public string name;
        public Sprite icon;
        public StatBlock statBlock;
        public int cost;
    }

    public static ItemRegistry Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    [SerializeField] private List<ItemSetting> itemSettings = new List<ItemSetting>();

    private Dictionary<string, Item> items = new Dictionary<string, Item>();
    private Dictionary<string, ItemSetting> itemSettingDictionary = new Dictionary<string, ItemSetting>();

    private void Start()
    {
        foreach (var itemSetting in itemSettings)
            itemSettingDictionary.Add(itemSetting.name, itemSetting);
        AddItem("Test", null, null);
    }

    public Item GetItemById(string id)
    {
        return items[id];
    }

    public void AddItem(string name, System.Action<PlayerController> onUse, System.Action<PlayerController> onUpdate)
    {
        var settings = itemSettingDictionary[name];
        if (settings == null)
            return;
        Item item = new Item(name);
        item.OnUse = onUse;
        item.OnUpdate = onUpdate;
        item.stats = settings.statBlock;
        item.icon = settings.icon;
        item.cost = settings.cost;
        items.Add(item.ID, item);
    }
}
