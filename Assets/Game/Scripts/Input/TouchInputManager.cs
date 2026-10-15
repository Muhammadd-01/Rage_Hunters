using System;
using System.Collections.Generic;
using UnityEngine;
using RageHunters.Core;

namespace RageHunters.Input
{
    /// <summary>
    /// Central coordinator for mobile UI touch controls.
    /// Aggregates virtual joystick, camera touch drag, and action touch buttons into a unified API
    /// consumed by PlayerInputHandler and gameplay systems.
    /// Handles visibility toggling, sensitivity configuration, and layout persistence.
    /// </summary>
    [DisallowMultipleComponent]
    public class TouchInputManager : Singleton<TouchInputManager>
    {
        #region Serialized Fields

        [Header("Hierarchy & Canvas")]
        [Tooltip("Root CanvasGroup containing all touch UI elements for fading/hiding during menus.")]
        [SerializeField] private CanvasGroup _touchControlsCanvasGroup;

        [Header("Primary Control Components")]
        [Tooltip("Virtual joystick for player movement.")]
        [SerializeField] private VirtualJoystick _movementJoystick;

        [Tooltip("Touch surface region for camera aiming and rotation.")]
        [SerializeField] private TouchCameraRegion _cameraRegion;

        [Header("Action Buttons")]
        [SerializeField] private TouchButton _sprintButton;
        [SerializeField] private TouchButton _jumpButton;
        [SerializeField] private TouchButton _crouchButton;
        [SerializeField] private TouchButton _attackButton;
        [SerializeField] private TouchButton _interactButton;
        [SerializeField] private TouchButton _reloadButton;
        [SerializeField] private TouchButton _inventoryButton;
        [SerializeField] private TouchButton _mapButton;
        [SerializeField] private TouchButton _pauseButton;

        [Header("Additional Buttons")]
        [Tooltip("Optional list of custom or ability touch buttons.")]
        [SerializeField] private List<TouchButton> _customButtons = new List<TouchButton>();

        [Header("Settings & Preferences")]
        [SerializeField] private bool _autoEnableOnMobile = true;
        [SerializeField] private bool _loadSettingsOnAwake = true;

        #endregion

        #region Private Fields

        private readonly Dictionary<TouchButtonType, TouchButton> _buttonMap = new();
        private readonly Dictionary<TouchButtonType, Vector2> _defaultButtonPositions = new();

        private float _sensitivityX = MobileInputActions.DefaultSensitivityX;
        private float _sensitivityY = MobileInputActions.DefaultSensitivityY;
        private bool _invertY = MobileInputActions.DefaultInvertY;
        private bool _invertX = MobileInputActions.DefaultInvertX;
        private float _joystickDeadZone = MobileInputActions.DefaultJoystickDeadZone;
        private JoystickType _joystickType = MobileInputActions.DefaultJoystickType;

        #endregion

        #region Input Properties (Consumed by PlayerInputHandler)

        /// <summary>Normalized 2D movement vector from the virtual joystick (-1 to 1).</summary>
        public Vector2 MoveInput => _movementJoystick != null ? _movementJoystick.Direction : Vector2.zero;

        /// <summary>Processed 2D look delta for camera rotation from the touch camera region.</summary>
        public Vector2 LookInput => _cameraRegion != null ? _cameraRegion.LookDelta : Vector2.zero;

        /// <summary>True if sprint is actively held down or toggled on.</summary>
        public bool SprintHeld => IsButtonHeld(TouchButtonType.Sprint);

        /// <summary>True on the frame jump was pressed.</summary>
        public bool JumpPressed => WasButtonPressed(TouchButtonType.Jump);

        /// <summary>True while jump button is held down.</summary>
        public bool JumpHeld => IsButtonHeld(TouchButtonType.Jump);

        /// <summary>True on the frame crouch was pressed.</summary>
        public bool CrouchPressed => WasButtonPressed(TouchButtonType.Crouch);

        /// <summary>True while crouch is held down or toggled on.</summary>
        public bool CrouchHeld => IsButtonHeld(TouchButtonType.Crouch);

        /// <summary>True on the frame attack was initiated.</summary>
        public bool AttackPressed => WasButtonPressed(TouchButtonType.Attack);

        /// <summary>True while attack button is held down.</summary>
        public bool AttackHeld => IsButtonHeld(TouchButtonType.Attack);

        /// <summary>True on the frame interact was pressed.</summary>
        public bool InteractPressed => WasButtonPressed(TouchButtonType.Interact);

        /// <summary>True on the frame reload was pressed.</summary>
        public bool ReloadPressed => WasButtonPressed(TouchButtonType.Reload);

        /// <summary>True on the frame inventory was pressed.</summary>
        public bool InventoryPressed => WasButtonPressed(TouchButtonType.Inventory);

        /// <summary>True on the frame map was pressed.</summary>
        public bool MapPressed => WasButtonPressed(TouchButtonType.Map);

