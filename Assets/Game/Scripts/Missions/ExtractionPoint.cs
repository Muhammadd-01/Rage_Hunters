using UnityEngine;

namespace RageHunters.Missions
{
    /// <summary>
    /// Marks a world object as an extraction point.
    /// Players interact with it to trigger extraction when objectives are complete.
    /// </summary>
    public class ExtractionPoint : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float _extractionTime = 5f;
        [SerializeField] private float _interactionRadius = 3f;
        [SerializeField] private GameObject _activeVisual;
        [SerializeField] private GameObject _inactiveVisual;

        private bool _isActive;
        private bool _isExtracting;
        private float _extractionProgress;

        #region Events

        public event System.Action<float> OnExtractionProgress;
        public event System.Action OnExtractionStarted;
        public event System.Action OnExtractionCancelled;

        #endregion

        #region Properties

        public bool IsActive => _isActive;
        public bool IsExtracting => _isExtracting;
        public float ExtractionProgress => _extractionProgress;
        public float InteractionRadius => _interactionRadius;

        #endregion

        private void Start()
        {
            SetActive(false);

            // Listen for extraction availability
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnExtractionAvailable += () => SetActive(true);
            }
        }

        /// <summary>
        /// Activates or deactivates the extraction point.
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;

            if (_activeVisual != null) _activeVisual.SetActive(active);
            if (_inactiveVisual != null) _inactiveVisual.SetActive(!active);
        }

        /// <summary>
        /// Begins the extraction countdown. Call every frame while the player holds interact.
        /// </summary>
        public void UpdateExtraction(bool playerHoldingInteract)
        {
            if (!_isActive) return;

            if (playerHoldingInteract)
            {
                if (!_isExtracting)
                {
                    _isExtracting = true;
                    _extractionProgress = 0f;
                    OnExtractionStarted?.Invoke();
                }

                _extractionProgress += Time.deltaTime / _extractionTime;
                OnExtractionProgress?.Invoke(_extractionProgress);

                if (_extractionProgress >= 1f)
                {
                    CompleteExtraction();
                }
            }
            else if (_isExtracting)
            {
                CancelExtraction();
            }
        }

        private void CompleteExtraction()
        {
            _isExtracting = false;
            _extractionProgress = 1f;

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.PlayerExtracted();
            }
        }

        private void CancelExtraction()
        {
            _isExtracting = false;
            _extractionProgress = 0f;
            OnExtractionCancelled?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isActive ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);
        }
    }
}
