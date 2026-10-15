using UnityEngine;

namespace RageHunters.Combat
{
    /// <summary>
    /// Body location category for localized damage calculations.
    /// </summary>
    public enum BodyPartType
    {
        Head,
        Torso,
        Arms,
        Legs,
        Weakspot
    }

    /// <summary>
    /// Collider-based hit detection zone that maps impacts to specific body parts
    /// and relays damage with multipliers to the root IDamageable receiver.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HitBox : MonoBehaviour, IDamageable
    {
        #region Inspector Fields

        [Header("Zone Configuration")]
        [SerializeField] private BodyPartType bodyPart = BodyPartType.Torso;
        [Tooltip("Damage multiplier applied when this hitbox is struck (e.g., 2.0x for Head)")]
        [SerializeField] private float damageMultiplier = 1.0f;

        [Header("Target Target Link")]
        [Tooltip("The root IDamageable receiving damage. Auto-found on parent if not assigned.")]
        [SerializeField] private Component damageableReceiver;

        #endregion

        #region Private Fields

        private IDamageable _targetDamageable;
        private Collider _collider;

        #endregion

        #region Properties

        public BodyPartType BodyPart => bodyPart;
        public float DamageMultiplier => damageMultiplier;

        public float CurrentHealth => _targetDamageable?.CurrentHealth ?? 0f;
        public float MaxHealth => _targetDamageable?.MaxHealth ?? 0f;
        public bool IsDead => _targetDamageable?.IsDead ?? true;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _collider = GetComponent<Collider>();

            if (damageableReceiver != null && damageableReceiver is IDamageable assigned)
            {
                _targetDamageable = assigned;
            }
            else
            {
                _targetDamageable = GetComponentInParent<IDamageable>();
            }

            // Auto-tune default multipliers based on body part if still at 1.0
            if (Mathf.Approximately(damageMultiplier, 1.0f))
            {
                damageMultiplier = bodyPart switch
                {
                    BodyPartType.Head => 2.0f,
                    BodyPartType.Torso => 1.0f,
                    BodyPartType.Arms => 0.8f,
                    BodyPartType.Legs => 0.75f,
                    BodyPartType.Weakspot => 2.5f,
                    _ => 1.0f
                };
            }
        }

        #endregion

        #region IDamageable Implementation

        /// <summary>
        /// Scales the incoming damage by this hitbox's body part multiplier
        /// and relays it to the parent IDamageable.
        /// </summary>
        public void TakeDamage(float amount, Vector3 hitPoint, GameObject attacker)
        {
            if (_targetDamageable == null || _targetDamageable.IsDead)
            {
                return;
            }

            float scaledDamage = amount * damageMultiplier;
            _targetDamageable.TakeDamage(scaledDamage, hitPoint, attacker);
        }

        #endregion

        #region Public Helpers

        /// <summary>
        /// Explicitly bind a damage receiver to this hitbox at runtime (e.g. for procedural ragdolls).
        /// </summary>
        public void SetDamageReceiver(IDamageable receiver)
        {
            _targetDamageable = receiver;
        }

        #endregion
    }
}
