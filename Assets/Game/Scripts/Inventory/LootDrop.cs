using UnityEngine;

namespace RageHunters.Inventory
{
    /// <summary>
    /// World loot pickup item with visual bobbing/rotation, interaction prompts,
    /// and auto/manual pickup resolution into an InventorySystem.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LootDrop : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Loot Payload")]
        [SerializeField] private ItemDataSO itemData;
        [Min(1)]
        [SerializeField] private int quantity = 1;

        [Header("Interaction Settings")]
        [SerializeField] private string promptText = "Pick up";
        [SerializeField] private bool autoPickupOnTrigger = false;
        [SerializeField] private bool destroyOnPickup = true;

        [Header("Visual Effects")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool enableBobbing = true;
        [SerializeField] private float bobSpeed = 2.0f;
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private bool enableRotation = true;
        [SerializeField] private float rotationSpeed = 60f;
        [SerializeField] private Light glowLight;
        [SerializeField] private ParticleSystem glowParticles;

        [Header("Audio & Feedback")]
        [SerializeField] private AudioClip pickupAudioClip;

        #endregion

        #region Private Fields

        private Vector3 _initialVisualLocalPosition;
        private Collider _collider;
        private bool _isPickedUp;

        #endregion

        #region Properties

        public ItemDataSO ItemData => itemData;
        public int Quantity => quantity;
        public string PromptText => $"{promptText} {itemData?.ItemName ?? "Item"} x{quantity}";
        public bool IsPickedUp => _isPickedUp;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;

            if (visualRoot == null)
            {
                visualRoot = transform;
                _initialVisualLocalPosition = Vector3.zero;
            }
            else
            {
                _initialVisualLocalPosition = visualRoot.localPosition;
            }

            ConfigureRarityGlow();
        }

        private void Update()
        {
            if (_isPickedUp) return;

            // Visual bob & spin animation
            if (visualRoot != null)
            {
                if (enableBobbing)
                {
                    float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                    visualRoot.localPosition = _initialVisualLocalPosition + new Vector3(0f, yOffset, 0f);
                }

                if (enableRotation)
                {
                    visualRoot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isPickedUp || !autoPickupOnTrigger) return;

            if (other.TryGetComponent<InventorySystem>(out var inventory))
            {
                TryCollect(inventory);
            }
        }

        #endregion

        #region Initialization & Setup

        /// <summary>
        /// Dynamically initializes a dropped item instance in the world.
        /// </summary>
        public void Initialize(ItemDataSO item, int count)
        {
            itemData = item;
            quantity = Mathf.Max(1, count);
            _isPickedUp = false;

            ConfigureRarityGlow();
        }

        private void ConfigureRarityGlow()
        {
            if (itemData == null) return;

            Color rarityColor = itemData.Rarity switch
            {
                ItemRarity.Common => Color.white,
                ItemRarity.Uncommon => Color.green,
                ItemRarity.Rare => new Color(0.2f, 0.6f, 1.0f),
                ItemRarity.Epic => new Color(0.7f, 0.2f, 0.9f),
                ItemRarity.Legendary => new Color(1.0f, 0.65f, 0.0f),
                _ => Color.white
            };

            if (glowLight != null)
            {
                glowLight.color = rarityColor;
            }

            if (glowParticles != null)
            {
                var main = glowParticles.main;
                main.startColor = rarityColor;
            }
        }

        #endregion

        #region Interaction

        /// <summary>
        /// Attempts to collect this loot drop and insert it into the target inventory.
        /// </summary>
        public bool TryCollect(InventorySystem inventory)
        {
            if (_isPickedUp || inventory == null || itemData == null)
            {
                return false;
            }

            if (inventory.AddItem(itemData, quantity, out int remainder))
            {
                // Full quantity collected
                _isPickedUp = true;
                CompleteCollection();
                return true;
            }

            if (remainder < quantity)
            {
                // Partial quantity collected
                quantity = remainder;
                return true;
            }

            return false;
        }

        private void CompleteCollection()
        {
            if (pickupAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(pickupAudioClip, transform.position);
            }

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
