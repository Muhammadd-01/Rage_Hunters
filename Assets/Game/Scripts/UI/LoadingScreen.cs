using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RageHunters.Core;

namespace RageHunters.UI
{
    /// <summary>
    /// Loading screen with progress bar.
    /// Listens to SceneLoader progress events and displays loading status.
    /// </summary>
    public class LoadingScreen : UIScreen
    {
        #region Serialized Fields

        [Header("Progress Display")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Image _progressFill;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _tipText;

        [Header("Visual")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private GameObject _loadingSpinner;

        [Header("Configuration")]
        [SerializeField] private float _smoothSpeed = 3f;
        [SerializeField] private Color _progressColor = new Color(0.8f, 0.1f, 0.1f);

        #endregion

        #region Private Fields

        private float _targetProgress;
        private float _displayedProgress;

        private static readonly string[] _tips = new string[]
        {
            "Use crouch to reduce your visibility to enemies.",
            "Keep an eye on your stamina — sprinting drains it fast.",
            "Different villain archetypes have different detection patterns.",
            "Explore thoroughly — key items may be hidden in unlikely places.",
            "The extraction point activates after objectives are completed.",
            "Listen for footsteps to detect nearby enemies.",
            "Equip utility items for tactical advantages.",
            "Stealthy villains prefer flanking — watch your back.",
            "Coordinate with teammates for the best chance of survival.",
            "Every match awards XP, even if you don't extract."
        };

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            ResetProgress();

            if (_progressFill != null)
                _progressFill.color = _progressColor;

            ShowRandomTip();
        }

        private void Update()
        {
            if (!IsVisible) return;

            // Smooth progress bar animation
            if (_displayedProgress < _targetProgress)
            {
                _displayedProgress = Mathf.MoveTowards(
                    _displayedProgress, _targetProgress,
                    _smoothSpeed * Time.unscaledDeltaTime);

                UpdateProgressUI(_displayedProgress);
            }

            // Rotate spinner
            if (_loadingSpinner != null)
            {
                _loadingSpinner.transform.Rotate(0f, 0f, -200f * Time.unscaledDeltaTime);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets the loading progress value (0.0 to 1.0).
        /// The displayed bar smoothly interpolates toward this value.
        /// </summary>
        public void SetProgress(float progress)
        {
            _targetProgress = Mathf.Clamp01(progress);
        }

        /// <summary>
        /// Sets the status text displayed below the progress bar.
        /// </summary>
        public void SetStatusText(string status)
        {
            if (_statusText != null)
                _statusText.text = status;
        }

        /// <summary>
        /// Resets the progress bar to zero.
        /// </summary>
        public void ResetProgress()
        {
            _targetProgress = 0f;
            _displayedProgress = 0f;
            UpdateProgressUI(0f);

            if (_statusText != null)
                _statusText.text = "Loading...";
        }

        /// <summary>
        /// Shows the loading screen with a fresh progress bar and random tip.
        /// </summary>
        public void ShowLoading()
        {
            ResetProgress();
            ShowRandomTip();
            Show(immediate: true);
        }

        #endregion

        #region Private Helpers

        private void UpdateProgressUI(float progress)
        {
            if (_progressBar != null)
                _progressBar.value = progress;

            if (_progressText != null)
                _progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        private void ShowRandomTip()
        {
            if (_tipText != null && _tips.Length > 0)
            {
                _tipText.text = _tips[Random.Range(0, _tips.Length)];
            }
        }

        #endregion
    }
}
