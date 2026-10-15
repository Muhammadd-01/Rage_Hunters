using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RageHunters.Core;

namespace RageHunters.UI
{
    /// <summary>
    /// Central manager overseeing all UI screens and overlays.
    /// Handles registration, transition animations, hierarchical back-stack navigation,
    /// UI audio triggers, gameplay input blocking, and GameManager lifecycle integration.
    /// </summary>
    public sealed class UIManager : Singleton<UIManager>
    {
        #region Serialized Fields

        [Header("Audio Settings")]
        [SerializeField]
        [Tooltip("Audio source designated for 2D UI interface sound effects.")]
        private AudioSource _uiAudioSource;

        [SerializeField]
        [Tooltip("Default audio clip played on generic button clicks.")]
        private AudioClip _defaultButtonClickClip;

        [Header("Pre-configured Screens (Optional Scene References)")]
        [SerializeField]
        [Tooltip("Initial list of screens to pre-register in Awake.")]
        private List<UIScreen> _initialScreens = new();

        [Header("Input Blocking")]
        [SerializeField]
        [Tooltip("Whether to broadcast gameplay input blocking when modal menus are opened.")]
        private bool _blockGameplayInputWhenMenuOpen = true;

        #endregion

        #region Private Fields

        private readonly Dictionary<Type, UIScreen> _screenByType = new();
        private readonly Dictionary<string, UIScreen> _screenById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<UIScreen> _backStack = new();

        private UIScreen _activeModalScreen;
        private bool _isGameplayInputBlocked;

        #endregion

        #region Events

        /// <summary>
        /// Fired when any screen visibility changes. (screen, isVisible)
        /// </summary>
        public event Action<UIScreen, bool> OnScreenVisibilityChanged;

        /// <summary>
        /// Fired when gameplay input blocking state changes. (isBlocked)
        /// </summary>
        public event Action<bool> OnGameplayInputBlockedChanged;

        #endregion

        #region Properties

        /// <summary>
        /// True if any blocking modal menu (Pause, Settings, etc.) is currently open.
        /// </summary>
        public bool IsGameplayInputBlocked => _isGameplayInputBlocked;

        /// <summary>
        /// Number of screens currently tracked on the back-navigation stack.
        /// </summary>
        public int BackStackCount => _backStack.Count;

        /// <summary>
        /// Currently top-most active screen on the back-stack.
        /// </summary>
        public UIScreen CurrentActiveScreen => _backStack.Count > 0 ? _backStack.Peek() : null;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            if (_uiAudioSource == null)
            {
                _uiAudioSource = gameObject.GetComponent<AudioSource>();
                if (_uiAudioSource == null)
                {
                    _uiAudioSource = gameObject.AddComponent<AudioSource>();
                    _uiAudioSource.playOnAwake = false;
                    _uiAudioSource.spatialBlend = 0f; // 2D UI sound
                }
            }

            // Register serialized initial screens
            for (int i = 0; i < _initialScreens.Count; i++)
            {
                if (_initialScreens[i] != null)
                {
                    RegisterScreen(_initialScreens[i]);
                }
            }

