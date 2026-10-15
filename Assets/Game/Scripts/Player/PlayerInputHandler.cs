using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RageHunters.Player
{
    /// <summary>
    /// Bridges Unity's new Input System actions to gameplay logic.
    /// Supports desktop mouse/keyboard, gamepads, and virtual mobile touch controls.
    /// Can be fed directly by mobile touch managers and on-screen controls.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInputHandler : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input Actions Asset")]
        [SerializeField]
        [Tooltip("The InputSystem_Actions asset containing the 'Player' action map.")]
        private InputActionAsset _inputActions;

        [Header("Action Names")]
        [SerializeField]
        private string _actionMapName = "Player";

        [SerializeField]
        private string _moveActionName = "Move";

        [SerializeField]
        private string _lookActionName = "Look";

        [SerializeField]
        private string _sprintActionName = "Sprint";

        [SerializeField]
        private string _jumpActionName = "Jump";

        [SerializeField]
        private string _crouchActionName = "Crouch";

        [SerializeField]
        private string _attackActionName = "Attack";

        [SerializeField]
        private string _interactActionName = "Interact";

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Automatically enable the Player action map on Start.")]
        private bool _autoEnableInput = true;

        #endregion

        #region Private Fields

        private InputActionMap _playerActionMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _jumpAction;
        private InputAction _crouchAction;
        private InputAction _attackAction;
        private InputAction _interactAction;

        // Input state backing fields
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _sprintPressed;
        private bool _jumpPressed;
        private bool _crouchToggled;
        private bool _attackPressed;
        private bool _interactPressed;

        // Virtual input overrides (used by virtual touch controls/UI)
        private Vector2 _virtualMoveInput;
        private Vector2 _virtualLookInput;
        private bool _virtualSprintActive;
        private bool _virtualAttackActive;
        private bool _virtualInteractActive;
        private bool _hasVirtualMove;
        private bool _hasVirtualLook;

        #endregion

        #region Properties

        /// <summary>
        /// 2D movement direction vector (X: horizontal, Y: vertical).
        /// Combines hardware input with touch virtual joystick input.
        /// </summary>
        public Vector2 MoveInput => _hasVirtualMove ? _virtualMoveInput : _moveInput;

        /// <summary>
        /// 2D camera look delta vector (X: yaw, Y: pitch).
        /// Combines hardware input with touch look zone input.
        /// </summary>
        public Vector2 LookInput => _hasVirtualLook ? _virtualLookInput : _lookInput;

        /// <summary>
        /// Indicates whether sprint is currently held or actively engaged.
        /// </summary>
        public bool SprintPressed => _sprintPressed || _virtualSprintActive;

        /// <summary>
        /// Consume-once jump flag. Reading this returns true if jump was triggered,
        /// then immediately consumes and clears the flag to prevent duplicate jumps.
        /// </summary>
        public bool JumpPressed
        {
            get
            {
                if (!_jumpPressed) return false;
                _jumpPressed = false;
                return true;
            }
        }

        /// <summary>
        /// Peeks at the jump flag without consuming it.
        /// </summary>
        public bool PeekJumpPressed => _jumpPressed;

        /// <summary>
        /// Current crouch toggle state. Toggled by crouch button presses.
        /// </summary>
        public bool CrouchPressed => _crouchToggled;

        /// <summary>
        /// Indicates whether attack action was triggered this frame.
        /// </summary>
        public bool AttackPressed => _attackPressed || _virtualAttackActive;

        /// <summary>
        /// Indicates whether interact action was triggered this frame.
        /// </summary>
        public bool InteractPressed => _interactPressed || _virtualInteractActive;

        /// <summary>
        /// Indicates whether the player input map is currently active.
        /// </summary>
        public bool IsInputActive => _playerActionMap != null && _playerActionMap.enabled;

        #endregion

        #region Events

        /// <summary>
        /// Fired when attack action is performed.
        /// </summary>
        public event Action OnAttackTriggered;

        /// <summary>
        /// Fired when interact action is performed.
        /// </summary>
        public event Action OnInteractTriggered;

        /// <summary>
        /// Fired when crouch toggle state changes.
        /// </summary>
        public event Action<bool> OnCrouchToggled;

        /// <summary>
        /// Fired when jump action is triggered.
        /// </summary>
        public event Action OnJumpTriggered;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeActionMap();
        }

        private void OnEnable()
        {
            if (_autoEnableInput)
            {
                EnableInput();
            }
        }

        private void OnDisable()
        {
            DisableInput();
        }

        private void Update()
        {
            PollContinuousInputs();
        }

        private void LateUpdate()
        {
            // Reset single-frame triggers if not consumed by properties
            _attackPressed = false;
            _interactPressed = false;
            _virtualAttackActive = false;
            _virtualInteractActive = false;

            // Clear virtual look delta each frame (look delta is per-frame)
            if (_hasVirtualLook)
            {
                _virtualLookInput = Vector2.zero;
                _hasVirtualLook = false;
            }
        }

        #endregion

        #region Initialization & Bindings

        private void InitializeActionMap()
        {
            if (_inputActions == null)
            {
                // Attempt to load default actions asset if unassigned
                _inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
            }

            if (_inputActions == null)
            {
                Debug.LogWarning($"[PlayerInputHandler] No InputActionAsset assigned on '{gameObject.name}'. Mobile virtual inputs will still function.");
                return;
            }

            _playerActionMap = _inputActions.FindActionMap(_actionMapName, false);
            if (_playerActionMap == null)
            {
                Debug.LogWarning($"[PlayerInputHandler] Action map '{_actionMapName}' not found in InputActionAsset.");
                return;
            }

            _moveAction = _playerActionMap.FindAction(_moveActionName, false);
            _lookAction = _playerActionMap.FindAction(_lookActionName, false);
            _sprintAction = _playerActionMap.FindAction(_sprintActionName, false);
            _jumpAction = _playerActionMap.FindAction(_jumpActionName, false);
            _crouchAction = _playerActionMap.FindAction(_crouchActionName, false);
            _attackAction = _playerActionMap.FindAction(_attackActionName, false);
            _interactAction = _playerActionMap.FindAction(_interactActionName, false);

            RegisterActionCallbacks();
        }

        private void RegisterActionCallbacks()
        {
            if (_jumpAction != null)
            {
                _jumpAction.performed += OnJumpPerformed;
            }

            if (_crouchAction != null)
            {
                _crouchAction.performed += OnCrouchPerformed;
            }

            if (_attackAction != null)
            {
                _attackAction.performed += OnAttackPerformed;
            }

            if (_interactAction != null)
            {
                _interactAction.performed += OnInteractPerformed;
            }
        }

        private void UnregisterActionCallbacks()
        {
            if (_jumpAction != null)
            {
                _jumpAction.performed -= OnJumpPerformed;
            }

            if (_crouchAction != null)
            {
                _crouchAction.performed -= OnCrouchPerformed;
            }

            if (_attackAction != null)
            {
                _attackAction.performed -= OnAttackPerformed;
            }

            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractPerformed;
            }
        }

        #endregion

        #region Polling & Callbacks

        private void PollContinuousInputs()
        {
            if (_playerActionMap == null || !_playerActionMap.enabled)
            {
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
                _sprintPressed = false;
                return;
            }

            if (_moveAction != null)
            {
                _moveInput = _moveAction.ReadValue<Vector2>();
            }

            if (_lookAction != null)
            {
                _lookInput = _lookAction.ReadValue<Vector2>();
            }

            if (_sprintAction != null)
            {
                _sprintPressed = _sprintAction.IsPressed();
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            TriggerJump();
        }

        private void OnCrouchPerformed(InputAction.CallbackContext context)
        {
            ToggleCrouch();
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            TriggerAttack();
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            TriggerInteract();
        }

        #endregion

        #region Public API — Map Control

        /// <summary>
        /// Enables the player action map.
        /// </summary>
        public void EnableInput()
        {
            if (_playerActionMap != null && !_playerActionMap.enabled)
            {
                _playerActionMap.Enable();
            }
        }

        /// <summary>
        /// Disables the player action map and clears current inputs.
        /// </summary>
        public void DisableInput()
        {
            if (_playerActionMap != null && _playerActionMap.enabled)
            {
                _playerActionMap.Disable();
            }

            ResetInputs();
        }

        /// <summary>
        /// Enables or disables the player action map.
        /// </summary>
        public void SetInputActive(bool active)
        {
            if (active)
            {
                EnableInput();
            }
            else
            {
                DisableInput();
            }
        }

        /// <summary>
        /// Clears all active input states and reset flags.
        /// </summary>
        public void ResetInputs()
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _sprintPressed = false;
            _jumpPressed = false;
            _attackPressed = false;
            _interactPressed = false;
            _virtualMoveInput = Vector2.zero;
            _virtualLookInput = Vector2.zero;
            _virtualSprintActive = false;
            _virtualAttackActive = false;
            _virtualInteractActive = false;
            _hasVirtualMove = false;
            _hasVirtualLook = false;
        }

        #endregion

        #region Public API — Mobile & Virtual Touch Controls

        /// <summary>
        /// Feeds movement input from mobile touch virtual joysticks.
        /// </summary>
        public void SetMoveInput(Vector2 movement)
        {
            _virtualMoveInput = Vector2.ClampMagnitude(movement, 1.0f);
            _hasVirtualMove = movement.sqrMagnitude > 0.001f;
        }

        /// <summary>
        /// Feeds camera look delta from mobile touch look surfaces.
        /// </summary>
        public void SetLookInput(Vector2 lookDelta)
        {
            _virtualLookInput = lookDelta;
            _hasVirtualLook = true;
        }

        /// <summary>
        /// Sets the sprint state directly from a touch sprint toggle/button.
        /// </summary>
        public void SetSprint(bool isSprinting)
        {
            _virtualSprintActive = isSprinting;
        }

        /// <summary>
        /// Triggers a jump request (consume-once).
        /// </summary>
        public void TriggerJump()
        {
            _jumpPressed = true;
            OnJumpTriggered?.Invoke();
        }

        /// <summary>
        /// Manually consumes the jump flag without waiting for property read.
        /// </summary>
        public void ConsumeJump()
        {
            _jumpPressed = false;
        }

        /// <summary>
        /// Toggles the crouch state.
        /// </summary>
        public void ToggleCrouch()
        {
            _crouchToggled = !_crouchToggled;
            OnCrouchToggled?.Invoke(_crouchToggled);
        }

        /// <summary>
        /// Explicitly sets the crouch state.
        /// </summary>
        public void SetCrouch(bool isCrouched)
        {
            if (_crouchToggled != isCrouched)
            {
                _crouchToggled = isCrouched;
                OnCrouchToggled?.Invoke(_crouchToggled);
            }
        }

        /// <summary>
        /// Triggers an attack event (one-frame).
        /// </summary>
        public void TriggerAttack()
        {
            _attackPressed = true;
            _virtualAttackActive = true;
            OnAttackTriggered?.Invoke();
        }

        /// <summary>
        /// Triggers an interaction event (one-frame).
        /// </summary>
        public void TriggerInteract()
        {
            _interactPressed = true;
            _virtualInteractActive = true;
            OnInteractTriggered?.Invoke();
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            UnregisterActionCallbacks();
        }

        #endregion
    }
}
