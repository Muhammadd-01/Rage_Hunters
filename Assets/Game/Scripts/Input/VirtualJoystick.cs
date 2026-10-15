using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RageHunters.Input
{
    /// <summary>
    /// Operating modes for the virtual joystick.
    /// </summary>
    public enum JoystickType
    {
        /// <summary>Joystick remains fixed at its authored anchored position.</summary>
        Fixed,
        /// <summary>Joystick appears wherever the touch begins inside the activation area.</summary>
        Floating,
        /// <summary>Joystick moves dynamically to follow the finger when dragged past the outer radius.</summary>
        DynamicFollow
    }

    /// <summary>
    /// UI-based virtual joystick designed for mobile movement controls.
    /// Works seamlessly with Canvas Scaler (Screen Space - Overlay and Screen Space - Camera),
    /// clamps handle inside the background radius, supports dead zones, fading, and multi-touch tracking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        #region Serialized Fields

        [Header("Hierarchy References")]
        [Tooltip("The background circle RectTransform that bounds the handle.")]
        [SerializeField] private RectTransform _backgroundRect;

        [Tooltip("The movable handle/knob RectTransform.")]
        [SerializeField] private RectTransform _handleRect;

        [Tooltip("Optional parent canvas. If null, automatically located in parents.")]
        [SerializeField] private Canvas _canvas;

        [Header("Joystick Configuration")]
        [Tooltip("Behavior mode: Fixed stays in place, Floating relocates to touch, DynamicFollow trails touch.")]
        [SerializeField] private JoystickType _joystickType = JoystickType.Fixed;

        [Tooltip("Maximum movement radius for the handle in local pixels. If 0, uses half of background width.")]
        [SerializeField] private float _handleRange = 100f;

        [Tooltip("Normalized dead zone (0 to 1). Input magnitude below this value returns zero.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _deadZone = 0.1f;

        [Tooltip("If true, outputs an exact unit vector when outside dead zone. If false, scales linearly from 0 to 1.")]
        [SerializeField] private bool _normalizeOutput = false;

        [Header("Visual Feedback & Fading")]
        [Tooltip("CanvasGroup used for fading the joystick. If null, added automatically.")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Tooltip("Alpha value when idle/not touched.")]
        [Range(0f, 1f)]
        [SerializeField] private float _idleAlpha = 0.4f;

        [Tooltip("Alpha value while touched and dragging.")]
        [Range(0f, 1f)]
        [SerializeField] private float _activeAlpha = 1.0f;

        [Tooltip("Duration in seconds for fading between idle and active states.")]
        [SerializeField] private float _fadeDuration = 0.15f;

        [Header("Appearance & Customization")]
        [Tooltip("Whether to snap handle back to center instantly or smoothly interpolate on release.")]
        [SerializeField] private bool _snapToCenterOnRelease = true;

        [SerializeField] private float _returnSpeed = 25f;

        #endregion

        #region Private Fields

        private RectTransform _baseRectTransform;
        private UnityEngine.Camera _uiCamera;
        private Vector2 _defaultBackgroundPosition;
        private Vector2 _inputDirection = Vector2.zero;
        private Vector2 _rawInput = Vector2.zero;
        private int _activePointerId = -1;
        private bool _isDragging = false;
        private float _targetAlpha;
        private float _currentFadeVelocity;

        #endregion

        #region Properties

        /// <summary>
        /// Current normalized direction vector (-1 to 1 on X and Y).
        /// </summary>
        public Vector2 Direction => _inputDirection;

        /// <summary>
        /// Horizontal axis component of joystick movement (-1 to 1).
        /// </summary>
        public float Horizontal => _inputDirection.x;

        /// <summary>
        /// Vertical axis component of joystick movement (-1 to 1).
        /// </summary>
        public float Vertical => _inputDirection.y;

        /// <summary>
        /// Whether the joystick is currently pressed and actively controlled.
        /// </summary>
        public bool IsDragging => _isDragging;

        /// <summary>
        /// The joystick operating mode.
        /// </summary>
        public JoystickType Mode
        {
            get => _joystickType;
            set => _joystickType = value;
        }

        /// <summary>
        /// Active dead zone value.
        /// </summary>
        public float DeadZone
        {
            get => _deadZone;
            set => _deadZone = Mathf.Clamp(value, 0f, 0.5f);
        }

        public void SetHandle(RectTransform handle) => _handleRect = handle;

        #endregion

        #region Events

        /// <summary>Invoked whenever the joystick direction changes.</summary>
        public event Action<Vector2> OnDirectionChanged;

        /// <summary>Invoked when the user presses down on the joystick.</summary>
        public event Action OnJoystickDown;

        /// <summary>Invoked when the user releases the joystick.</summary>
        public event Action OnJoystickUp;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _baseRectTransform = GetComponent<RectTransform>();

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_backgroundRect == null)
            {
                _backgroundRect = _baseRectTransform;
            }

            if (_handleRect == null && transform.childCount > 0)
            {
                _handleRect = transform.GetChild(0) as RectTransform;
            }

            _defaultBackgroundPosition = _backgroundRect.anchoredPosition;

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            _targetAlpha = _idleAlpha;
            _canvasGroup.alpha = _idleAlpha;

            UpdateCameraReference();
        }

        private void OnEnable()
        {
            ResetJoystick();
        }

        private void OnDisable()
        {
            ResetJoystick();
        }

        private void Update()
        {
            UpdateFade();

            if (!_isDragging && !_snapToCenterOnRelease && _handleRect != null)
            {
                _handleRect.anchoredPosition = Vector2.Lerp(
                    _handleRect.anchoredPosition,
                    Vector2.zero,
                    Time.unscaledDeltaTime * _returnSpeed
                );
            }
        }

        #endregion

        #region Pointer Event Handlers

        /// <summary>
        /// Handles initial touch down on the joystick region.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isDragging)
            {
                return; // Already tracked by another finger
            }

            _activePointerId = eventData.pointerId;
            _isDragging = true;
            _targetAlpha = _activeAlpha;

            UpdateCameraReference();

            if (_joystickType == JoystickType.Floating || _joystickType == JoystickType.DynamicFollow)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _baseRectTransform,
                    eventData.position,
                    _uiCamera,
                    out Vector2 localPoint))
                {
                    _backgroundRect.anchoredPosition = localPoint;
                }
            }

            OnDrag(eventData);
            OnJoystickDown?.Invoke();
        }

        /// <summary>
        /// Tracks movement while dragging the joystick handle.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || eventData.pointerId != _activePointerId)
            {
                return;
            }

            UpdateCameraReference();

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _backgroundRect,
                eventData.position,
                _uiCamera,
                out Vector2 positionInBg))
            {
                return;
            }

            float maxRadius = GetRadius();

            // Dynamic follow: move background toward pointer if dragged past outer bounds
            if (_joystickType == JoystickType.DynamicFollow && positionInBg.magnitude > maxRadius)
            {
                Vector2 excess = positionInBg - positionInBg.normalized * maxRadius;
                _backgroundRect.anchoredPosition += excess;
                positionInBg = positionInBg.normalized * maxRadius;
            }

            // Clamp handle offset within background radius
            Vector2 clampedPosition = Vector2.ClampMagnitude(positionInBg, maxRadius);

            if (_handleRect != null)
            {
                _handleRect.anchoredPosition = clampedPosition;
            }

            // Calculate normalized input direction
            _rawInput = clampedPosition / maxRadius;
            CalculateDirection(_rawInput);
        }

        /// <summary>
        /// Handles finger release, resetting handle and restoring idle fade.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId)
            {
                return;
            }

            ResetJoystick();
            OnJoystickUp?.Invoke();
        }

        #endregion

        #region Internal Methods

        private void CalculateDirection(Vector2 raw)
        {
            float magnitude = raw.magnitude;

            if (magnitude < _deadZone)
            {
                _inputDirection = Vector2.zero;
            }
            else
            {
                if (_normalizeOutput)
                {
                    _inputDirection = raw.normalized;
                }
                else
                {
                    // Smoothly remap magnitude from deadZone..1 to 0..1
                    float normalizedMagnitude = (magnitude - _deadZone) / (1f - _deadZone);
                    _inputDirection = raw.normalized * Mathf.Clamp01(normalizedMagnitude);
                }
            }

            OnDirectionChanged?.Invoke(_inputDirection);
        }

        private float GetRadius()
        {
            if (_handleRange > 0f)
            {
                return _handleRange;
            }

            if (_backgroundRect != null)
            {
                return _backgroundRect.sizeDelta.x * 0.5f;
            }

            return 100f;
        }

        private void UpdateCameraReference()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCamera = _canvas.worldCamera;
            }
            else
            {
                _uiCamera = null;
            }
        }

        private void UpdateFade()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            if (Mathf.Abs(_canvasGroup.alpha - _targetAlpha) > 0.005f)
            {
                _canvasGroup.alpha = Mathf.SmoothDamp(
                    _canvasGroup.alpha,
                    _targetAlpha,
                    ref _currentFadeVelocity,
                    _fadeDuration,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime
                );
            }
            else
            {
                _canvasGroup.alpha = _targetAlpha;
            }
        }

        /// <summary>
        /// Resets the joystick state, handle position, and input vectors to zero.
        /// </summary>
        public void ResetJoystick()
        {
            _isDragging = false;
            _activePointerId = -1;
            _inputDirection = Vector2.zero;
            _rawInput = Vector2.zero;
            _targetAlpha = _idleAlpha;

            if (_handleRect != null)
            {
                _handleRect.anchoredPosition = Vector2.zero;
            }

            if (_backgroundRect != null && (_joystickType == JoystickType.Floating || _joystickType == JoystickType.DynamicFollow))
            {
                _backgroundRect.anchoredPosition = _defaultBackgroundPosition;
            }

            OnDirectionChanged?.Invoke(Vector2.zero);
        }

        #endregion
    }
}
