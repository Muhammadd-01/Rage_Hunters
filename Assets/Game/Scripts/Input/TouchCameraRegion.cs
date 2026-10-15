using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RageHunters.Input
{
    /// <summary>
    /// Invisible UI touch surface designed for camera look/aim rotation on mobile devices.
    /// Tracks pointer touch delta across frames, handles sensitivity scaling, inversion,
    /// smoothing, and multi-touch isolation so it does not conflict with joysticks or buttons.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class TouchCameraRegion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        #region Serialized Fields

        [Header("Sensitivity & Look Feel")]
        [Tooltip("Horizontal sensitivity multiplier for camera rotation.")]
        [SerializeField] private float _sensitivityX = 0.12f;

        [Tooltip("Vertical sensitivity multiplier for camera rotation.")]
        [SerializeField] private float _sensitivityY = 0.12f;

        [Tooltip("Invert vertical look axis.")]
        [SerializeField] private bool _invertY = false;

        [Tooltip("Invert horizontal look axis.")]
        [SerializeField] private bool _invertX = false;

        [Tooltip("Smoothing factor (0 = raw delta, higher values smooth jerky touch frames).")]
        [Range(0f, 0.95f)]
        [SerializeField] private float _smoothing = 0.1f;

        [Tooltip("Maximum allowed delta per frame to suppress sudden touch jump artifacts.")]
        [SerializeField] private float _maxDeltaClamp = 100f;

        [Header("Screen Area Filter (Optional)")]
        [Tooltip("If true, only touches beginning inside the normalized screen bounds are accepted.")]
        [SerializeField] private bool _useScreenBoundsFilter = false;

        [Tooltip("Minimum normalized screen coordinates (X: 0..1, Y: 0..1). Default covers right half of screen.")]
        [SerializeField] private Vector2 _minScreenBounds = new Vector2(0.4f, 0f);

        [Tooltip("Maximum normalized screen coordinates (X: 0..1, Y: 0..1).")]
        [SerializeField] private Vector2 _maxScreenBounds = new Vector2(1f, 1f);

        #endregion

        #region Private Fields

        private Vector2 _accumulatedDelta = Vector2.zero;
        private Vector2 _smoothedDelta = Vector2.zero;
        private Vector2 _currentFrameDelta = Vector2.zero;
        private Vector2 _previousPointerPosition = Vector2.zero;
        private int _activePointerId = -1;
        private bool _isTouching = false;

        #endregion

        #region Properties

        /// <summary>
        /// Output look delta for the current frame, scaled by sensitivity and axes settings.
        /// Automatically cleared at the end of each frame.
        /// </summary>
        public Vector2 LookDelta => _currentFrameDelta;

        /// <summary>
        /// Whether the touch region is currently tracked by an active finger.
        /// </summary>
        public bool IsTouching => _isTouching;

        /// <summary>
        /// Active horizontal camera sensitivity.
        /// </summary>
        public float SensitivityX
        {
            get => _sensitivityX;
            set => _sensitivityX = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// Active vertical camera sensitivity.
        /// </summary>
        public float SensitivityY
        {
            get => _sensitivityY;
            set => _sensitivityY = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// Inversion state for vertical look.
        /// </summary>
        public bool InvertY
        {
            get => _invertY;
            set => _invertY = value;
        }

        /// <summary>
        /// Inversion state for horizontal look.
        /// </summary>
        public bool InvertX
        {
            get => _invertX;
            set => _invertX = value;
        }

        #endregion

        #region Events

        /// <summary>Invoked every frame look delta is non-zero.</summary>
        public event Action<Vector2> OnLookDeltaChanged;

        /// <summary>Invoked when the user begins touching the camera region.</summary>
        public event Action OnTouchStarted;

        /// <summary>Invoked when the user lifts finger from the camera region.</summary>
        public event Action OnTouchEnded;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            EnsureRaycastTarget();
        }

        private void OnEnable()
        {
            ResetRegion();
        }

        private void OnDisable()
        {
            ResetRegion();
        }

        private void Update()
        {
            if (_isTouching)
            {
                // Apply delta clamp to prevent huge jumps (e.g. finger teleportation or frame drops)
                Vector2 clampedDelta = Vector2.ClampMagnitude(_accumulatedDelta, _maxDeltaClamp);

                // Apply axis inversion and sensitivity
                float x = clampedDelta.x * (_invertX ? -1f : 1f) * _sensitivityX;
                float y = clampedDelta.y * (_invertY ? -1f : 1f) * _sensitivityY;
                Vector2 processedDelta = new Vector2(x, y);

                // Apply optional smoothing
                if (_smoothing > 0f)
                {
                    _smoothedDelta = Vector2.Lerp(_smoothedDelta, processedDelta, 1f - _smoothing);
                    _currentFrameDelta = _smoothedDelta;
                }
                else
                {
                    _currentFrameDelta = processedDelta;
                }

                if (_currentFrameDelta.sqrMagnitude > 0.0001f)
                {
                    OnLookDeltaChanged?.Invoke(_currentFrameDelta);
                }

                _accumulatedDelta = Vector2.zero;
            }
            else
            {
                _smoothedDelta = Vector2.zero;
                _currentFrameDelta = Vector2.zero;
            }
        }

        private void LateUpdate()
        {
            // Reset frame delta if no touch drag occurred this frame
            if (!_isTouching)
            {
                _currentFrameDelta = Vector2.zero;
            }
        }

        #endregion

        #region Pointer Event Handlers

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isTouching)
            {
                return; // Region is already bound to another finger
            }

            if (_useScreenBoundsFilter && !IsWithinScreenBounds(eventData.position))
            {
                return;
            }

            _activePointerId = eventData.pointerId;
            _isTouching = true;
            _previousPointerPosition = eventData.position;
            _accumulatedDelta = Vector2.zero;
            _smoothedDelta = Vector2.zero;
            _currentFrameDelta = Vector2.zero;

            OnTouchStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isTouching || eventData.pointerId != _activePointerId)
            {
                return;
            }

            Vector2 currentPosition = eventData.position;
            Vector2 delta = currentPosition - _previousPointerPosition;
            _accumulatedDelta += delta;
            _previousPointerPosition = currentPosition;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId)
            {
                return;
            }

            ResetRegion();
            OnTouchEnded?.Invoke();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reads and resets current look delta in one call (convenient for polling loops).
        /// </summary>
        public Vector2 ConsumeLookDelta()
        {
            Vector2 delta = _currentFrameDelta;
            _currentFrameDelta = Vector2.zero;
            return delta;
        }

        /// <summary>
        /// Resets the touch tracking state.
        /// </summary>
        public void ResetRegion()
        {
            _isTouching = false;
            _activePointerId = -1;
            _accumulatedDelta = Vector2.zero;
            _smoothedDelta = Vector2.zero;
            _currentFrameDelta = Vector2.zero;
            _previousPointerPosition = Vector2.zero;
        }

        #endregion

        #region Helpers

        private bool IsWithinScreenBounds(Vector2 screenPos)
        {
            float normX = screenPos.x / Screen.width;
            float normY = screenPos.y / Screen.height;

            return normX >= _minScreenBounds.x && normX <= _maxScreenBounds.x &&
                   normY >= _minScreenBounds.y && normY <= _maxScreenBounds.y;
        }

        private void EnsureRaycastTarget()
        {
            // If there's no Graphic on this RectTransform to catch raycasts, ensure an invisible image exists
            Graphic graphic = GetComponent<Graphic>();
            if (graphic == null)
            {
                Image invisibleImage = gameObject.AddComponent<Image>();
                invisibleImage.color = new Color(0f, 0f, 0f, 0f);
                invisibleImage.raycastTarget = true;
            }
            else
            {
                graphic.raycastTarget = true;
            }
        }

        #endregion
    }
}
