using System.Collections;
using UnityEngine;

namespace RageHunters.Core
{
    /// <summary>
    /// Initial entry point MonoBehaviour for the Bootstrapper / Initialization scene.
    /// Ensures singletons are instantiated in the correct order, checks required services,
    /// and seamlessly transitions to the Main Menu.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Startup Configuration")]
        [SerializeField]
        [Tooltip("Minimum time in seconds to hold bootstrapper/splash screen.")]
        private float _splashDelay = 1.0f;

        [SerializeField]
        [Tooltip("Target scene name to transition to after initialization completes.")]
        private string _targetInitialScene = GameConstants.Scenes.MainMenu;

        [SerializeField]
        [Tooltip("Whether to automatically begin bootstrap sequence in Start.")]
        private bool _autoStartBootstrap = true;

        #endregion

        #region Private Fields

        private bool _bootstrapStarted;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_autoStartBootstrap)
            {
                StartCoroutine(BootstrapRoutine());
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired during bootstrapping with normalized progress (0-1) and status description.
        /// </summary>
        public static event System.Action<float, string> OnBootstrapProgress;

        /// <summary>
        /// Fired when bootstrapping has completed and systems are ready.
        /// </summary>
        public static event System.Action OnBootstrapReady;

        /// <summary>
        /// True once bootstrap sequence has completed and the game is ready.
        /// </summary>
        public static bool IsReady { get; private set; }

        #endregion

        #region Public API

        /// <summary>
        /// Manually initiates the bootstrap sequence if autoStart was disabled.
        /// </summary>
        public void StartBootstrap()
        {
            if (_bootstrapStarted) return;
            StartCoroutine(BootstrapRoutine());
        }

        #endregion

        #region Bootstrap Pipeline

        private IEnumerator BootstrapRoutine()
        {
            _bootstrapStarted = true;
            IsReady = false;
            Debug.Log("[GameBootstrapper] Starting RAGE HUNTERS initialization pipeline...");

            OnBootstrapProgress?.Invoke(0.1f, "Initializing Event System...");
            float startTime = Time.unscaledTime;

            // 1. Initialize EventBus
            EventBus.ClearAll();
            Debug.Log("[GameBootstrapper] EventBus initialized.");

            // 2. Initialize Core Singletons in deterministic order
            OnBootstrapProgress?.Invoke(0.3f, "Initializing Game Systems...");
            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("[GameBootstrapper] Failed to initialize GameManager!");
                yield break;
            }
            gameManager.ChangeState(GameManager.GameState.Initializing);
            Debug.Log("[GameBootstrapper] GameManager initialized.");

            var sceneLoader = SceneLoader.Instance;
            if (sceneLoader == null)
            {
                Debug.LogError("[GameBootstrapper] Failed to initialize SceneLoader!");
                yield break;
            }
            Debug.Log("[GameBootstrapper] SceneLoader initialized.");

            // 3. Verify Required Engine & Device Services
            OnBootstrapProgress?.Invoke(0.6f, "Verifying Hardware & Services...");
            yield return StartCoroutine(CheckRequiredServices());

            // 4. Honor splash delay
            OnBootstrapProgress?.Invoke(0.85f, "Preparing Game World...");
            float elapsed = Time.unscaledTime - startTime;
            if (elapsed < _splashDelay)
            {
                yield return new WaitForSecondsRealtime(_splashDelay - elapsed);
            }

            OnBootstrapProgress?.Invoke(1.0f, "Ready");
            IsReady = true;
            OnBootstrapReady?.Invoke();

            // 5. Transition to Main Menu
            Debug.Log($"[GameBootstrapper] Bootstrapping completed. Loading initial scene '{_targetInitialScene}'...");
            gameManager.ChangeState(GameManager.GameState.Loading);

            sceneLoader.LoadScene(_targetInitialScene, null, () =>
            {
                GameManager.Instance.ChangeState(GameManager.GameState.MainMenu);
                Debug.Log("[GameBootstrapper] Transitioned to MainMenu.");
            });
        }

        private IEnumerator CheckRequiredServices()
        {
            // Frame delay to allow any dependent subsystem or graphics pipeline tasks to register
            yield return null;

            // Log hardware and graphics capabilities
            Debug.Log($"[GameBootstrapper] Device Model: {SystemInfo.deviceModel} | Graphics: {SystemInfo.graphicsDeviceType} | Memory: {SystemInfo.systemMemorySize}MB");
        }

        #endregion
    }
}
