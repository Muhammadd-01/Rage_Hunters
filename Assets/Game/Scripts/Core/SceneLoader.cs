using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RageHunters.Core
{
    /// <summary>
    /// Asynchronous scene loading manager (singleton).
    /// Handles single and additive scene loading, progress tracking,
    /// loading screen hooks, and an Addressables-ready abstraction fallback.
    /// </summary>
    public sealed class SceneLoader : Singleton<SceneLoader>
    {
        #region Events & Callbacks

        /// <summary>
        /// Fired when scene loading starts. (sceneName, isAdditive)
        /// </summary>
        public event Action<string, bool> OnLoadingStarted;

        /// <summary>
        /// Fired during scene load with normalized progress [0f, 1f].
        /// </summary>
        public event Action<float> OnLoadingProgress;

        /// <summary>
        /// Fired when scene loading and activation has completed. (sceneName)
        /// </summary>
        public event Action<string> OnLoadingCompleted;

        #endregion

        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Minimum duration in seconds to display loading screen to prevent flickering on fast loads.")]
        private float _minimumLoadingTime = 0.5f;

        [SerializeField]
        [Tooltip("Whether to automatically activate the scene once loaded.")]
        private bool _autoActivateScene = true;

        #endregion

        #region Private Fields

        private Coroutine _currentLoadRoutine;
        private bool _isLoading;
        private float _currentProgress;
        private string _activeTargetScene;

        #endregion

        #region Properties

        public bool IsLoading => _isLoading;
        public float CurrentProgress => _currentProgress;
        public string ActiveTargetScene => _activeTargetScene;

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads a scene asynchronously by name using Single mode.
        /// </summary>
        /// <param name="sceneName">Target scene name.</param>
        /// <param name="onProgress">Optional callback for loading progress (0-1).</param>
        /// <param name="onComplete">Optional callback upon completion.</param>
        public void LoadScene(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        {
            LoadScene(sceneName, LoadSceneMode.Single, onProgress, onComplete);
        }

        /// <summary>
        /// Loads a scene asynchronously with specified LoadSceneMode.
        /// </summary>
        /// <param name="sceneName">Target scene name.</param>
        /// <param name="mode">Single or Additive mode.</param>
        /// <param name="onProgress">Optional callback for loading progress (0-1).</param>
        /// <param name="onComplete">Optional callback upon completion.</param>
        public void LoadScene(string sceneName, LoadSceneMode mode, Action<float> onProgress = null, Action onComplete = null)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneLoader] Cannot load '{sceneName}'. Scene load '{_activeTargetScene}' is already in progress.");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneLoader] LoadScene called with an empty scene name.");
                return;
            }

            if (_currentLoadRoutine != null)
            {
                StopCoroutine(_currentLoadRoutine);
            }

            _currentLoadRoutine = StartCoroutine(LoadSceneRoutine(sceneName, mode, onProgress, onComplete));
        }

        /// <summary>
        /// Unloads an additively loaded scene asynchronously.
        /// </summary>
        public void UnloadScene(string sceneName, Action onComplete = null)
        {
            StartCoroutine(UnloadSceneRoutine(sceneName, onComplete));
        }

        #endregion

        #region Loading Routines

        private IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode mode, Action<float> onProgress, Action onComplete)
        {
            _isLoading = true;
            _activeTargetScene = sceneName;
            _currentProgress = 0f;

            bool isAdditive = mode == LoadSceneMode.Additive;
            OnLoadingStarted?.Invoke(sceneName, isAdditive);

            float startTime = Time.unscaledTime;

            // Architecture hook: Can swap to Addressables.LoadSceneAsync here once Addressables package is wired.
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);

            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Failed to start loading scene '{sceneName}'. Verify scene is in Build Settings.");
                _isLoading = false;
                yield break;
            }

            operation.allowSceneActivation = _autoActivateScene;

            while (!operation.isDone)
            {
                // AsyncOperation.progress maxes out at 0.9 when allowSceneActivation is false or until scene is ready
                float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
                _currentProgress = targetProgress;

                OnLoadingProgress?.Invoke(_currentProgress);
                onProgress?.Invoke(_currentProgress);

                // If loading finished under minimum display time, wait to keep UI smooth
                if (operation.progress >= 0.9f)
                {
                    float elapsedTime = Time.unscaledTime - startTime;
                    if (elapsedTime < _minimumLoadingTime)
                    {
                        yield return new WaitForSecondsRealtime(_minimumLoadingTime - elapsedTime);
                    }

                    if (!operation.allowSceneActivation)
                    {
                        operation.allowSceneActivation = true;
                    }
                }

                yield return null;
            }

            _currentProgress = 1f;
            OnLoadingProgress?.Invoke(1f);
            onProgress?.Invoke(1f);

            _isLoading = false;
            _currentLoadRoutine = null;

            OnLoadingCompleted?.Invoke(sceneName);
            onComplete?.Invoke();
        }

        private IEnumerator UnloadSceneRoutine(string sceneName, Action onComplete)
        {
            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation != null)
            {
                while (!operation.isDone)
                {
                    yield return null;
                }
            }

            onComplete?.Invoke();
        }

        #endregion
    }
}
