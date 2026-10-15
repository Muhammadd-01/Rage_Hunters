using System.Collections.Generic;
using UnityEngine;

namespace RageHunters.AI
{
    /// <summary>
    /// Patrol movement behavior profile.
    /// </summary>
    public enum PatrolBehavior
    {
        Sequential,
        PingPong,
        Random,
        Stationary
    }

    /// <summary>
    /// Defines archetype attributes, detection parameters, and AI behavior tuning for villains.
    /// </summary>
    [CreateAssetMenu(fileName = "NewVillainArchetype", menuName = "Rage Hunters/AI/Villain Archetype")]
    public class VillainArchetypeSO : ScriptableObject
    {
        #region Inspector Fields

        [Header("Identity")]
        [SerializeField] private string archetypeName = "Raider";
        [TextArea(2, 4)]
        [SerializeField] private string description = string.Empty;
        [SerializeField] private Sprite icon;

        [Header("Base Attributes")]
        [Min(1f)]
        [SerializeField] private float baseHealth = 100f;
        [Min(0.5f)]
        [SerializeField] private float moveSpeed = 3.5f;
        [Min(1f)]
        [SerializeField] private float chaseSpeed = 5.5f;
        [Min(0.5f)]
        [SerializeField] private float retreatSpeed = 6.0f;

        [Header("Perception & Detection")]
        [Tooltip("Maximum sight distance in meters")]
        [Min(1f)]
        [SerializeField] private float detectionRange = 15f;
        [Tooltip("Sound detection radius in meters")]
        [Min(1f)]
        [SerializeField] private float hearingRange = 10f;
        [Tooltip("Horizontal field of view angle in degrees")]
        [Range(10f, 360f)]
        [SerializeField] private float visionAngle = 90f;
        [Tooltip("Detection delay before fully recognizing a target (seconds)")]
        [Min(0.05f)]
        [SerializeField] private float detectionDelay = 0.5f;

        [Header("Behavior Tendencies (0 - 1)")]
        [Tooltip("Likelihood to press attack vs keep distance")]
        [Range(0f, 1f)]
        [SerializeField] private float aggressiveness = 0.7f;
        [Tooltip("Reduces sound emission and visibility footprint")]
        [Range(0f, 1f)]
        [SerializeField] private float stealthiness = 0.2f;
        [Tooltip("Tactical competence: flanking, using cover, retreating when low health")]
        [Range(0f, 1f)]
        [SerializeField] private float intelligence = 0.5f;

        [Header("Combat")]
        [Tooltip("Preferred engagement distance in meters")]
        [Min(0.5f)]
        [SerializeField] private float preferredCombatRange = 2.0f;
        [Tooltip("Attack damage dealt per hit")]
        [Min(1f)]
        [SerializeField] private float baseAttackDamage = 15f;
        [Tooltip("Minimum time between attacks")]
        [Min(0.2f)]
        [SerializeField] private float attackCooldown = 1.2f;
        [Tooltip("Health percentage below which the villain considers retreating (0-1)")]
        [Range(0f, 0.8f)]
        [SerializeField] private float retreatHealthThreshold = 0.25f;

        [Header("Patrol & Search")]
        [SerializeField] private PatrolBehavior patrolBehavior = PatrolBehavior.Sequential;
        [Tooltip("Time to linger at each patrol waypoint (seconds)")]
        [Min(0f)]
        [SerializeField] private float patrolWaitDuration = 2.0f;
        [Tooltip("Time spent searching last known position before giving up (seconds)")]
        [Min(1f)]
        [SerializeField] private float searchDuration = 5.0f;

        [Header("Special Abilities")]
        [SerializeField] private List<string> specialAbilities = new();

        #endregion

        #region Properties

        public string ArchetypeName => archetypeName;
        public string Description => description;
        public Sprite Icon => icon;

        public float BaseHealth => baseHealth;
        public float MoveSpeed => moveSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float RetreatSpeed => retreatSpeed;

        public float DetectionRange => detectionRange;
        public float HearingRange => hearingRange;
        public float VisionAngle => visionAngle;
        public float DetectionDelay => detectionDelay;

        public float Aggressiveness => aggressiveness;
        public float Stealthiness => stealthiness;
        public float Intelligence => intelligence;

        public float PreferredCombatRange => preferredCombatRange;
        public float BaseAttackDamage => baseAttackDamage;
        public float AttackCooldown => attackCooldown;
        public float RetreatHealthThreshold => retreatHealthThreshold;

        public PatrolBehavior PatrolBehavior => patrolBehavior;
        public float PatrolWaitDuration => patrolWaitDuration;
        public float SearchDuration => searchDuration;

        public IReadOnlyList<string> SpecialAbilities => specialAbilities;

        #endregion
    }
}
