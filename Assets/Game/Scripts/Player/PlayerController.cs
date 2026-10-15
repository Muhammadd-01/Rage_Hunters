using System;
using UnityEngine;
using RageHunters.Core;

namespace RageHunters.Player
{
    /// <summary>
    /// Third-person character controller driving locomotion, rotation, crouching, jumping,
    /// and state transitions via Unity's CharacterController component.
    /// Fully mobile-compatible without mouse-lock dependencies.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        #region State Definitions

        /// <summary>
        /// Locomotion and traversal states of the player character.
        /// </summary>
        public enum MovementState
        {
            Idle,
            Walking,
            Running,
            Sprinting,
            Crouching,
            Jumping,
            Falling
        }

        #endregion

        #region Serialized Fields

        [Header("Locomotion Speeds")]
        [SerializeField]
        [Tooltip("Speed when moving with light input or walking.")]
        private float _walkSpeed = GameConstants.PlayerDefaults.DefaultWalkSpeed;

        [SerializeField]
        [Tooltip("Standard running speed with full joystick/key input.")]
        private float _runSpeed = GameConstants.PlayerDefaults.DefaultRunSpeed;

        [SerializeField]
        [Tooltip("Maximum sprint speed when sprinting is engaged.")]
        private float _sprintSpeed = GameConstants.PlayerDefaults.DefaultSprintSpeed;

        [SerializeField]
        [Tooltip("Locomotion speed while crouched.")]
        private float _crouchSpeed = GameConstants.PlayerDefaults.DefaultCrouchSpeed;

        [Header("Acceleration & Smoothing")]
        [SerializeField]
        [Tooltip("Acceleration rate towards target horizontal speed (units/s^2).")]
        private float _acceleration = 12f;

        [SerializeField]
        [Tooltip("Deceleration rate when stopping (units/s^2).")]
        private float _deceleration = 16f;

        [SerializeField]
        [Tooltip("Smoothing time for character yaw rotation towards movement direction.")]
        private float _rotationSmoothTime = 0.1f;

        [Header("Jump & Gravity")]
        [SerializeField]
        [Tooltip("Initial upward vertical velocity applied on jump.")]
        private float _jumpForce = GameConstants.PlayerDefaults.DefaultJumpForce;

        [SerializeField]
        [Tooltip("Downward gravitational acceleration.")]
        private float _gravity = GameConstants.PlayerDefaults.DefaultGravity;

        [SerializeField]
        [Tooltip("Maximum falling velocity clamp.")]
        private float _terminalVelocity = -50f;

        [SerializeField]
        [Tooltip("Slight downward velocity applied when grounded to maintain surface contact.")]
        private float _groundedDownwardForce = -2.5f;

        [SerializeField]
        [Tooltip("Grace period (seconds) during which player can still jump after walking off a ledge.")]
        private float _coyoteTime = 0.15f;

        [SerializeField]
        [Tooltip("Buffer window (seconds) where jump input pressed just before landing is executed upon landing.")]
        private float _jumpBufferTime = 0.12f;

        [Header("Ground Detection")]
        [SerializeField]
        [Tooltip("Radius of the ground check sphere.")]
        private float _groundCheckRadius = 0.28f;

        [SerializeField]
        [Tooltip("Vertical offset from character base for ground sphere casting.")]
        private float _groundCheckOffset = 0.05f;

        [SerializeField]
        [Tooltip("Layer mask identifying solid ground.")]
        private LayerMask _groundLayers = ~0;

        [Header("Crouch Dimensions")]
        [SerializeField]
        [Tooltip("Standing height for the CharacterController capsule.")]
        private float _standingHeight = 2.0f;

        [SerializeField]
        [Tooltip("Crouched height for the CharacterController capsule.")]
        private float _crouchHeight = 1.2f;

        [SerializeField]
        [Tooltip("Center of the capsule when standing.")]
        private Vector3 _standingCenter = new(0f, 1.0f, 0f);