        /// <summary>True on the frame pause was pressed.</summary>
        public bool PausePressed => WasButtonPressed(TouchButtonType.Pause);

        #endregion

        #region Configuration Properties

        public float SensitivityX
        {
            get => _sensitivityX;
            set
            {
                _sensitivityX = value;
                if (_cameraRegion != null) _cameraRegion.SensitivityX = value;
            }
        }

        public float SensitivityY
        {
            get => _sensitivityY;
            set
            {
                _sensitivityY = value;
                if (_cameraRegion != null) _cameraRegion.SensitivityY = value;
            }
        }

        public bool InvertY
        {
            get => _invertY;
            set
            {
                _invertY = value;
                if (_cameraRegion != null) _cameraRegion.InvertY = value;
            }
        }

        public bool InvertX
        {
            get => _invertX;
            set
            {
                _invertX = value;
                if (_cameraRegion != null) _cameraRegion.InvertX = value;
            }
        }

        public float JoystickDeadZone
        {
            get => _joystickDeadZone;
            set
            {
                _joystickDeadZone = value;
                if (_movementJoystick != null) _movementJoystick.DeadZone = value;
            }
        }

        public JoystickType JoystickMode
        {
            get => _joystickType;
            set
            {
                _joystickType = value;
                if (_movementJoystick != null) _movementJoystick.Mode = value;
            }
        }

        #endregion

        #region Unity Lifecycle & Initialization

        protected override void Awake()
        {
            base.Awake();

            RegisterButtons();
            CacheDefaultLayout();

            if (_loadSettingsOnAwake)
            {
                LoadSettings();
            }

            if (_autoEnableOnMobile)
            {
                bool shouldEnable = MobileInputActions.ShouldEnableTouchControls();
                SetTouchControlsVisible(shouldEnable);
            }
        }

        #endregion

        #region Button Registration & Querying

        private void RegisterButtons()
        {
            _buttonMap.Clear();

            RegisterButton(_sprintButton, TouchButtonType.Sprint);
            RegisterButton(_jumpButton, TouchButtonType.Jump);
            RegisterButton(_crouchButton, TouchButtonType.Crouch);
            RegisterButton(_attackButton, TouchButtonType.Attack);
            RegisterButton(_interactButton, TouchButtonType.Interact);
            RegisterButton(_reloadButton, TouchButtonType.Reload);
            RegisterButton(_inventoryButton, TouchButtonType.Inventory);
            RegisterButton(_mapButton, TouchButtonType.Map);
            RegisterButton(_pauseButton, TouchButtonType.Pause);

            foreach (var button in _customButtons)
            {
                if (button != null)
                {
                    _buttonMap[button.ButtonType] = button;
                }
            }
        }

        private void RegisterButton(TouchButton button, TouchButtonType fallbackType)
        {
            if (button == null) return;

            TouchButtonType type = button.ButtonType != TouchButtonType.Custom ? button.ButtonType : fallbackType;
            _buttonMap[type] = button;
        }

        /// <summary>
        /// Gets a touch button instance by type.
        /// </summary>
        public TouchButton GetButton(TouchButtonType type)
        {
            _buttonMap.TryGetValue(type, out TouchButton button);
            return button;
        }

        /// <summary>
        /// Checks if a button is currently held or toggled active.
        /// </summary>
        public bool IsButtonHeld(TouchButtonType type)
        {
            return _buttonMap.TryGetValue(type, out TouchButton btn) && btn.IsPressed;
        }

        /// <summary>
        /// Checks if a button was pressed during the current frame.
        /// </summary>
        public bool WasButtonPressed(TouchButtonType type)
        {
            return _buttonMap.TryGetValue(type, out TouchButton btn) && btn.WasPressedThisFrame;
        }

        /// <summary>
        /// Checks if a button was released during the current frame.
        /// </summary>
        public bool WasButtonReleased(TouchButtonType type)
        {
            return _buttonMap.TryGetValue(type, out TouchButton btn) && btn.WasReleasedThisFrame;
        }

        #endregion

        #region Visibility & Control State

        /// <summary>
        /// Shows or hides all touch controls (useful when entering menus, inventory, or dialogue).
        /// </summary>
        /// <param name="visible">True to display controls, false to hide and disable interaction.</param>
        public void SetTouchControlsVisible(bool visible)
        {
            if (_touchControlsCanvasGroup == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            _touchControlsCanvasGroup.alpha = visible ? 1f : 0f;
            _touchControlsCanvasGroup.interactable = visible;
            _touchControlsCanvasGroup.blocksRaycasts = visible;

            if (!visible)
            {
                ResetAllControls();
            }
        }

        /// <summary>
        /// Resets all touch inputs (joystick, camera delta, and pressed buttons).
        /// </summary>
        public void ResetAllControls()
        {
            if (_movementJoystick != null)
            {
                _movementJoystick.ResetJoystick();
            }

            if (_cameraRegion != null)
            {
                _cameraRegion.ResetRegion();
            }

            foreach (var kvp in _buttonMap)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.ResetState();
                }
            }
        }

