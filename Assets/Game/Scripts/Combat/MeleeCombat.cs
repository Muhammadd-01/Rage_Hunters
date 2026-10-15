using System;
using UnityEngine;

namespace RageHunters.Combat
{
    /// <summary>
    /// Component responsible for melee attack execution, spatial sweep/sphere casting,
    /// hit registration with IDamageable targets, and animator trigger integration.
    /// Optimized for mobile: uses non-allocating physics queries.
    /// </summary>
    public class MeleeCombat : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Combat Settings")]
        [Min(1f)]
        [SerializeField] private float baseDamage = 25f;
        [Min(0.5f)]
        [SerializeField] private float attackRange = 2.2f;
        [Min(0.1f)]
        [SerializeField] private float attackRadius = 0.6f;
        [Min(0.1f)]
        [SerializeField] private float attackCooldown = 1.0f;
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Attacker Stats")]
        [SerializeField] private float criticalChance = 0.1f;
        [SerializeField] private float criticalMultiplier = 1.5f;

        [Header("Animation & Visuals")]
        [SerializeField] private Animator animator;
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private GameObject hitEffectPrefab;

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = new(1f, 0f, 0f, 0.4f);

        #endregion

        #region Private Fields

        private float _lastAttackTime = -999f;
        private readonly Collider[] _hitBuffer = new Collider[16];
        private AttackerStats _attackerStats;

        #endregion

        #region Events

        public event Action OnAttackStarted;
        public event Action<IDamageable, DamageCalculationResult> OnHitLanded;

        #endregion

        #region Properties

        public bool CanAttack => Time.time >= _lastAttackTime + attackCooldown;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public float BaseDamage => baseDamage;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (attackOrigin == null)
            {
                attackOrigin = transform;
            }

            _attackerStats = new AttackerStats
            {
                AttackPower = 1.0f,
                CriticalChance = criticalChance,
                CriticalMultiplier = criticalMultiplier,
                ArmorPenetration = 0.0f
            };
        }

        #endregion

        #region Public Combat API

        /// <summary>
        /// Configure melee parameters from a weapon data asset.
        /// </summary>
        public void ApplyWeaponData(WeaponDataSO weapon)
        {
            if (weapon == null) return;

            baseDamage = weapon.BaseDamage;
            attackRange = weapon.Range;
            attackRadius = weapon.HitboxRadius;
            attackCooldown = weapon.AttackCooldown;
            criticalChance = weapon.CriticalChance;
            criticalMultiplier = weapon.CriticalMultiplier;

            if (!string.IsNullOrEmpty(weapon.AttackAnimationTrigger))
            {
                attackTrigger = weapon.AttackAnimationTrigger;
            }

            if (weapon.HitImpactEffect != null)
            {
                hitEffectPrefab = weapon.HitImpactEffect;
            }

            _attackerStats.CriticalChance = criticalChance;
            _attackerStats.CriticalMultiplier = criticalMultiplier;
        }

        /// <summary>
        /// Attempts to trigger a melee attack. Returns true if executed.
        /// </summary>
        public bool TryAttack()
        {
            if (!CanAttack) return false;

            ExecuteAttack();
            return true;
        }

        /// <summary>
        /// Forces attack execution, resets cooldown timer, triggers animation, and sweeps for hits.
        /// </summary>
        public void ExecuteAttack()
        {
            _lastAttackTime = Time.time;

            if (animator != null && !string.IsNullOrEmpty(attackTrigger))
            {
                animator.SetTrigger(attackTrigger);
            }

            OnAttackStarted?.Invoke();

            // Perform non-alloc sphere sweep in front of the attacker
            PerformHitDetection();
        }

        /// <summary>
        /// Can also be called via Animation Event at the precise swing frame.
        /// </summary>
        public void OnAnimationHitFrame()
        {
            PerformHitDetection();
        }

        #endregion

        #region Hit Detection

        private void PerformHitDetection()
        {
            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
            Vector3 forward = attackOrigin != null ? attackOrigin.forward : transform.forward;
            Vector3 castCenter = origin + forward * (attackRange * 0.5f);

            int hitCount = Physics.OverlapSphereNonAlloc(
                castCenter,
                attackRange * 0.5f + attackRadius,
                _hitBuffer,
                hitLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _hitBuffer[i];
                if (hit == null) continue;

                // Do not hit self or descendants
                if (hit.transform.root == transform.root) continue;

                if (hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    Vector3 closestPoint = hit.ClosestPoint(origin);
                    float distance = Vector3.Distance(origin, closestPoint);

                    DefenderStats defenderStats = DefenderStats.Default;
                    var result = DamageSystem.CalculateDamage(
                        _attackerStats,
                        defenderStats,
                        null,
                        distance,
                        1.0f);

                    damageable.TakeDamage(result.FinalDamage > 0f ? result.FinalDamage : baseDamage, closestPoint, gameObject);
                    OnHitLanded?.Invoke(damageable, result);

                    if (hitEffectPrefab != null)
                    {
                        Instantiate(hitEffectPrefab, closestPoint, Quaternion.LookRotation(forward));
                    }
                }
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            Transform origin = attackOrigin != null ? attackOrigin : transform;
            Vector3 center = origin.position + origin.forward * (attackRange * 0.5f);

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(center, attackRange * 0.5f + attackRadius);
            Gizmos.DrawRay(origin.position, origin.forward * attackRange);
        }

        #endregion
    }
}
