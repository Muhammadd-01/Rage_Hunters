using UnityEngine;

namespace RageHunters.Inventory
{
    /// <summary>
    /// Item category classification.
    /// </summary>
    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable,
        Utility,
        KeyItem,
        Loot
    }

    /// <summary>
    /// Item rarity tiers for loot distribution and UI styling.
    /// </summary>
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// Base ScriptableObject defining immutable item data for inventory, loot, and equipment systems.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Rage Hunters/Inventory/Item Data")]
    public class ItemDataSO : ScriptableObject
    {
        #region Inspector Fields

        [Header("General Info")]
        [SerializeField] private string itemId;
        [SerializeField] private string itemName = "New Item";
        [TextArea(2, 4)]
        [SerializeField] private string description = string.Empty;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemType itemType = ItemType.Loot;
        [SerializeField] private ItemRarity rarity = ItemRarity.Common;

        [Header("Stacking & Economy")]
        [SerializeField] private bool stackable = true;
        [Min(1)]
        [SerializeField] private int maxStack = 99;
        [Min(0)]
        [SerializeField] private int value = 10;
        [SerializeField] private float weight = 0.5f;

        [Header("World Visuals")]
        [SerializeField] private GameObject worldPrefab;

        #endregion

        #region Properties

        public string ItemId => string.IsNullOrEmpty(itemId) ? name : itemId;
        public string ItemName => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemType Type => itemType;
        public ItemRarity Rarity => rarity;
        public bool Stackable => stackable;
        public int MaxStack => stackable ? Mathf.Max(1, maxStack) : 1;
        public int Value => value;
        public float Weight => weight;
        public GameObject WorldPrefab => worldPrefab;

        #endregion

        #region Validation

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = name;
            }

            if (!stackable)
            {
                maxStack = 1;
            }
        }

        #endregion
    }
}