            // Auto-discover screens under this GameObject if any
            var childScreens = GetComponentsInChildren<UIScreen>(includeInactive: true);
            for (int i = 0; i < childScreens.Length; i++)
            {
                RegisterScreen(childScreens[i]);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(HandleGameStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(HandleGameStateChanged);
        }

        private void Update()
        {
            HandleBackNavigationInput();
        }

        #endregion

        #region Screen Registration

        /// <summary>
        /// Registers a UIScreen instance with the manager.
        /// </summary>
        public void RegisterScreen(UIScreen screen)
        {
            if (screen == null) return;

            Type type = screen.GetType();
            _screenByType[type] = screen;

            if (!string.IsNullOrEmpty(screen.ScreenId))
            {
                _screenById[screen.ScreenId] = screen;
            }
        }

        /// <summary>
        /// Unregisters a UIScreen instance.
        /// </summary>
        public void UnregisterScreen(UIScreen screen)
        {
            if (screen == null) return;

            Type type = screen.GetType();
            if (_screenByType.TryGetValue(type, out var registered) && registered == screen)
            {
                _screenByType.Remove(type);
            }

            if (!string.IsNullOrEmpty(screen.ScreenId) && _screenById.TryGetValue(screen.ScreenId, out var idRegistered) && idRegistered == screen)
            {
                _screenById.Remove(screen.ScreenId);
            }
        }

        /// <summary>
        /// Retrieves a registered screen by type.
        /// </summary>
        public T GetScreen<T>() where T : UIScreen
        {
            if (_screenByType.TryGetValue(typeof(T), out var screen))
            {
                return screen as T;
            }
            return null;
        }

        /// <summary>
        /// Retrieves a registered screen by ID.
        /// </summary>
        public UIScreen GetScreen(string screenId)
        {
            if (string.IsNullOrEmpty(screenId)) return null;

            if (_screenById.TryGetValue(screenId, out var screen))
            {
                return screen;
            }
            return null;
        }

        #endregion

        #region Show / Hide Operations

        /// <summary>
        /// Shows a screen of type T with transition options.
        /// </summary>
        public T Show<T>(bool immediate = false, bool hideCurrent = false) where T : UIScreen
        {
            var screen = GetScreen<T>();
            if (screen == null)
            {
                Debug.LogWarning($"[UIManager] Cannot show screen of type '{typeof(T).Name}'. Not registered.");
                return null;
            }

            ShowScreenInternal(screen, immediate, hideCurrent);
            return screen;
        }

        /// <summary>
        /// Shows a screen by ID with transition options.
        /// </summary>
        public UIScreen Show(string screenId, bool immediate = false, bool hideCurrent = false)
        {
            var screen = GetScreen(screenId);
            if (screen == null)
            {
                Debug.LogWarning($"[UIManager] Cannot show screen with ID '{screenId}'. Not registered.");
                return null;
            }

            ShowScreenInternal(screen, immediate, hideCurrent);
            return screen;
        }

        /// <summary>
        /// Hides a screen of type T.
        /// </summary>
        public void Hide<T>(bool immediate = false) where T : UIScreen
        {
            var screen = GetScreen<T>();
            if (screen != null)
            {
                HideScreenInternal(screen, immediate);
            }
        }

        /// <summary>
        /// Hides a screen by ID.
        /// </summary>
        public void Hide(string screenId, bool immediate = false)
        {
            var screen = GetScreen(screenId);
            if (screen != null)
            {
                HideScreenInternal(screen, immediate);
            }
        }

        /// <summary>
        /// Hides all currently registered screens.
        /// </summary>
        public void HideAllScreens(bool immediate = false)
        {
            foreach (var kvp in _screenByType)
            {
                if (kvp.Value != null && kvp.Value.IsVisible)
                {
                    kvp.Value.Hide(immediate);
                    OnScreenVisibilityChanged?.Invoke(kvp.Value, false);
                }
            }

            _backStack.Clear();
            UpdateGameplayInputState();
        }

        private void ShowScreenInternal(UIScreen screen, bool immediate, bool hideCurrent)
        {
            if (hideCurrent && _backStack.Count > 0)
            {
                var top = _backStack.Peek();
                if (top != screen && top.IsVisible)
                {
                    top.Hide(immediate);
                }
            }

            screen.Show(immediate);

            if (screen.CanAddToBackStack)
            {
                if (_backStack.Count == 0 || _backStack.Peek() != screen)
                {
                    _backStack.Push(screen);
                }
            }

            OnScreenVisibilityChanged?.Invoke(screen, true);
            UpdateGameplayInputState();
        }

        private void HideScreenInternal(UIScreen screen, bool immediate)
        {
            screen.Hide(immediate);

            if (_backStack.Count > 0 && _backStack.Peek() == screen)
            {
                _backStack.Pop();
            }

            OnScreenVisibilityChanged?.Invoke(screen, false);
            UpdateGameplayInputState();
        }

        #endregion

        #region Back Navigation

        /// <summary>
        /// Navigates back one step in the screen hierarchy.
        /// </summary>
        /// <returns>True if back navigation was executed.</returns>
        public bool NavigateBack()
        {
            if (_backStack.Count == 0) return false;

            var current = _backStack.Peek();
            if (current != null && current.OnBackButtonPressed())
            {
                // Handled internally by screen
                return true;
            }

            if (_backStack.Count > 1)
            {
                var leavingScreen = _backStack.Pop();
                leavingScreen.Hide(immediate: false);
                OnScreenVisibilityChanged?.Invoke(leavingScreen, false);

                var previousScreen = _backStack.Peek();
                if (previousScreen != null && !previousScreen.IsVisible)
                {
                    previousScreen.Show(immediate: false);
                    OnScreenVisibilityChanged?.Invoke(previousScreen, true);
                }

                UpdateGameplayInputState();
                return true;
            }

            return false;
        }

        private void HandleBackNavigationInput()
        {
            // Support both Escape key (PC/Editor) and Android hardware back button via New Input System
            bool backPressed = false;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                backPressed = true;
            }

#if UNITY_ANDROID
            // Handled via Escape in the new Input System on Android
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                backPressed = true;
            }
#endif

