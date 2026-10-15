using UnityEngine;
using RageHunters.Inventory;

namespace RageHunters.Combat
{
    /// <summary>
    /// Type of combat attack delivery.
    /// </summary>
    public enum WeaponAttackType
    {
        Melee,
        Ranged
    }

    /// <summary>
    /// ScriptableObject defining weapon-specific attributes, extending item definitions.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Rage Hunters/Combat/Weapon Data")]
    public class WeaponDataSO : ItemDataSO
    {
        #region Combat Configuration

        [Header("Weapon Stats")]
        [SerializeField] private WeaponAttackType attackType = WeaponAttackType.Melee;
        [Min(0f)]
        [SerializeField] private float baseDamage = 25f;
        [Tooltip("Attacks per second")]
        [Min(0.1f)]
        [SerializeField] private float attackSpeed = 1.2f;
        [Min(0.5f)]
        [SerializeField] private float range = 2.0f;
        [Range(0f, 1f)]
        [SerializeField] private float criticalChance = 0.1f;
        [Min(1f)]
        [SerializeField] private float criticalMultiplier = 1.5f;

        [Header("Ranged Settings")]
        [SerializeField] private bool usesAmmunition = false;
        [Min(1)]
        [SerializeField] private int magazineCapacity = 30;
        [SerializeField] private ItemDataSO requiredAmmoItem;
        [Min(0.1f)]
        [SerializeField] private float reloadTime = 2.0f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 50f;

        [Header("Melee Settings")]
        [Tooltip("Radius of the melee sphere cast or sweep")]
        [Min(0.1f)]
        [SerializeField] private float hitboxRadius = 0.5f;
        [Tooltip("Angle cone in degrees for melee detection")]
        [Range(10f, 180f)]
        [SerializeField] private float meleeHitAngle = 90f;

        [Header("Animation & Visuals")]
        [SerializeField] private string attackAnimationTrigger = "Attack";
        [SerializeField] private string reloadAnimationTrigger = "Reload";
        [SerializeField] private int attackComboSteps = 1;
        [SerializeField] private GameObject hitImpactEffect;

        #endregion

        #region Properties

        public WeaponAttackType AttackType => attackType;
        public float BaseDamage => baseDamage;
        public float AttackSpeed => attackSpeed;
        public float AttackCooldown => attackSpeed > 0f ? 1f / attackSpeed : 1f;
        public float Range => range;
        public float CriticalChance => criticalChance;
        public float CriticalMultiplier => criticalMultiplier;

        public bool UsesAmmunition => usesAmmunition;
        public int MagazineCapacity => magazineCapacity;
        public ItemDataSO RequiredAmmoItem => requiredAmmoItem;
        public float ReloadTime => reloadTime;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;

        public float HitboxRadius => hitboxRadius;
        public float MeleeHitAngle => meleeHitAngle;

        public string AttackAnimationTrigger => attackAnimationTrigger;
        public string ReloadAnimationTrigger => reloadAnimationTrigger;
        public int AttackComboSteps => attackComboSteps;
        public GameObject HitImpactEffect => hitImpactEffect;

        #endregion

        #region Validation

        private void Reset()
        {
            // By default, weapon items are weapons and not stackable
            typeof(ItemDataSO).GetField("itemType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(this, ItemType.Weapon);
            typeof(ItemDataSO).GetField("stackable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(this, false);
            typeof(ItemDataSO).GetField("maxStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(this, 1);
        }

        #endregion
    }
}
