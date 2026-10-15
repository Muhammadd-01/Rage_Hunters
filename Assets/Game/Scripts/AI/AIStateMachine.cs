using System;
using System.Collections.Generic;
using UnityEngine;

namespace RageHunters.AI
{
    /// <summary>
    /// Standard AI behavior states supported by the default combat villain FSM.
    /// </summary>
    public enum AIStateType
    {
        Idle,
        Patrol,
        Investigate,
        Detect,
        Chase,
        Attack,
        Search,
        Retreat,
        ReturnToPatrol
    }

    /// <summary>
    /// Generic finite state machine for AI actors.
    /// Manages state registration, transitions, enter/exit callbacks, and update loop forwarding.
    /// </summary>
    /// <typeparam name="TKey">Enum state key identifier.</typeparam>
    public class AIStateMachine<TKey> where TKey : struct, Enum
    {
        #region Private Fields

        private readonly Dictionary<TKey, AIState> _states = new();
        private AIState _currentState;
        private TKey _currentStateKey;
        private TKey _previousStateKey;
        private bool _isInitialized;
        private bool _enableDebugLogging;
        private readonly string _ownerName;

        #endregion

        #region Events

        /// <summary>
        /// Invoked when a state transition completes. Passes (previousState, newState).
        /// </summary>
        public event Action<TKey, TKey> OnStateChanged;

        #endregion

        #region Properties

        public TKey CurrentStateKey => _currentStateKey;
        public TKey PreviousStateKey => _previousStateKey;
        public AIState CurrentState => _currentState;
        public bool IsInitialized => _isInitialized;

        public bool EnableDebugLogging
        {
            get => _enableDebugLogging;
            set => _enableDebugLogging = value;
        }

        #endregion

        #region Constructor

        public AIStateMachine(string ownerName = "AI", bool enableDebugLogging = false)
        {
            _ownerName = ownerName;
            _enableDebugLogging = enableDebugLogging;
        }

        #endregion

        #region State Management

        /// <summary>
        /// Registers a state instance with its corresponding enum key.
        /// </summary>
        public void RegisterState(TKey key, AIState state)
        {
            if (state == null)
            {
                Debug.LogWarning($"[{_ownerName} FSM] Cannot register null state for key {key}.");
                return;
            }

            _states[key] = state;
        }

        /// <summary>
        /// Transitions to the target state key. Exits current state and enters new state.
        /// </summary>
        public void TransitionTo(TKey newKey)
        {
            if (!_states.TryGetValue(newKey, out var nextState))
            {
                Debug.LogError($"[{_ownerName} FSM] State key '{newKey}' is not registered!");
                return;
            }

            if (_isInitialized && EqualityComparer<TKey>.Default.Equals(_currentStateKey, newKey))
            {
                // Already in this state
                return;
            }

            TKey oldKey = _currentStateKey;

            if (_currentState != null)
            {
                _currentState.Exit();
            }

            _previousStateKey = oldKey;
            _currentStateKey = newKey;
            _currentState = nextState;
            _isInitialized = true;

            if (_enableDebugLogging)
            {
                Debug.Log($"[{_ownerName} FSM] Transitioned from [{oldKey}] -> [{newKey}]");
            }

            _currentState.Enter();
            OnStateChanged?.Invoke(oldKey, newKey);
        }

        /// <summary>
        /// Forwards Update loop execution to the active state.
        /// </summary>
        public void Update()
        {
            if (_currentState != null)
            {
                _currentState.Update();
            }
        }

        /// <summary>
        /// Forwards FixedUpdate physics loop execution to the active state.
        /// </summary>
        public void FixedUpdate()
        {
            if (_currentState != null)
            {
                _currentState.FixedUpdate();
            }
        }

        /// <summary>
        /// Returns whether a state key has been registered in the machine.
        /// </summary>
        public bool HasState(TKey key) => _states.ContainsKey(key);

        #endregion
    }

    /// <summary>
    /// Non-generic convenience wrapper of AIStateMachine keyed to standard AIStateType.
    /// </summary>
    public class AIStateMachine : AIStateMachine<AIStateType>
    {
        public AIStateMachine(string ownerName = "AI", bool enableDebugLogging = false)
            : base(ownerName, enableDebugLogging)
        {
        }
    }
}
