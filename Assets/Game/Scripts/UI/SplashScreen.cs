using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RageHunters.Core;

namespace RageHunters.UI
{
    /// <summary>
    /// Initial loading / splash screen shown upon game startup.
    /// Displays the title branding, progress bar, and initialization subsystem status.
    /// Seamlessly auto-transitions to the MainMenu when GameBootstrapper signals completion.
    /// </summary>
    public sealed class SplashScreen : UIScreen
    {
        #region Serialized Fields

        [Header("UI Components")]
        [SerializeField]
        [Tooltip("Game logo display image.")]
        private Image _logoImage;

        [SerializeField]
        [Tooltip("Loading progress bar slider.")]
        private Slider _loadingBar;

        [SerializeField]
        [Tooltip("Text displaying current initialization subsystem status.")]
        private TMP_Text _statusText;

        [SerializeField]
        [Tooltip("Text displaying client application version.")]
        private TMP_Text _versionText;

        [Header("Animation & Timing")]
        [SerializeField]
        [Tooltip("Minimum time in seconds to hold the splash screen before transitioning.")]
        private float _minDisplayDuration = 1.5f;

        [SerializeField]
        [Tooltip("Smooth speed for progress bar interpolation.")]
        private float _progressSmoothSpeed = 5.0f;

        #endregion

        #region Private Fields

        private float _targetProgress;
        private float _currentProgress;
        private float _timeDisplayed;
        private bool _isReadyTriggered;
        private Coroutine _transitionRoutine;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }

            if (_loadingBar != null)
            {
                _loadingBar.value = 0f;
            }

            if (_statusText != null)
            {
                _statusText.text = "Initializing...";
            }
        }

        private void OnEnable()
        {
            GameBootstrapper.OnBootstrapProgress += HandleBootstrapProgress;
            GameBootstrapper.OnBootstrapReady += HandleBootstrapReady;

            _timeDisplayed = 0f;
            _targetProgress = 0f;
            _currentProgress = 0f;
            _isReadyTriggered = false;

            // If GameBootstrapper already finished before this screen enabled
            if (GameBootstrapper.IsReady)
            {
                HandleBootstrapReady();
            }
        }

        private void OnDisable()
        {
            GameBootstrapper.OnBootstrapProgress -= HandleBootstrapProgress;
            GameBootstrapper.OnBootstrapReady -= HandleBootstrapReady;

            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
            }
        }

        private void Update()
        {
            _timeDisplayed += Time.unscaledDeltaTime;

            // Smooth progress bar filling
            if (_loadingBar != null && Mathf.Abs(_currentProgress - _targetProgress) > 0.001f)
            {
                _currentProgress = Mathf.MoveTowards(_currentProgress, _targetProgress, _progressSmoothSpeed * Time.unscaledDeltaTime);
                _loadingBar.value = _currentProgress;
            }
        }

        #endregion

        #region Bootstrap Event Handlers

        private void HandleBootstrapProgress(float progress, string status)
        {
            _targetProgress = Mathf.Clamp01(progress);

            if (_statusText != null && !string.IsNullOrEmpty(status))
            {
                _statusText.text = status;
            }
        }

        private void HandleBootstrapReady()
        {
            if (_isReadyTriggered) return;
            _isReadyTriggered = true;

            _targetProgress = 1f;
            if (_statusText != null)
            {
                _statusText.text = "Ready";
            }

            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
            }
            _transitionRoutine = StartCoroutine(TransitionToMainMenuRoutine());
        }

        private IEnumerator TransitionToMainMenuRoutine()
        {
            // Ensure minimum display duration is respected
            while (_timeDisplayed < _minDisplayDuration || _currentProgress < 0.95f)
            {
                yield return null;
            }

            if (_loadingBar != null)
            {
                _loadingBar.value = 1f;
            }

            // Small delay for visual polish
            yield return new WaitForSecondsRealtime(0.2f);

            // Transition to MainMenu screen
            if (UIManager.HasInstance)
            {
                UIManager.Instance.Show<MainMenuScreen>(immediate: false, hideCurrent: true);
            }
            else
            {
                Hide(immediate: false);
            }

            _transitionRoutine = null;
        }

        #endregion
    }
}
