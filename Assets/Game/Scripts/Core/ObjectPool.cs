using System;
using System.Collections.Generic;
using UnityEngine;

namespace RageHunters.Core
{
    /// <summary>
    /// Optional interface implemented by pooled GameObject components
    /// to handle lifecycle resets when retrieved from or returned to the pool.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }

    /// <summary>
    /// High-performance GameObject pool for mobile devices.
    /// Supports pre-warming, auto-expansion, active/inactive tracking,
    /// and lifecycle notifications via IPoolable.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Pool Setup")]
        [SerializeField]
        [Tooltip("The prefab template to instantiate for this pool.")]
        private GameObject _prefab;

        [SerializeField]
        [Tooltip("Initial number of instances to instantiate during pre-warming.")]
        private int _initialPoolSize = GameConstants.PoolingDefaults.DefaultInitialPoolSize;

        [SerializeField]
        [Tooltip("Whether the pool can instantiate new objects beyond initial pool size when exhausted.")]
        private bool _autoExpand = true;

        [SerializeField]
        [Tooltip("Maximum allowed capacity if auto-expand is enabled (0 for unlimited).")]
        private int _maxCapacity = GameConstants.PoolingDefaults.DefaultMaxPoolSize;

        [SerializeField]
        [Tooltip("Whether to automatically pre-warm the pool on Awake.")]
        private bool _prewarmOnAwake = true;

        #endregion

        #region Private Fields

        private readonly Queue<GameObject> _inactiveQueue = new();
        private readonly HashSet<GameObject> _activeSet = new();
        private Transform _poolRoot;

        #endregion

        #region Properties

        public GameObject Prefab => _prefab;
        public int InactiveCount => _inactiveQueue.Count;
        public int ActiveCount => _activeSet.Count;
        public int TotalCount => InactiveCount + ActiveCount;
        public bool IsAutoExpandable => _autoExpand;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _poolRoot = transform;

            if (_prewarmOnAwake && _prefab != null)
            {
                Prewarm(_initialPoolSize);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Configures the pool dynamically with a prefab and prewarms it.
        /// </summary>
        public void Initialize(GameObject prefab, int initialSize, bool autoExpand = true, int maxCapacity = 0)
        {
            _prefab = prefab;
            _initialPoolSize = initialSize;
            _autoExpand = autoExpand;
            _maxCapacity = maxCapacity;

            Prewarm(initialSize);
        }

        /// <summary>
        /// Pre-warms the pool by instantiating the requested number of instances.
        /// </summary>
        /// <param name="count">Number of instances to create.</param>
        public void Prewarm(int count)
        {
            if (_prefab == null)
            {
                Debug.LogError($"[ObjectPool] Cannot prewarm on '{gameObject.name}': Prefab reference is null.");
                return;
            }

            int needed = count - TotalCount;
            for (int i = 0; i < needed; i++)
            {
                CreateNewInstance();
            }
        }

        /// <summary>
        /// Retrieves an inactive GameObject from the pool, activates it, and notifies IPoolable listeners.
        /// </summary>
        /// <returns>Active GameObject instance, or null if exhausted and cannot expand.</returns>
        public GameObject Get()
        {
            return Get(Vector3.zero, Quaternion.identity, null);
        }

        /// <summary>
        /// Retrieves an inactive GameObject from the pool positioned and rotated as specified.
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject instance = null;

            while (_inactiveQueue.Count > 0 && instance == null)
            {
                instance = _inactiveQueue.Dequeue();
            }

            if (instance == null)
            {
                if (_autoExpand && (_maxCapacity <= 0 || TotalCount < _maxCapacity))
                {
                    instance = CreateInstanceObject();
                }
                else
                {
                    Debug.LogWarning($"[ObjectPool] Pool for '{_prefab.name}' reached capacity limit ({_maxCapacity}) or cannot expand.");
                    return null;
                }
            }

            _activeSet.Add(instance);

            Transform instTransform = instance.transform;
            if (parent != null)
            {
                instTransform.SetParent(parent, false);
            }
            instTransform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            // Notify IPoolable components
            var poolables = instance.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawnFromPool();
            }

            return instance;
        }

        /// <summary>
        /// Retrieves an inactive component T from the pool positioned and rotated as specified.
        /// </summary>
        public T Get<T>(Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            var go = Get(position, rotation, parent);
            return go != null ? go.GetComponent<T>() : null;
        }

        /// <summary>
        /// Returns an active GameObject instance to the pool, deactivating it.
        /// </summary>
        /// <param name="instance">GameObject to return.</param>
        public void Return(GameObject instance)
        {
            if (instance == null) return;

            if (!_activeSet.Remove(instance))
            {
                // Prevent duplicate returns or returning objects foreign to this pool
                if (_inactiveQueue.Contains(instance))
                {
                    Debug.LogWarning($"[ObjectPool] Attempted to return '{instance.name}' which is already inactive in pool.");
                    return;
                }
            }

            // Notify IPoolable components before deactivation
            var poolables = instance.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnReturnToPool();
            }

            instance.SetActive(false);
            if (_poolRoot != null)
            {
                instance.transform.SetParent(_poolRoot, false);
            }

            _inactiveQueue.Enqueue(instance);
        }

        /// <summary>
        /// Clears and destroys all pooled objects.
        /// </summary>
        public void Clear()
        {
            while (_inactiveQueue.Count > 0)
            {
                var go = _inactiveQueue.Dequeue();
                if (go != null) Destroy(go);
            }

            foreach (var go in _activeSet)
            {
                if (go != null) Destroy(go);
            }

            _activeSet.Clear();
        }

        #endregion

        #region Internal Helpers

        private GameObject CreateNewInstance()
        {
            var instance = CreateInstanceObject();
            instance.SetActive(false);
            _inactiveQueue.Enqueue(instance);
            return instance;
        }

        private GameObject CreateInstanceObject()
        {
            var instance = Instantiate(_prefab, _poolRoot != null ? _poolRoot : transform);
            instance.name = $"{_prefab.name} (Pooled)";
            return instance;
        }

        #endregion
    }
}