        [SerializeField]
        [Tooltip("Center of the capsule when crouched.")]
        private Vector3 _crouchCenter = new(0f, 0.6f, 0f);

        [SerializeField]
        [Tooltip("Transition speed for capsule height and center interpolation.")]
        private float _crouchTransitionSpeed = 10f;

        [Header("Dependencies")]
        [SerializeField]
        [Tooltip("Optional camera transform reference. Defaults to Camera.main if null.")]
        private Transform _cameraTransform;

        [SerializeField]
        [Tooltip("Input handler bridging user actions. Auto-acquired on Awake if null.")]
        private PlayerInputHandler _inputHandler;

        [SerializeField]
        [Tooltip("Optional stats reference for stamina checks. Auto-acquired if present.")]
        private PlayerStats _playerStats;

        #endregion

        #region Private Fields

        private CharacterController _characterController;
        private MovementState _currentState = MovementState.Idle;
        private MovementState _previousState = MovementState.Idle;

        private float _currentHorizontalSpeed;
        private float _verticalVelocity;
        private float _rotationVelocity;

        private bool _isGrounded;
        private float _coyoteTimeCounter;
        private float _jumpBufferCounter;

        private Vector3 _moveDirection;
        private Vector3 _desiredPlanarVelocity;

        #endregion

        #region Properties

        /// <summary>
        /// Current active locomotion state of the character.
        /// </summary>
        public MovementState CurrentState => _currentState;

        /// <summary>
        /// Previous locomotion state before the most recent transition.
        /// </summary>
        public MovementState PreviousState => _previousState;

        /// <summary>
        /// Current horizontal speed magnitude (meters/second).
        /// </summary>
        public float CurrentSpeed => _currentHorizontalSpeed;

        /// <summary>
        /// Current vertical velocity (Y axis).
        /// </summary>
        public float VerticalVelocity => _verticalVelocity;

        /// <summary>
        /// Whether the character is confirmed grounded.
        /// </summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>
        /// Whether the character is currently crouched.
        /// </summary>
        public bool IsCrouching => _currentState == MovementState.Crouching;

        /// <summary>
        /// CharacterController attached to this GameObject.
        /// </summary>
        public CharacterController CharacterController => _characterController;

        #endregion

        #region Events

        /// <summary>
        /// Fired whenever the locomotion state changes (previousState, newState).
        /// </summary>
        public event Action<MovementState, MovementState> OnStateChanged;

        /// <summary>
        /// Fired whenever the locomotion state changes (newState).
        /// </summary>
        public event Action<MovementState> OnMovementStateChanged;

        /// <summary>
        /// Fired when the character initiates a jump.
        /// </summary>
        public event Action OnJumpInitiated;