            if (backPressed)
            {
                NavigateBack();
            }
        }

        #endregion

        #region Input Blocking & State Check

        private void UpdateGameplayInputState()
        {
            if (!_blockGameplayInputWhenMenuOpen) return;

            // An input blocking screen is any visible screen that is not pure in-game HUD
            bool shouldBlock = false;
            foreach (var kvp in _screenByType)
            {
                var screen = kvp.Value;
                if (screen != null && screen.IsVisible)
                {
                    // HUDScreen does not block gameplay input
                    if (screen is not HUDScreen)
                    {
                        shouldBlock = true;
                        break;
                    }
                }
            }

            if (_isGameplayInputBlocked != shouldBlock)
            {
                _isGameplayInputBlocked = shouldBlock;
                OnGameplayInputBlockedChanged?.Invoke(_isGameplayInputBlocked);
            }
        }

        #endregion

        #region Audio Management

        /// <summary>
        /// Plays a UI click sound effect.
        /// </summary>
        /// <param name="clip">Optional custom clip; plays default click if null.</param>
        public void PlayClickSound(AudioClip clip = null)
        {
            var targetClip = clip != null ? clip : _defaultButtonClickClip;
            if (targetClip != null && _uiAudioSource != null)
            {
                _uiAudioSource.PlayOneShot(targetClip);
            }
        }

        #endregion

        #region GameManager Lifecycle Integration

        private void HandleGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.NewState)
            {
                case GameManager.GameState.Initializing:
                    HideAllScreens(immediate: true);
                    Show<SplashScreen>(immediate: true);
                    break;

                case GameManager.GameState.MainMenu:
                    HideAllScreens(immediate: true);
                    Show<MainMenuScreen>(immediate: false);
                    break;

                case GameManager.GameState.Loading:
                    Show<LoadingScreen>(immediate: true);
                    break;

                case GameManager.GameState.InGame:
                    Hide<LoadingScreen>(immediate: false);
                    Hide<PauseMenuScreen>(immediate: true);
                    Hide<MatchResultsScreen>(immediate: true);
                    Show<HUDScreen>(immediate: false);
                    break;

                case GameManager.GameState.Paused:
                    Show<PauseMenuScreen>(immediate: false);
                    break;

                case GameManager.GameState.MatchEnd:
                    Hide<PauseMenuScreen>(immediate: true);
                    Hide<HUDScreen>(immediate: true);
                    Show<MatchResultsScreen>(immediate: false);
                    break;

                case GameManager.GameState.Disconnected:
                    HideAllScreens(immediate: true);
                    Show<MainMenuScreen>(immediate: true);
                    break;
            }
        }

        #endregion
    }
}
