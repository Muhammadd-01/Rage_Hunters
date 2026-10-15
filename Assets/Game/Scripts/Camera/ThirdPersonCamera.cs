using UnityEngine;
using RageHunters.Core;
using RageHunters.Player;

namespace RageHunters.Camera
{
    /// <summary>
    /// Smooth third-person orbital camera controller.
    /// Orbit angle is driven by PlayerInputHandler.LookInput (supporting mouse, gamepad, and mobile touch).
    /// Features target tracking with configurable offset, smooth damping, and obstacle collision avoidance.
    /// Operates without requiring desktop cursor lock for seamless mobile operation.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    [DisallowMultipleComponent]
    public class ThirdPersonCamera : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Target Tracking")]
        [SerializeField]
        [Tooltip("The Transform the camera orbits and tracks. If null, auto-finds player by tag.")]
        private Transform _target;

        [SerializeField]
        [Tooltip("Offset added to target position (e.g., eye/shoulder height).")]
        private Vector3 _targetOffset = new(0f, 1.5f, 0f);

        [Header("Distance & Orbit")]
        [SerializeField]
        [Tooltip("Default desired distance from target.")]
        private float _defaultDistance = 5.0f;

        [SerializeField]
        [Tooltip("Minimum allowable distance from target (e.g. during collision or zoom).")]
        private float _minDistance = 1.0f;

        [SerializeField]
        [Tooltip("Maximum allowable orbit distance from target.")]
        private float _maxDistance = 8.0f;

        [Header("Orbit Limits & Sensitivity")]
        [SerializeField]
        [Tooltip("Horizontal yaw rotation sensitivity.")]
        private float _yawSensitivity = GameConstants.PlayerDefaults.DefaultLookSensitivity;

        [SerializeField]
        [Tooltip("Vertical pitch rotation sensitivity.")]
        private float _pitchSensitivity = GameConstants.PlayerDefaults.DefaultLookSensitivity;

        [SerializeField]
        [Tooltip("Minimum downward pitch angle in degrees (looking up at player).")]
        private float _minPitch = -25f;

        [SerializeField]
        [Tooltip("Maximum upward pitch angle in degrees (looking down from above).")]
        private float _maxPitch = 70f;

        [SerializeField]
        [Tooltip("Invert vertical look axis.")]
        private bool _invertPitch = false;

        [Header("Smoothing")]
        [SerializeField]
        [Tooltip("Damping time for camera position following.")]
        private float _positionSmoothTime = 0.05f;

        [SerializeField]
        [Tooltip("Damping time for camera rotation interpolation.")]
        private float _rotationSmoothTime = 0.05f;

        [SerializeField]
        [Tooltip("Speed at which distance restores after collision clearance.")]
        private float _zoomRestoreSpeed = 8.0f;

        [Header("Collision Avoidance")]
        [SerializeField]
        [Tooltip("Layer mask for geometry that obstructs the camera view.")]
        private LayerMask _collisionLayers = ~0;

        [SerializeField]
        [Tooltip("Radius of the spherecast used for obstacle detection.")]
        private float _collisionRadius = 0.25f;

        [SerializeField]
        [Tooltip("Safety distance maintained between camera and obstructing surface.")]
        private float _collisionPadding = 0.15f;

        [Header("Input Dependency")]
        [SerializeField]
        [Tooltip("Player input handler providing LookInput vector. Auto-acquired if null.")]
        private PlayerInputHandler _inputHandler;

        #endregion

        #region Private Fields

        private float _currentYaw;
        private float _currentPitch;
        private float _desiredDistance;
        private float _currentDistance;

        private Vector3 _currentVelocity;
        private float _yawVelocity;
        private float _pitchVelocity;
        private float _smoothedYaw;
        private float _smoothedPitch;

        #endregion

        #region Properties

        /// <summary>
        /// Current target being tracked by the camera.
        /// </summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>
        /// Current orbit yaw in degrees.
        /// </summary>
        public float CurrentYaw => _currentYaw;

        /// <summary>
        /// Current orbit pitch in degrees.
        /// </summary>
        public float CurrentPitch => _currentPitch;

        /// <summary>
        /// Current active distance from the target.
        /// </summary>
        public float CurrentDistance => _currentDistance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize angles from current camera orientation
            Vector3 euler = transform.eulerAngles;
            _currentYaw = euler.y;
            _currentPitch = euler.x;
            _smoothedYaw = _currentYaw;
            _smoothedPitch = _currentPitch;

            _desiredDistance = Mathf.Clamp(_defaultDistance, _minDistance, _maxDistance);
            _currentDistance = _desiredDistance;

            // Exclude Player layer from camera collision to avoid self-clipping
            _collisionLayers &= ~LayerMask.GetMask(GameConstants.Layers.Player);
            _collisionLayers &= ~LayerMask.GetMask(GameConstants.Layers.IgnoreRaycast);