        /// <summary>
        /// Fired when the character touches down on the ground after falling or jumping.
        /// </summary>
        public event Action OnLanded;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_inputHandler == null)
            {
                _inputHandler = GetComponent<PlayerInputHandler>();
            }

            if (_playerStats == null)
            {
                _playerStats = GetComponent<PlayerStats>();
            }

            // Exclude Player layer from ground layers if set to Everything
            int playerLayer = gameObject.layer;
            _groundLayers &= ~(1 << playerLayer);
        }

        private void Start()
        {
            ResolveCameraReference();
            SetState(MovementState.Idle);
        }

        private void Update()
        {
            if (_playerStats != null && !_playerStats.IsAlive)
            {
                ApplyDeadState();
                return;
            }

            ResolveCameraReference();
            CheckGroundedStatus();
            HandleJumpBufferAndCoyoteTime(Time.deltaTime);
            UpdateCrouchDimensions(Time.deltaTime);
            CalculateMovement(Time.deltaTime);
            ApplyGravityAndJump(Time.deltaTime);
            ApplyFinalMotion(Time.deltaTime);
            UpdateLocomotionState();
        }

        #endregion

        #region Camera Reference

        private void ResolveCameraReference()
        {
            if (_cameraTransform == null && UnityEngine.Camera.main != null)
            {
                _cameraTransform = UnityEngine.Camera.main.transform;
            }
        }

        #endregion

        #region Ground Detection

        private void CheckGroundedStatus()
        {
            bool previousGrounded = _isGrounded;

            // CharacterController internal grounded check
            bool ccGrounded = _characterController.isGrounded;

            // Supplemental sphere check for edge detection and stability
            Vector3 spherePosition = transform.position + Vector3.up * (_characterController.radius - _groundCheckOffset);
            bool sphereGrounded = Physics.CheckSphere(spherePosition, _groundCheckRadius, _groundLayers, QueryTriggerInteraction.Ignore);

            _isGrounded = ccGrounded || sphereGrounded;

            // Touchdown transition
            if (!previousGrounded && _isGrounded && _verticalVelocity <= 0f)
            {
                OnLanded?.Invoke();
            }
        }

        #endregion

        #region Movement & Rotation

        private void CalculateMovement(float deltaTime)
        {
            Vector2 input = _inputHandler != null ? _inputHandler.MoveInput : Vector2.zero;
            float inputMagnitude = Mathf.Clamp01(input.magnitude);

            // Compute camera-relative movement direction
            if (inputMagnitude > 0.01f && _cameraTransform != null)
            {
                Vector3 camForward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
                Vector3 camRight = Vector3.ProjectOnPlane(_cameraTransform.right, Vector3.up).normalized;
                _moveDirection = (camForward * input.y + camRight * input.x).normalized;

                // Smoothly rotate character towards movement direction
                float targetAngle = Mathf.Atan2(_moveDirection.x, _moveDirection.z) * Mathf.Rad2Deg;
                float smoothedAngle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    targetAngle,
                    ref _rotationVelocity,
                    _rotationSmoothTime
                );
                transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
            }
            else
            {
                _moveDirection = Vector3.zero;
            }

            // Determine target speed based on input & state
            float targetSpeed = DetermineTargetSpeed(inputMagnitude);

            // Smooth acceleration / deceleration
            float accelerationRate = targetSpeed > _currentHorizontalSpeed ? _acceleration : _deceleration;
            _currentHorizontalSpeed = Mathf.MoveTowards(_currentHorizontalSpeed, targetSpeed, accelerationRate * deltaTime);

            // Calculate planar velocity
            _desiredPlanarVelocity = _moveDirection * _currentHorizontalSpeed;
        }

        private float DetermineTargetSpeed(float inputMagnitude)
        {
            if (inputMagnitude <= 0.01f)
            {
                return 0f;
            }

            // Check crouch
            if (_inputHandler != null && _inputHandler.CrouchPressed)
            {
                return _crouchSpeed * inputMagnitude;
            }

            // Check sprint condition
            bool sprintRequested = _inputHandler != null && _inputHandler.SprintPressed;
            bool canSprint = _playerStats == null || _playerStats.CanSprint;

            if (sprintRequested && canSprint && inputMagnitude > 0.7f)
            {
                if (_playerStats != null)
                {
                    _playerStats.SetSprinting(true);
                }
                return _sprintSpeed;
            }
            else
            {
                if (_playerStats != null)
                {
                    _playerStats.SetSprinting(false);
                }

                // If input magnitude is low, walk; otherwise, run
                return (inputMagnitude < 0.6f ? _walkSpeed : _runSpeed) * inputMagnitude;
            }
        }

        #endregion

        #region Crouch Dimensions

        private void UpdateCrouchDimensions(float deltaTime)
        {
            bool isCrouching = _inputHandler != null && _inputHandler.CrouchPressed;

            float targetHeight = isCrouching ? _crouchHeight : _standingHeight;
            Vector3 targetCenter = isCrouching ? _crouchCenter : _standingCenter;

            if (!Mathf.Approximately(_characterController.height, targetHeight))
            {
                float newHeight = Mathf.MoveTowards(_characterController.height, targetHeight, _crouchTransitionSpeed * deltaTime);
                Vector3 newCenter = Vector3.MoveTowards(_characterController.center, targetCenter, _crouchTransitionSpeed * deltaTime);

                _characterController.height = newHeight;
                _characterController.center = newCenter;
            }
        }

        #endregion

        #region Jumping & Gravity

        private void HandleJumpBufferAndCoyoteTime(float deltaTime)
        {
            if (_isGrounded)
            {
                _coyoteTimeCounter = _coyoteTime;
            }
            else
            {
                _coyoteTimeCounter -= deltaTime;
            }

            if (_inputHandler != null && _inputHandler.JumpPressed)
            {
                _jumpBufferCounter = _jumpBufferTime;
            }
            else
            {
                _jumpBufferCounter -= deltaTime;
            }

            // Trigger jump if coyote time and jump buffer are both valid, and not crouching
            bool isCrouched = _inputHandler != null && _inputHandler.CrouchPressed;
            if (_jumpBufferCounter > 0f && _coyoteTimeCounter > 0f && !isCrouched)
            {
                ExecuteJump();
            }
        }

        private void ExecuteJump()
        {
            _verticalVelocity = _jumpForce;
            _coyoteTimeCounter = 0f;
            _jumpBufferCounter = 0f;

            OnJumpInitiated?.Invoke();
        }

        private void ApplyGravityAndJump(float deltaTime)
        {
            if (_isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = _groundedDownwardForce;
            }
            else
            {
                _verticalVelocity += _gravity * deltaTime;
                _verticalVelocity = Mathf.Max(_verticalVelocity, _terminalVelocity);
            }
        }

        private void ApplyFinalMotion(float deltaTime)
        {
            Vector3 totalVelocity = _desiredPlanarVelocity + Vector3.up * _verticalVelocity;
            _characterController.Move(totalVelocity * deltaTime);
        }

        #endregion

        #region Locomotion State Machine

        private void UpdateLocomotionState()
        {
            MovementState newState;

            if (!_isGrounded)
            {
                newState = _verticalVelocity > 0f ? MovementState.Jumping : MovementState.Falling;
            }
            else if (_inputHandler != null && _inputHandler.CrouchPressed)
            {
                newState = MovementState.Crouching;
            }
            else if (_currentHorizontalSpeed < 0.1f)
            {
                newState = MovementState.Idle;
            }
            else if (_inputHandler != null && _inputHandler.SprintPressed && (_playerStats == null || _playerStats.CanSprint) && _currentHorizontalSpeed > _runSpeed * 1.05f)
            {
                newState = MovementState.Sprinting;
            }
            else if (_currentHorizontalSpeed > _walkSpeed * 1.1f)
            {
                newState = MovementState.Running;
            }
            else
            {
                newState = MovementState.Walking;
            }

            SetState(newState);
        }

        private void SetState(MovementState newState)
        {
            if (_currentState == newState) return;

            _previousState = _currentState;
            _currentState = newState;

            OnStateChanged?.Invoke(_previousState, _currentState);
            OnMovementStateChanged?.Invoke(_currentState);
        }

        private void ApplyDeadState()
        {
            _currentHorizontalSpeed = 0f;
            _verticalVelocity = _groundedDownwardForce;
            _desiredPlanarVelocity = Vector3.zero;

            if (!_characterController.isGrounded)
            {
                _verticalVelocity += _gravity * Time.deltaTime;
                _characterController.Move(Vector3.up * (_verticalVelocity * Time.deltaTime));
            }

            SetState(MovementState.Idle);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Explicitly sets the camera transform used for camera-relative steering.
        /// </summary>
        public void SetCameraTransform(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        /// <summary>
        /// Manually forces a vertical velocity (useful for launch pads or knockback).
        /// </summary>
        public void AddVerticalImpulse(float impulse)
        {
            _verticalVelocity = impulse;
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }

            if (_characterController != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Vector3 spherePosition = transform.position + Vector3.up * (_characterController.radius - _groundCheckOffset);
                Gizmos.DrawWireSphere(spherePosition, _groundCheckRadius);
            }
        }

        #endregion
    }
}
