using System;
using System.Collections;
using UnityEngine;

namespace RageHunters.UI
{
    /// <summary>
    /// Base class for all UI screens and overlays.
    /// Manages screen lifecycle, canvas group fading transitions,
    /// back-button navigation hierarchy, and visibility state.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Screen Configuration")]
        [SerializeField]
        [Tooltip("Unique screen identifier. Defaults to type name if empty.")]
        private string _screenId;

        [SerializeField]
        [Tooltip("Whether this screen can be pushed to the UIManager back-navigation history stack.")]
        private bool _canAddToBackStack = true;

        [SerializeField]
        [Tooltip("Duration of fade in/out animation in seconds.")]
        private float _fadeDuration = 0.25f;

        [SerializeField]
        [Tooltip("Whether this screen should start active and visible when loaded.")]
        private bool _startVisible = false;

        #endregion

        #region Protected & Private Fields

        [SerializeField]
        [HideInInspector]
        private CanvasGroup _canvasGroup;

        private Coroutine _activeTransitionRoutine;
        private bool _isVisible;

        #endregion

        #region Events

        /// <summary>
        /// Invoked when the screen becomes fully visible.
        /// </summary>
        public event Action OnScreenShown;

        /// <summary>
        /// Invoked when the screen becomes fully hidden.
        /// </summary>
        public event Action OnScreenHidden;

        #endregion

        #region Properties

        /// <summary>
        /// Unique identifier for this screen.
        /// </summary>
        public virtual string ScreenId => string.IsNullOrEmpty(_screenId) ? GetType().Name : _screenId;

        /// <summary>
        /// Whether this screen should be tracked on the back-navigation stack.
        /// </summary>
        public bool CanAddToBackStack => _canAddToBackStack;

        /// <summary>
        /// Current visibility status of the screen.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Cached CanvasGroup reference for UI transitions.
        /// </summary>
        public CanvasGroup CanvasGroup => _canvasGroup;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (!_startVisible)
            {
                Hide(immediate: true);
            }
            else
            {
                Show(immediate: true);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_activeTransitionRoutine != null)
            {
                StopCoroutine(_activeTransitionRoutine);
                _activeTransitionRoutine = null;
            }
        }

        #endregion

        #region Show / Hide API

        /// <summary>
        /// Displays the screen with an optional fade transition.
        /// </summary>
        /// <param name="immediate">If true, skips transition animation.</param>
        public virtual void Show(bool immediate = false)
        {
            if (_activeTransitionRoutine != null)
            {
                StopCoroutine(_activeTransitionRoutine);
                _activeTransitionRoutine = null;
            }

            gameObject.SetActive(true);
            _isVisible = true;

            if (immediate || _fadeDuration <= 0f)
            {
                SetCanvasGroupState(1f, interactable: true);
                OnScreenShown?.Invoke();
                OnShown();
            }
            else
            {
                _activeTransitionRoutine = StartCoroutine(FadeRoutine(1f, true, () =>
                {
                    OnScreenShown?.Invoke();
                    OnShown();
                }));
            }
        }

        /// <summary>
        /// Hides the screen with an optional fade transition.
        /// </summary>
        /// <param name="immediate">If true, skips transition animation.</param>
        public virtual void Hide(bool immediate = false)
        {
            if (_activeTransitionRoutine != null)
            {
                StopCoroutine(_activeTransitionRoutine);
                _activeTransitionRoutine = null;
            }

            _isVisible = false;

            if (immediate || _fadeDuration <= 0f)
            {
                SetCanvasGroupState(0f, interactable: false);
                gameObject.SetActive(false);
                OnScreenHidden?.Invoke();
                OnHidden();
            }
            else
            {
                SetCanvasGroupInteractable(false);
                _activeTransitionRoutine = StartCoroutine(FadeRoutine(0f, false, () =>
                {
                    gameObject.SetActive(false);
                    OnScreenHidden?.Invoke();
                    OnHidden();
                }));
            }
        }

        /// <summary>
        /// Invoked when back navigation is requested (e.g., Android back button, Escape key).
        /// </summary>
        /// <returns>True if the screen consumed the back event, false to permit UIManager fallback.</returns>
        public virtual bool OnBackButtonPressed()
        {
            return false;
        }

        #endregion

        #region Virtual Hooks

        /// <summary>
        /// Called when the screen has completely finished appearing.
        /// </summary>
        protected virtual void OnShown() { }

        /// <summary>
        /// Called when the screen has completely finished disappearing.
        /// </summary>
        protected virtual void OnHidden() { }

        #endregion

        #region Internal Helpers

        private void SetCanvasGroupState(float alpha, bool interactable)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = alpha;
            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
        }

        private void SetCanvasGroupInteractable(bool interactable)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
        }

        private IEnumerator FadeRoutine(float targetAlpha, bool enableInteractionAtEnd, Action onComplete)
        {
            if (_canvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float initialAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeDuration);
                _canvasGroup.alpha = Mathf.Lerp(initialAlpha, targetAlpha, t);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            SetCanvasGroupInteractable(enableInteractionAtEnd);
            _activeTransitionRoutine = null;
            onComplete?.Invoke();
        }

        #endregion
    }
}
