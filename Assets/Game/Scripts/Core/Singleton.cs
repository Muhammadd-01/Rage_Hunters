using System;
using UnityEngine;

namespace RageHunters.Core
{
    /// <summary>
    /// Generic persistent singleton MonoBehaviour base class.
    /// Supports thread-safe lazy initialization, DontDestroyOnLoad persistence,
    /// duplicate instance destruction, and application quit handling.
    /// </summary>
    /// <typeparam name="T">The component type derived from Singleton.</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region Fields

        private static T _instance;
        private static readonly object _lock = new();
        private static bool _applicationIsQuitting;

        [SerializeField]
        [Tooltip("Whether this singleton should persist across scene loads.")]
        private bool _persistentAcrossScenes = true;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the singleton instance. If not found in the scene and the application is running,
        /// creates a new GameObject with the component attached.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance of '{typeof(T)}' requested while application is quitting. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance != null)
                    {
                        return _instance;
                    }

                    _instance = FindFirstObjectByType<T>();

                    if (_instance != null)
                    {
                        return _instance;
                    }

                    var singletonObject = new GameObject($"{typeof(T).Name} [Singleton]");
                    _instance = singletonObject.AddComponent<T>();

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Indicates whether an instance currently exists in memory.
        /// </summary>
        public static bool HasInstance => _instance != null;

        /// <summary>
        /// Whether this instance will persist across scenes.
        /// </summary>
        public bool IsPersistent => _persistentAcrossScenes;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Handles singleton registration, duplicate checks, and persistence.
        /// If overriding in a derived class, always call base.Awake().
        /// </summary>
        protected virtual void Awake()
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = this as T;

                    if (_persistentAcrossScenes)
                    {
                        if (transform.parent != null)
                        {
                            transform.SetParent(null);
                        }
                        DontDestroyOnLoad(gameObject);
                    }

                    OnInitialized();
                }
                else if (_instance != this)
                {
                    Debug.LogWarning($"[Singleton] Duplicate instance of '{typeof(T)}' detected on GameObject '{gameObject.name}'. Destroying duplicate.");
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Resets the cached instance if this component is destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            lock (_lock)
            {
                if (_instance == this)
                {
                    _instance = null;
                }
            }
        }

        /// <summary>
        /// Marks the singleton manager as shutting down to prevent recreating instances during quit.
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        #endregion

        #region Virtual Hooks

        /// <summary>
        /// Called when the singleton instance is successfully initialized and assigned for the first time.
        /// </summary>
        protected virtual void OnInitialized() { }

        #endregion
    }
}
