using System;
using System.Collections.Generic;
using UnityEngine;

namespace RageHunters.Inventory
{
    /// <summary>
    /// Designated equipment slot targets.
    /// </summary>
    public enum EquipmentSlotType
    {
        Weapon,
        Armor,
        Utility1,
        Utility2
    }

    /// <summary>
    /// Represents an inventory slot holding an item definition and stacked quantity.
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        [SerializeField] private ItemDataSO item;
        [SerializeField] private int quantity;

        public ItemDataSO Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;

        public InventorySlot()
        {
            item = null;
            quantity = 0;
        }

        public InventorySlot(ItemDataSO item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }

        public void Set(ItemDataSO newItem, int newQuantity)
        {
            item = newItem;
            quantity = newQuantity;
        }

        public void AddQuantity(int amount)
        {
            quantity += amount;
        }

        public void RemoveQuantity(int amount)
        {
            quantity = Mathf.Max(0, quantity - amount);
            if (quantity == 0)
            {
                item = null;
            }
        }

        public void Clear()
        {
            item = null;
            quantity = 0;
        }
    }

    /// <summary>
    /// Manages player inventory capacity, item stacking, equipment slots,
    /// and dispatches state change events.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Configuration")]
        [Min(1)]
        [SerializeField] private int capacity = 20;

        [Header("Starting Items")]
        [SerializeField] private List<ItemDataSO> startingItems = new();

        #endregion

        #region Private Fields

        [SerializeField] private List<InventorySlot> slots = new();
        private readonly Dictionary<EquipmentSlotType, ItemDataSO> _equipment = new();

        #endregion

        #region Events

        public event Action<ItemDataSO, int> OnItemAdded;
        public event Action<ItemDataSO, int> OnItemRemoved;
        public event Action<EquipmentSlotType, ItemDataSO> OnEquipped;
        public event Action<EquipmentSlotType, ItemDataSO> OnUnequipped;
        public event Action OnInventoryChanged;

        #endregion

        #region Properties

        public int Capacity => capacity;
        public IReadOnlyList<InventorySlot> Slots => slots;
        public IReadOnlyDictionary<EquipmentSlotType, ItemDataSO> Equipment => _equipment;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeSlots();
            InitializeEquipment();

            // Grant starting items
            for (int i = 0; i < startingItems.Count; i++)
            {
                if (startingItems[i] != null)
                {
                    AddItem(startingItems[i], 1, out _);
                }
            }
        }

        #endregion

        #region Initialization

        private void InitializeSlots()
        {
            if (slots.Count != capacity)
            {
                slots.Clear();
                for (int i = 0; i < capacity; i++)
                {
                    slots.Add(new InventorySlot());
                }
            }
        }

        private void InitializeEquipment()
        {
            _equipment[EquipmentSlotType.Weapon] = null;
            _equipment[EquipmentSlotType.Armor] = null;
            _equipment[EquipmentSlotType.Utility1] = null;
            _equipment[EquipmentSlotType.Utility2] = null;
        }

        #endregion

        #region Inventory Operations

        /// <summary>
        /// Adds an item and quantity to inventory. Handles stacking up to maxStack.
        /// Outputs any remainder that could not fit due to capacity.
        /// </summary>
        public bool AddItem(ItemDataSO item, int quantity, out int remainder)
        {
            remainder = quantity;
            if (item == null || quantity <= 0) return false;

            int toAdd = quantity;

            // 1. If stackable, search existing slots containing this item
            if (item.Stackable)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i].Item == item && slots[i].Quantity < item.MaxStack)
                    {
                        int spaceInSlot = item.MaxStack - slots[i].Quantity;
                        int addAmount = Mathf.Min(spaceInSlot, toAdd);

                        slots[i].AddQuantity(addAmount);
                        toAdd -= addAmount;

                        if (toAdd <= 0)
                        {
                            break;
                        }
                    }
                }
            }

            // 2. Place remaining amount into empty slots
            if (toAdd > 0)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i].IsEmpty)
                    {
                        int addAmount = Mathf.Min(item.MaxStack, toAdd);
                        slots[i].Set(item, addAmount);
                        toAdd -= addAmount;

                        if (toAdd <= 0)
                        {
                            break;
                        }
                    }
                }
            }

            int addedCount = quantity - toAdd;
            remainder = toAdd;

            if (addedCount > 0)
            {
                OnItemAdded?.Invoke(item, addedCount);
                OnInventoryChanged?.Invoke();
            }

            return remainder == 0;
        }

        /// <summary>
        /// Removes the specified quantity of an item from inventory.
        /// </summary>
        public bool RemoveItem(ItemDataSO item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;
            if (!HasItem(item, quantity)) return false;

            int toRemove = quantity;

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i].Item == item)
                {
                    int available = slots[i].Quantity;
                    int take = Mathf.Min(available, toRemove);

                    slots[i].RemoveQuantity(take);
                    toRemove -= take;

                    if (toRemove <= 0)
                    {
                        break;
                    }
                }
            }

            OnItemRemoved?.Invoke(item, quantity);
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Checks whether the inventory contains at least the required quantity of an item.
        /// </summary>
        public bool HasItem(ItemDataSO item, int requiredQuantity = 1)
        {
            if (item == null || requiredQuantity <= 0) return false;
            return GetItemCount(item) >= requiredQuantity;
        }

        /// <summary>
        /// Counts total quantity of the specified item across all slots.
        /// </summary>
        public int GetItemCount(ItemDataSO item)
        {
            if (item == null) return 0;

            int total = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Item == item)
                {
                    total += slots[i].Quantity;
                }
            }
            return total;
        }

        #endregion

        #region Equipment Operations

        /// <summary>
        /// Equips an item into the specified equipment slot, unequipping any item already present.
        /// </summary>
        public bool EquipItem(EquipmentSlotType slot, ItemDataSO item)
        {
            if (item == null) return false;

            // Validate item type matching
            if (!IsItemCompatibleWithSlot(slot, item))
            {
                Debug.LogWarning($"[InventorySystem] Cannot equip {item.ItemName} ({item.Type}) into slot {slot}.");
                return false;
            }

            // Unequip currently equipped item if any
            if (_equipment.TryGetValue(slot, out var existingItem) && existingItem != null)
            {
                UnequipItem(slot);
            }

            // Remove 1 copy from inventory
            if (!RemoveItem(item, 1))
            {
                return false;
            }

            _equipment[slot] = item;
            OnEquipped?.Invoke(slot, item);
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Unequips the item in the specified slot, returning it to inventory.
        /// </summary>
        public bool UnequipItem(EquipmentSlotType slot)
        {
            if (!_equipment.TryGetValue(slot, out var equippedItem) || equippedItem == null)
            {
                return false;
            }

            // Try to return to inventory
            if (AddItem(equippedItem, 1, out int remainder) && remainder == 0)
            {
                _equipment[slot] = null;
                OnUnequipped?.Invoke(slot, equippedItem);
                OnInventoryChanged?.Invoke();
                return true;
            }

            Debug.LogWarning($"[InventorySystem] Inventory is full, cannot unequip {equippedItem.ItemName}.");
            return false;
        }

        /// <summary>
        /// Gets the item currently equipped in the designated slot.
        /// </summary>
        public ItemDataSO GetEquippedItem(EquipmentSlotType slot)
        {
            return _equipment.TryGetValue(slot, out var item) ? item : null;
        }

        private bool IsItemCompatibleWithSlot(EquipmentSlotType slot, ItemDataSO item)
        {
            return slot switch
            {
                EquipmentSlotType.Weapon => item.Type == ItemType.Weapon,
                EquipmentSlotType.Armor => item.Type == ItemType.Armor,
                EquipmentSlotType.Utility1 or EquipmentSlotType.Utility2 => item.Type is ItemType.Utility or ItemType.Consumable,
                _ => false
            };
        }

        #endregion
    }
}