            FindTargetIfMissing();
            FindInputHandlerIfMissing();
        }

        private void Start()
        {
            FindTargetIfMissing();
            FindInputHandlerIfMissing();
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                FindTargetIfMissing();
                if (_target == null) return;
            }

            if (_inputHandler == null)
            {
                FindInputHandlerIfMissing();
            }

            ProcessOrbitInput();
            UpdateCameraTransform(Time.deltaTime);
        }

        #endregion

        #region Dependency Resolution

        private void FindTargetIfMissing()
        {
            if (_target != null) return;

            // Try to find player by tag
            var playerObj = GameObject.FindWithTag(GameConstants.Tags.Player);
            if (playerObj != null)
            {
                _target = playerObj.transform;
            }
        }

        private void FindInputHandlerIfMissing()
        {
            if (_inputHandler != null) return;

            if (_target != null)
            {
                _inputHandler = _target.GetComponent<PlayerInputHandler>();
            }

            if (_inputHandler == null)
            {
                _inputHandler = FindFirstObjectByType<PlayerInputHandler>();
            }
        }

        #endregion

        #region Orbit & Collision

        private void ProcessOrbitInput()
        {
            Vector2 lookInput = _inputHandler != null ? _inputHandler.LookInput : Vector2.zero;

            float pitchFactor = _invertPitch ? 1f : -1f;

            _currentYaw += lookInput.x * _yawSensitivity;
            _currentPitch += lookInput.y * _pitchSensitivity * pitchFactor;

            // Wrap yaw in [0, 360)
            if (_currentYaw > 360f) _currentYaw -= 360f;
            else if (_currentYaw < 0f) _currentYaw += 360f;

            // Clamp pitch to configured bounds
            _currentPitch = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);
        }

        private void UpdateCameraTransform(float deltaTime)
        {
            // Smooth yaw and pitch
            _smoothedYaw = Mathf.SmoothDampAngle(_smoothedYaw, _currentYaw, ref _yawVelocity, _rotationSmoothTime);
            _smoothedPitch = Mathf.SmoothDampAngle(_smoothedPitch, _currentPitch, ref _pitchVelocity, _rotationSmoothTime);

            Quaternion rotation = Quaternion.Euler(_smoothedPitch, _smoothedYaw, 0f);

            // Pivot point around target
            Vector3 targetPivot = _target.position + _targetOffset;

            // Calculate obstacle collision and desired distance
            float targetDistance = CalculateObstacleDistance(targetPivot, rotation);

            // Zoom in immediately when obstructed; smoothly return when clearance opens
            if (targetDistance < _currentDistance)
            {
                _currentDistance = targetDistance;
            }
            else
            {
                _currentDistance = Mathf.MoveTowards(_currentDistance, targetDistance, _zoomRestoreSpeed * deltaTime);
            }

            // Compute camera destination
            Vector3 desiredPosition = targetPivot - (rotation * Vector3.forward * _currentDistance);

            // Smooth position movement
            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, _positionSmoothTime);

            transform.position = smoothedPosition;
            transform.rotation = rotation;
        }

        private float CalculateObstacleDistance(Vector3 targetPivot, Quaternion rotation)
        {
            Vector3 direction = -(rotation * Vector3.forward);

            if (Physics.SphereCast(
                targetPivot,
                _collisionRadius,
                direction,
                out RaycastHit hit,
                _desiredDistance,
                _collisionLayers,
                QueryTriggerInteraction.Ignore))
            {
                float clampedHitDistance = Mathf.Max(_minDistance, hit.distance - _collisionPadding);
                return clampedHitDistance;
            }

            return _desiredDistance;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Explicitly sets the target and resets camera orbit behind the target.
        /// </summary>
        public void SetTarget(Transform newTarget, bool resetOrbit = false)
        {
            _target = newTarget;
            _inputHandler = null;
            FindInputHandlerIfMissing();

            if (resetOrbit && _target != null)
            {
                _currentYaw = _target.eulerAngles.y;
                _smoothedYaw = _currentYaw;
                _currentPitch = 15f;
                _smoothedPitch = _currentPitch;
            }
        }

        /// <summary>
        /// Adjusts the desired orbit distance (zoom in/out).
        /// </summary>
        public void SetDesiredDistance(float distance)
        {
            _desiredDistance = Mathf.Clamp(distance, _minDistance, _maxDistance);
        }

        /// <summary>
        /// Sets orbit angles manually.
        /// </summary>
        public void SetOrbit(float yaw, float pitch)
        {
            _currentYaw = yaw;
            _currentPitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);
            _smoothedYaw = _currentYaw;
            _smoothedPitch = _currentPitch;
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_target != null)
            {
                Vector3 targetPivot = _target.position + _targetOffset;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(targetPivot, 0.2f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(targetPivot, transform.position);
            }
        }

        #endregion
    }
}
