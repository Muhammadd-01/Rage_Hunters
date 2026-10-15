using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RageHunters.Combat;
using RageHunters.AI.States;

namespace RageHunters.AI
{
    /// <summary>
    /// Core AI Enemy Controller component. Integrates NavMesh pathfinding, perception cones,
    /// target tracking, combat execution, health management, and AIStateMachine state orchestration.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIController : MonoBehaviour, IDamageable
    {
        #region Inspector Fields

        [Header("Configuration Asset")]
        [SerializeField] private VillainArchetypeSO archetype;

        [Header("Perception Layers")]
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private LayerMask obstructionMask;
        [SerializeField] private Transform eyesOrigin;

        [Header("Patrol Waypoints")]
        [SerializeField] private List<Transform> patrolWaypoints = new();

        [Header("Combat References")]
        [SerializeField] private MeleeCombat meleeCombat;
        [SerializeField] private Animator animator;

        [Header("Debug & Gizmos")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool logStateTransitions = false;

        #endregion

        #region Private Fields

        private NavMeshAgent _agent;
        private AIStateMachine _stateMachine;
        private Transform _target;
        private Vector3 _lastKnownTargetPosition;
        private int _currentWaypointIndex;
        private float _currentHealth;
        private bool _isDead;
        private float _lastAttackTime = -999f;
        private readonly Collider[] _perceptionBuffer = new Collider[16];

        #endregion

        #region Events

        public event Action<Transform> OnTargetSpotted;
        public event Action OnTargetLost;
        public event Action<float, Vector3, GameObject> OnDamaged;
        public event Action<GameObject> OnDeath;

        #endregion

        #region Properties

        public VillainArchetypeSO Archetype => archetype;
        public NavMeshAgent Agent => _agent;
        public AIStateMachine StateMachine => _stateMachine;
        public Transform Target => _target;
        public Vector3 LastKnownTargetPosition
        {
            get => _lastKnownTargetPosition;
            set => _lastKnownTargetPosition = value;
        }
        public bool HasTarget => _target != null;
        public IReadOnlyList<Transform> PatrolWaypoints => patrolWaypoints;
        public int CurrentWaypointIndex
        {
            get => _currentWaypointIndex;
            set => _currentWaypointIndex = value;
        }

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => archetype != null ? archetype.BaseHealth : 100f;
        public bool IsDead => _isDead;

        public Transform EyesTransform => eyesOrigin != null ? eyesOrigin : transform;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();

            if (meleeCombat == null)
            {
                meleeCombat = GetComponentInChildren<MeleeCombat>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (eyesOrigin == null)
            {
                eyesOrigin = transform;
            }

            // Initialize health
            _currentHealth = archetype != null ? archetype.BaseHealth : 100f;

            // Configure NavMeshAgent defaults
            if (archetype != null)
            {
                _agent.speed = archetype.MoveSpeed;
                _agent.stoppingDistance = Mathf.Max(0.5f, archetype.PreferredCombatRange * 0.75f);
            }

            InitializeStateMachine();
        }

        private void Start()
        {
            // Initial transition
            if (patrolWaypoints != null && patrolWaypoints.Count > 0)
            {
                _stateMachine.TransitionTo(AIStateType.Patrol);
            }
            else
            {
                _stateMachine.TransitionTo(AIStateType.Idle);
            }
        }

        private void Update()
        {
            if (_isDead) return;

            UpdatePerceptionTargetPosition();
            _stateMachine.Update();

            // Forward speed to animator if present
            if (animator != null && _agent != null)
            {
                animator.SetFloat("Speed", _agent.velocity.magnitude);
            }
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            _stateMachine.FixedUpdate();
        }

        #endregion

        #region State Machine Setup

        private void InitializeStateMachine()
        {
            _stateMachine = new AIStateMachine(gameObject.name, logStateTransitions);

            _stateMachine.RegisterState(AIStateType.Idle, new IdleState(this));
            _stateMachine.RegisterState(AIStateType.Patrol, new PatrolState(this));
            _stateMachine.RegisterState(AIStateType.Investigate, new InvestigateState(this));
            _stateMachine.RegisterState(AIStateType.Detect, new DetectState(this));
            _stateMachine.RegisterState(AIStateType.Chase, new ChaseState(this));
            _stateMachine.RegisterState(AIStateType.Attack, new AttackState(this));
            _stateMachine.RegisterState(AIStateType.Search, new SearchState(this));
            _stateMachine.RegisterState(AIStateType.Retreat, new RetreatState(this));
            _stateMachine.RegisterState(AIStateType.ReturnToPatrol, new ReturnToPatrolState(this));
        }

        #endregion

        #region Perception System

        /// <summary>
        /// Scans surroundings for candidate targets using vision cone and hearing radius.
        /// </summary>
        public Transform DetectPotentialTarget()
        {
            if (_isDead) return null;

            float visionRange = archetype != null ? archetype.DetectionRange : 15f;
            float hearingRange = archetype != null ? archetype.HearingRange : 10f;
            float maxScanRadius = Mathf.Max(visionRange, hearingRange);

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                maxScanRadius,
                _perceptionBuffer,
                targetMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _perceptionBuffer[i];
                if (hit == null || hit.transform.root == transform.root) continue;

                Transform candidate = hit.transform;

                // 1. Check Vision Cone
                if (CanSeeTarget(candidate))
                {
                    SetTarget(candidate);
                    return candidate;
                }

                // 2. Check Hearing Radius
                if (CanHearTarget(candidate.position))
                {
                    SetTarget(candidate);
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Evaluates field of view angle, distance, and direct line of sight occlusion.
        /// </summary>
        public bool CanSeeTarget(Transform candidate)
        {
            if (candidate == null || _isDead) return false;

            float visionRange = archetype != null ? archetype.DetectionRange : 15f;
            float visionAngle = archetype != null ? archetype.VisionAngle : 90f;

            Vector3 eyePos = EyesTransform.position;
            Vector3 targetCenter = candidate.position + Vector3.up * 1.0f;
            Vector3 dirToTarget = (targetCenter - eyePos);
            float distance = dirToTarget.magnitude;

            if (distance > visionRange) return false;

            // Check FOV angle
            float angle = Vector3.Angle(transform.forward, dirToTarget.normalized);
            if (angle > visionAngle * 0.5f) return false;

            // Check raycast occlusion
            if (Physics.Raycast(eyePos, dirToTarget.normalized, out RaycastHit hit, distance, obstructionMask))
            {
                // Occluded by wall/obstacle
                if (hit.transform != candidate && !hit.transform.IsChildOf(candidate))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Evaluates whether a sound event origin falls within hearing radius.
        /// </summary>
        public bool CanHearTarget(Vector3 soundPosition, float soundRadius = 1f)
        {
            if (_isDead) return false;

            float hearingRange = archetype != null ? archetype.HearingRange : 10f;
            float totalHearingDistance = hearingRange + soundRadius;

            return Vector3.Distance(transform.position, soundPosition) <= totalHearingDistance;
        }

        private void UpdatePerceptionTargetPosition()
        {
            if (_target != null)
            {
                if (CanSeeTarget(_target))
                {
                    _lastKnownTargetPosition = _target.position;
                }
            }
        }

        public void SetTarget(Transform target)
        {
            if (_target != target)
            {
                _target = target;
                if (target != null)
                {
                    _lastKnownTargetPosition = target.position;
                    OnTargetSpotted?.Invoke(target);
                }
            }
        }

        public void ClearTarget()
        {
            if (_target != null)
            {
                _target = null;
                OnTargetLost?.Invoke();
            }
        }

        #endregion

        #region Navigation Helpers

        public void SetDestination(Vector3 destination)
        {
            if (_agent == null || !_agent.isActiveAndEnabled || !NavMesh.SamplePosition(destination, out var hit, 4f, NavMesh.AllAreas))
            {
                return;
            }

            _agent.isStopped = false;
            _agent.SetDestination(hit.position);
        }

        public bool HasReachedDestination(float threshold = 0.5f)
        {
            if (_agent == null || !_agent.isActiveAndEnabled || _agent.pathPending)
            {
                return false;
            }

            return _agent.remainingDistance <= Mathf.Max(_agent.stoppingDistance, threshold);
        }

        public void StopMovement()
        {
            if (_agent != null && _agent.isActiveAndEnabled)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }
        }

        public void ResumeMovement()
        {
            if (_agent != null && _agent.isActiveAndEnabled)
            {
                _agent.isStopped = false;
            }
        }

        public void SetSpeed(float speed)
        {
            if (_agent != null)
            {
                _agent.speed = speed;
            }
        }

        public void RotateTowards(Vector3 targetPosition, float rotationSpeed = 10f)
        {
            Vector3 direction = (targetPosition - transform.position);
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        #endregion

        #region Waypoint Helpers

        public Vector3 GetCurrentWaypointPosition()
        {
            if (patrolWaypoints == null || patrolWaypoints.Count == 0)
            {
                return transform.position;
            }

            _currentWaypointIndex = Mathf.Clamp(_currentWaypointIndex, 0, patrolWaypoints.Count - 1);
            return patrolWaypoints[_currentWaypointIndex] != null
                ? patrolWaypoints[_currentWaypointIndex].position
                : transform.position;
        }

        public void AdvanceToNextWaypoint()
        {
            if (patrolWaypoints == null || patrolWaypoints.Count == 0) return;

            PatrolBehavior behavior = archetype != null ? archetype.PatrolBehavior : PatrolBehavior.Sequential;

            switch (behavior)
            {
                case PatrolBehavior.Sequential:
                    _currentWaypointIndex = (_currentWaypointIndex + 1) % patrolWaypoints.Count;
                    break;
                case PatrolBehavior.Random:
                    _currentWaypointIndex = UnityEngine.Random.Range(0, patrolWaypoints.Count);
                    break;
                case PatrolBehavior.PingPong:
                    _currentWaypointIndex = (_currentWaypointIndex + 1) % patrolWaypoints.Count;
                    break;
                case PatrolBehavior.Stationary:
                    break;
            }
        }

        public Vector3 GetNearestWaypointPosition()
        {
            if (patrolWaypoints == null || patrolWaypoints.Count == 0)
            {
                return transform.position;
            }

            float closestDistSq = float.MaxValue;
            int closestIndex = 0;

            for (int i = 0; i < patrolWaypoints.Count; i++)
            {
                if (patrolWaypoints[i] == null) continue;

                float distSq = (patrolWaypoints[i].position - transform.position).sqrMagnitude;
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestIndex = i;
                }
            }

            _currentWaypointIndex = closestIndex;
            return patrolWaypoints[closestIndex].position;
        }

        #endregion

        #region Combat & Damage

        /// <summary>
        /// Executes an attack against the current target if cooldown permits.
        /// </summary>
        public bool TryPerformAttack()
        {
            float cooldown = archetype != null ? archetype.AttackCooldown : 1.2f;
            if (Time.time < _lastAttackTime + cooldown)
            {
                return false;
            }

            _lastAttackTime = Time.time;

            if (meleeCombat != null)
            {
                return meleeCombat.TryAttack();
            }

            // Direct attack fallback
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            if (_target != null && _target.TryGetComponent<IDamageable>(out var damageable))
            {
                float damage = archetype != null ? archetype.BaseAttackDamage : 15f;
                damageable.TakeDamage(damage, _target.position + Vector3.up, gameObject);
            }

            return true;
        }

        public void TakeDamage(float amount, Vector3 hitPoint, GameObject attacker)
        {
            if (_isDead) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnDamaged?.Invoke(amount, hitPoint, attacker);

            // Alert AI to attacker if not already targeting someone
            if (attacker != null)
            {
                SetTarget(attacker.transform);
                _lastKnownTargetPosition = attacker.transform.position;

                // Check retreat condition immediately
                float healthRatio = _currentHealth / MaxHealth;
                float retreatThreshold = archetype != null ? archetype.RetreatHealthThreshold : 0.25f;
                if (healthRatio <= retreatThreshold && _stateMachine.CurrentStateKey != AIStateType.Retreat)
                {
                    _stateMachine.TransitionTo(AIStateType.Retreat);
                }
                else if (_stateMachine.CurrentStateKey is AIStateType.Idle or AIStateType.Patrol)
                {
                    _stateMachine.TransitionTo(AIStateType.Detect);
                }
            }

            if (_currentHealth <= 0f)
            {
                Die(attacker);
            }
        }

        private void Die(GameObject attacker)
        {
            _isDead = true;
            StopMovement();

            if (animator != null)
            {
                animator.SetTrigger("Die");
            }

            OnDeath?.Invoke(attacker);
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            Vector3 eyePos = EyesTransform.position;
            float visionRange = archetype != null ? archetype.DetectionRange : 15f;
            float visionAngle = archetype != null ? archetype.VisionAngle : 90f;
            float hearingRange = archetype != null ? archetype.HearingRange : 10f;

            // Vision Cone (Yellow)
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.35f);
            Gizmos.DrawWireSphere(eyePos, visionRange);

            Vector3 forward = transform.forward;
            Vector3 leftBoundary = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * forward;
            Vector3 rightBoundary = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(eyePos, leftBoundary * visionRange);
            Gizmos.DrawRay(eyePos, rightBoundary * visionRange);

            // Hearing Radius (Blue)
            Gizmos.color = new Color(0.2f, 0.4f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, hearingRange);

            // Patrol path (Cyan)
            if (patrolWaypoints != null && patrolWaypoints.Count > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < patrolWaypoints.Count; i++)
                {
                    if (patrolWaypoints[i] == null) continue;
                    int next = (i + 1) % patrolWaypoints.Count;
                    if (patrolWaypoints[next] != null)
                    {
                        Gizmos.DrawLine(patrolWaypoints[i].position, patrolWaypoints[next].position);
                    }
                }
            }

            // Target / Last known position (Red)
            if (_target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _target.position);
                Gizmos.DrawWireSphere(_lastKnownTargetPosition, 0.75f);
            }
        }

        #endregion
    }
}