        #endregion

        #region Settings Persistence

        /// <summary>
        /// Loads sensitivity and control settings from PlayerPrefs.
        /// </summary>
        public void LoadSettings()
        {
            _sensitivityX = PlayerPrefs.GetFloat(MobileInputActions.PrefKeySensitivityX, MobileInputActions.DefaultSensitivityX);
            _sensitivityY = PlayerPrefs.GetFloat(MobileInputActions.PrefKeySensitivityY, MobileInputActions.DefaultSensitivityY);
            _invertY = PlayerPrefs.GetInt(MobileInputActions.PrefKeyInvertY, MobileInputActions.DefaultInvertY ? 1 : 0) == 1;
            _invertX = PlayerPrefs.GetInt(MobileInputActions.PrefKeyInvertX, MobileInputActions.DefaultInvertX ? 1 : 0) == 1;
            _joystickDeadZone = PlayerPrefs.GetFloat(MobileInputActions.PrefKeyDeadZone, MobileInputActions.DefaultJoystickDeadZone);
            _joystickType = (JoystickType)PlayerPrefs.GetInt(MobileInputActions.PrefKeyJoystickType, (int)MobileInputActions.DefaultJoystickType);

            ApplySettingsToComponents();
            LoadLayoutPositions();
        }

        /// <summary>
        /// Saves current sensitivity and control settings to PlayerPrefs.
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(MobileInputActions.PrefKeySensitivityX, _sensitivityX);
            PlayerPrefs.SetFloat(MobileInputActions.PrefKeySensitivityY, _sensitivityY);
            PlayerPrefs.SetInt(MobileInputActions.PrefKeyInvertY, _invertY ? 1 : 0);
            PlayerPrefs.SetInt(MobileInputActions.PrefKeyInvertX, _invertX ? 1 : 0);
            PlayerPrefs.SetFloat(MobileInputActions.PrefKeyDeadZone, _joystickDeadZone);
            PlayerPrefs.SetInt(MobileInputActions.PrefKeyJoystickType, (int)_joystickType);
            PlayerPrefs.Save();
        }

        private void ApplySettingsToComponents()
        {
            if (_cameraRegion != null)
            {
                _cameraRegion.SensitivityX = _sensitivityX;
                _cameraRegion.SensitivityY = _sensitivityY;
                _cameraRegion.InvertY = _invertY;
                _cameraRegion.InvertX = _invertX;
            }

            if (_movementJoystick != null)
            {
                _movementJoystick.DeadZone = _joystickDeadZone;
                _movementJoystick.Mode = _joystickType;
            }
        }

        #endregion

        #region Layout Configuration

        private void CacheDefaultLayout()
        {
            _defaultButtonPositions.Clear();
            foreach (var kvp in _buttonMap)
            {
                if (kvp.Value != null && kvp.Value.transform is RectTransform rect)
                {
                    _defaultButtonPositions[kvp.Key] = rect.anchoredPosition;
                }
            }
        }

        /// <summary>
        /// Saves customized positions for all buttons to PlayerPrefs.
        /// </summary>
        public void SaveLayoutPositions()
        {
            foreach (var kvp in _buttonMap)
            {
                if (kvp.Value != null && kvp.Value.transform is RectTransform rect)
                {
                    MobileInputActions.SaveButtonPosition(kvp.Key, rect.anchoredPosition);
                }
            }
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Restores saved button positions from PlayerPrefs.
        /// </summary>
        public void LoadLayoutPositions()
        {
            foreach (var kvp in _buttonMap)
            {
                if (kvp.Value != null && kvp.Value.transform is RectTransform rect)
                {
                    if (MobileInputActions.TryLoadButtonPosition(kvp.Key, out Vector2 savedPos))
                    {
                        rect.anchoredPosition = savedPos;
                    }
                }
            }
        }

        /// <summary>
        /// Resets all touch button positions to their initial authored positions.
        /// </summary>
        public void ResetLayoutToDefault()
        {
            MobileInputActions.ClearCustomLayout();

            foreach (var kvp in _buttonMap)
            {
                if (kvp.Value != null && kvp.Value.transform is RectTransform rect)
                {
                    if (_defaultButtonPositions.TryGetValue(kvp.Key, out Vector2 defaultPos))
                    {
                        rect.anchoredPosition = defaultPos;
                    }
                }
            }
        }

        /// <summary>
        /// Sets a custom anchored position for a specific button.
        /// </summary>
        public void SetButtonPosition(TouchButtonType type, Vector2 anchoredPosition)
        {
            if (_buttonMap.TryGetValue(type, out TouchButton button) && button != null && button.transform is RectTransform rect)
            {
                rect.anchoredPosition = anchoredPosition;
                MobileInputActions.SaveButtonPosition(type, anchoredPosition);
            }
        }

        #endregion
    }
}
