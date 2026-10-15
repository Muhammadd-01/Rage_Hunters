using UnityEngine;

namespace RageHunters.Player
{
    /// <summary>
    /// Bridges PlayerController, PlayerInputHandler, and PlayerStats states to the character Animator.
    /// Uses pre-cached Animator.StringToHash IDs for zero-garbage performance.
    /// Handles missing or dynamically instantiated Animators gracefully.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerAnimator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Animator Reference")]
        [SerializeField]
        [Tooltip("Target Animator component. If null, auto-searches children or self on Awake.")]
        private Animator _animator;

        [Header("Damping Settings")]
        [SerializeField]
        [Tooltip("Damping time applied to the Speed float parameter for smooth animation blending.")]
        private float _speedDampTime = 0.1f;

        [Header("Dependencies")]
        [SerializeField]
        [Tooltip("PlayerController reference. Auto-acquired on Awake if null.")]
        private PlayerController _playerController;

        [SerializeField]
        [Tooltip("PlayerInputHandler reference. Auto-acquired on Awake if null.")]
        private PlayerInputHandler _inputHandler;

        [SerializeField]
        [Tooltip("PlayerStats reference. Auto-acquired on Awake if null.")]
        private PlayerStats _playerStats;

        #endregion

        #region Cached Parameter Hashes

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
        private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
        private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage");
        private static readonly int DieHash = Animator.StringToHash("Die");

        #endregion

        #region Private Fields

        private bool _animatorMissingLogged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current Animator component, if assigned.
        /// </summary>
        public Animator Animator => _animator;

        /// <summary>
        /// Indicates whether an active Animator is currently bound.
        /// </summary>
        public bool HasAnimator => _animator != null;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            LocateDependencies();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            if (_animator == null)
            {
                // Periodically check if an animator was added dynamically to children
                _animator = GetComponentInChildren<Animator>();
                if (_animator == null)
                {
                    if (!_animatorMissingLogged)
                    {
                        Debug.Log($"[PlayerAnimator] No Animator found on '{gameObject.name}' or its children. Animation updates paused until assigned.");
                        _animatorMissingLogged = true;
                    }
                    return;
                }
            }

            UpdateLocomotionParameters(Time.deltaTime);
        }

        #endregion

        #region Dependency Binding

        private void LocateDependencies()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_playerController == null)
            {
                _playerController = GetComponent<PlayerController>();
            }

            if (_inputHandler == null)
            {
                _inputHandler = GetComponent<PlayerInputHandler>();
            }

            if (_playerStats == null)
            {
                _playerStats = GetComponent<PlayerStats>();
            }
        }

        private void SubscribeEvents()
        {
            if (_playerController != null)
            {
                _playerController.OnJumpInitiated += HandleJumpInitiated;
            }

            if (_inputHandler != null)
            {
                _inputHandler.OnAttackTriggered += HandleAttackTriggered;
            }

            if (_playerStats != null)
            {
                _playerStats.OnDamageTaken += HandleDamageTaken;
                _playerStats.OnDeath += HandleDeath;
                _playerStats.OnRespawn += HandleRespawn;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_playerController != null)
            {
                _playerController.OnJumpInitiated -= HandleJumpInitiated;
            }

            if (_inputHandler != null)
            {
                _inputHandler.OnAttackTriggered -= HandleAttackTriggered;
            }

            if (_playerStats != null)
            {
                _playerStats.OnDamageTaken -= HandleDamageTaken;
                _playerStats.OnDeath -= HandleDeath;
                _playerStats.OnRespawn -= HandleRespawn;
            }
        }

        #endregion

        #region Continuous Parameter Updates

        private void UpdateLocomotionParameters(float deltaTime)
        {
            if (_playerController == null) return;

            // Update Speed with smoothing damp time
            float speed = _playerController.CurrentSpeed;
            _animator.SetFloat(SpeedHash, speed, _speedDampTime, deltaTime);

            // Ground and Crouch states
            _animator.SetBool(IsGroundedHash, _playerController.IsGrounded);
            _animator.SetBool(IsCrouchingHash, _playerController.IsCrouching);

            // Falling state
            bool isFalling = _playerController.CurrentState == PlayerController.MovementState.Falling;
            _animator.SetBool(IsFallingHash, isFalling);
        }

        #endregion

        #region Event Callbacks

        private void HandleJumpInitiated()
        {
            TriggerJump();
        }

        private void HandleAttackTriggered()
        {
            TriggerAttack();
        }

        private void HandleDamageTaken(float damage, float remainingHealth)
        {
            TriggerTakeDamage();
        }

        private void HandleDeath()
        {
            TriggerDie();
        }

        private void HandleRespawn()
        {
            if (_animator == null) return;

            _animator.ResetTrigger(DieHash);
            _animator.ResetTrigger(TakeDamageHash);
            _animator.ResetTrigger(AttackHash);
            _animator.ResetTrigger(IsJumpingHash);
            _animator.Rebind();
        }

        #endregion

        #region Public Trigger API

        /// <summary>
        /// Explicitly assigns or replaces the Animator (e.g. after spawning a character model prefab).
        /// </summary>
        public void SetAnimator(Animator animator)
        {
            _animator = animator;
            _animatorMissingLogged = false;
        }

        /// <summary>
        /// Triggers the jump animation.
        /// </summary>
        public void TriggerJump()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(IsJumpingHash);
            }
        }

        /// <summary>
        /// Triggers the attack animation.
        /// </summary>
        public void TriggerAttack()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(AttackHash);
            }
        }

        /// <summary>
        /// Triggers the damage hit reaction animation.
        /// </summary>
        public void TriggerTakeDamage()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(TakeDamageHash);
            }
        }

        /// <summary>
        /// Triggers the death animation.
        /// </summary>
        public void TriggerDie()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(DieHash);
            }
        }

        #endregion
    }
}
