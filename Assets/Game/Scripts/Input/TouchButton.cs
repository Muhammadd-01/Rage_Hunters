using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace RageHunters.Input
{
    /// <summary>
    /// Supported behavior types for touch action buttons.
    /// </summary>
    public enum TouchButtonBehavior
    {
        /// <summary>Active as long as the finger is held down on the button.</summary>
        Hold,
        /// <summary>Each tap flips the active state between ON and OFF.</summary>
        Toggle
    }

    /// <summary>
    /// Identifiers for common mobile action buttons in RAGE HUNTERS.
    /// </summary>
    public enum TouchButtonType
    {
        Custom = 0,
        Sprint,
        Jump,
        Crouch,
        Attack,
        Interact,
        Reload,
        Inventory,
        Map,
        Pause,
        Ability1,
        Ability2
    }

    /// <summary>
    /// Reusable UI touch button component tailored for mobile gameplay controls.
    /// Supports hold/toggle modes, visual feedback (press scale and tint), cooldown timers,
    /// and frame-safe press/release detection.
    /// </summary>
    [DisallowMultipleComponent]
    [SelectionBase]
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        #region Serialized Fields

        [Header("Button Identity")]
        [Tooltip("The logical button type / action mapped to this touch button.")]
        [SerializeField] private TouchButtonType _buttonType = TouchButtonType.Custom;

        [Tooltip("Hold stays active while held; Toggle flips state on each tap.")]
        [SerializeField] private TouchButtonBehavior _behavior = TouchButtonBehavior.Hold;

        [Header("Visual References")]
        [Tooltip("Primary graphic target for color changes. If null, will attempt to find Image on this GameObject.")]
        [SerializeField] private Graphic _targetGraphic;

        [Tooltip("Transform to scale on press. If null, defaults to this transform.")]
        [SerializeField] private Transform _visualRoot;

        [Tooltip("Optional icon Image component.")]
        [SerializeField] private Image _iconImage;

        [Tooltip("Optional label text component for button prompt.")]
        [SerializeField] private TMP_Text _labelText;

        [Tooltip("Optional radial Image component to display cooldown progression (Filled type).")]
        [SerializeField] private Image _cooldownRadialImage;

        [Header("Colors & Animation")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color _toggledColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [Tooltip("Scale multiplier applied when button is pressed.")]
        [Range(0.7f, 1.3f)]
        [SerializeField] private float _pressedScale = 0.92f;

        [Tooltip("Interpolation speed for scaling.")]
        [SerializeField] private float _scaleSpeed = 20f;

        [Header("Interactability & Cooldown")]
        [SerializeField] private bool _interactable = true;

        [Tooltip("Optional base cooldown in seconds. If 0, button has no inherent cooldown.")]
        [SerializeField] private float _defaultCooldown = 0f;

        [Header("Events")]
        [SerializeField] private UnityEvent _onPressed = new UnityEvent();
        [SerializeField] private UnityEvent _onReleased = new UnityEvent();
        [SerializeField] private UnityEvent<bool> _onToggled = new UnityEvent<bool>();

        #endregion

        #region Private Fields

        private bool _isPressed = false;
        private bool _isToggled = false;
        private bool _wasPressedThisFrame = false;
        private bool _wasReleasedThisFrame = false;
        private int _activePointerId = -1;
        private float _remainingCooldown = 0f;
        private float _currentCooldownDuration = 0f;
        private Vector3 _originalScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;

        #endregion

        #region Properties

        /// <summary>Identified action type for this button.</summary>
        public TouchButtonType ButtonType => _buttonType;

        /// <summary>Active behavior mode (Hold or Toggle).</summary>
        public TouchButtonBehavior Behavior
        {
            get => _behavior;
            set => _behavior = value;
        }

        /// <summary>
        /// True if the button is currently held down (Hold mode) or toggled on (Toggle mode).
        /// </summary>
        public bool IsPressed => _behavior == TouchButtonBehavior.Toggle ? _isToggled : _isPressed;

        /// <summary>True only on the frame the button was pressed.</summary>
        public bool WasPressedThisFrame => _wasPressedThisFrame;

        /// <summary>True only on the frame the button was released.</summary>
        public bool WasReleasedThisFrame => _wasReleasedThisFrame;

        /// <summary>Whether the button is currently in a cooldown state.</summary>
        public bool IsOnCooldown => _remainingCooldown > 0f;

        /// <summary>Normalized remaining cooldown (1.0 = just started, 0.0 = ready).</summary>
        public float CooldownNormalized => _currentCooldownDuration > 0f ? Mathf.Clamp01(_remainingCooldown / _currentCooldownDuration) : 0f;

        /// <summary>Remaining cooldown time in seconds.</summary>
        public float RemainingCooldown => Mathf.Max(0f, _remainingCooldown);

        /// <summary>Whether this button can receive touch input.</summary>
        public bool Interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                UpdateVisualState();
            }
        }

        public UnityEvent OnPressed => _onPressed;
        public UnityEvent OnReleased => _onReleased;
        public UnityEvent<bool> OnToggled => _onToggled;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_visualRoot == null)
            {
                _visualRoot = transform;
            }

            _originalScale = _visualRoot.localScale;
            _targetScale = _originalScale;

            if (_targetGraphic == null)
            {
                _targetGraphic = GetComponent<Graphic>();
            }

            UpdateVisualState();
        }

        private void OnEnable()
        {
            ResetState();
        }

        private void OnDisable()
        {
            ResetState();
        }

        private void Update()
        {
            UpdateCooldown();
            UpdateScale();
        }

        private void LateUpdate()
        {
            // Frame flags are reset at the end of each frame
            _wasPressedThisFrame = false;
            _wasReleasedThisFrame = false;
        }

        #endregion

        #region Pointer Event Handlers

        /// <summary>
        /// Responds to touch pointer down.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_interactable || IsOnCooldown || _isPressed)
            {
                return;
            }

            _activePointerId = eventData.pointerId;
            _isPressed = true;
            _wasPressedThisFrame = true;
            _targetScale = _originalScale * _pressedScale;

            if (_behavior == TouchButtonBehavior.Toggle)
            {
                _isToggled = !_isToggled;
                _onToggled?.Invoke(_isToggled);
            }

            _onPressed?.Invoke();
            UpdateVisualState();

            if (_defaultCooldown > 0f)
            {
                StartCooldown(_defaultCooldown);
            }
        }

        /// <summary>
        /// Responds to touch pointer up.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId && _activePointerId != -1)
            {
                return;
            }

            _activePointerId = -1;
            _isPressed = false;
            _wasReleasedThisFrame = true;
            _targetScale = _originalScale;

            _onReleased?.Invoke();
            UpdateVisualState();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initiates an action cooldown period. While on cooldown, the button cannot be pressed.
        /// </summary>
        /// <param name="duration">Cooldown duration in seconds.</param>
        public void StartCooldown(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            _currentCooldownDuration = duration;
            _remainingCooldown = duration;

            if (_cooldownRadialImage != null)
            {
                _cooldownRadialImage.gameObject.SetActive(true);
                _cooldownRadialImage.fillAmount = 1f;
            }

            UpdateVisualState();
        }

        /// <summary>
        /// Resets cooldown immediately.
        /// </summary>
        public void ResetCooldown()
        {
            _remainingCooldown = 0f;
            _currentCooldownDuration = 0f;

            if (_cooldownRadialImage != null)
            {
                _cooldownRadialImage.fillAmount = 0f;
                _cooldownRadialImage.gameObject.SetActive(false);
            }

            UpdateVisualState();
        }

        /// <summary>
        /// Configures the button icon image.
        /// </summary>
        public void SetIcon(Sprite icon)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.gameObject.SetActive(icon != null);
            }
        }

        /// <summary>
        /// Configures the button label text.
        /// </summary>
        public void SetLabel(string label)
        {
            if (_labelText != null)
            {
                _labelText.text = label;
                _labelText.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
        }

        /// <summary>
        /// Manually sets the toggle state.
        /// </summary>
        public void SetToggled(bool toggled)
        {
            if (_behavior != TouchButtonBehavior.Toggle || _isToggled == toggled)
            {
                return;
            }

            _isToggled = toggled;
            _onToggled?.Invoke(_isToggled);
            UpdateVisualState();
        }

        /// <summary>
        /// Forces button release and restores idle state.
        /// </summary>
        public void ResetState()
        {
            _isPressed = false;
            _activePointerId = -1;
            _targetScale = _originalScale;

            if (_visualRoot != null)
            {
                _visualRoot.localScale = _originalScale;
            }

            UpdateVisualState();
        }

        #endregion

        #region Internal Visuals

        private void UpdateCooldown()
        {
            if (_remainingCooldown <= 0f)
            {
                return;
            }

            _remainingCooldown -= Time.unscaledDeltaTime;

            if (_cooldownRadialImage != null)
            {
                _cooldownRadialImage.fillAmount = CooldownNormalized;
            }

            if (_remainingCooldown <= 0f)
            {
                _remainingCooldown = 0f;
                _currentCooldownDuration = 0f;

                if (_cooldownRadialImage != null)
                {
                    _cooldownRadialImage.gameObject.SetActive(false);
                }

                UpdateVisualState();
            }
        }

        private void UpdateScale()
        {
            if (_visualRoot == null)
            {
                return;
            }

            if (Vector3.Distance(_visualRoot.localScale, _targetScale) > 0.001f)
            {
                _visualRoot.localScale = Vector3.Lerp(
                    _visualRoot.localScale,
                    _targetScale,
                    Time.unscaledDeltaTime * _scaleSpeed
                );
            }
            else
            {
                _visualRoot.localScale = _targetScale;
            }
        }

        private void UpdateVisualState()
        {
            if (_targetGraphic == null)
            {
                return;
            }

            if (!_interactable)
            {
                _targetGraphic.color = _disabledColor;
                return;
            }

            if (_behavior == TouchButtonBehavior.Toggle && _isToggled)
            {
                _targetGraphic.color = _toggledColor;
                return;
            }

            if (_isPressed)
            {
                _targetGraphic.color = _pressedColor;
                return;
            }

            _targetGraphic.color = _normalColor;
        }

        #endregion
    }
}
