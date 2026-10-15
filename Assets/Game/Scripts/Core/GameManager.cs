using System;
using UnityEngine;

namespace RageHunters.Core
{
    /// <summary>
    /// Central manager overseeing high-level application and gameplay states,
    /// match lifecycle, and mobile app lifecycle callbacks (pause, focus, quit).
    /// </summary>
    public sealed class GameManager : Singleton<GameManager>
    {
        #region State & Match Data Structures

        /// <summary>
        /// Defines all primary game lifecycle states.
        /// </summary>
        public enum GameState
        {
            Initializing,
            MainMenu,
            Loading,
            InGame,
            Paused,
            MatchEnd,
            Disconnected
        }

        /// <summary>
        /// Information regarding the active or most recent match session.
        /// </summary>
        [Serializable]
        public struct MatchInfo
        {
            public string MatchId;
            public string MapName;
            public float ElapsedTime;
            public float RemainingTime;
            public int CurrentPlayerCount;
            public bool IsActive;

            public static MatchInfo Empty => new()
            {
                MatchId = string.Empty,
                MapName = string.Empty,
                ElapsedTime = 0f,
                RemainingTime = 0f,
                CurrentPlayerCount = 0,
                IsActive = false
            };
        }

        #endregion

        #region Serialized Fields

        [Header("Initial Configuration")]
        [SerializeField]
        [Tooltip("State initialized when GameManager awakens.")]
        private GameState _currentState = GameState.Initializing;

        [SerializeField]
        [Tooltip("Target framerate for mobile rendering.")]
        private int _targetFrameRate = 60;

        #endregion

        #region Private Fields

        private GameState _previousState = GameState.Initializing;
        private MatchInfo _currentMatchInfo = MatchInfo.Empty;

        #endregion

        #region Events

        /// <summary>
        /// Delegate event fired whenever the game state changes.
        /// </summary>
        public event Action<GameState, GameState> OnGameStateChanged;

        /// <summary>
        /// Fired when an active match is initialized or updated.
        /// </summary>
        public event Action<MatchInfo> OnMatchInfoUpdated;

        /// <summary>
        /// Fired when application pause status changes (e.g., mobile backgrounding).
        /// </summary>
        public event Action<bool> OnApplicationPauseStatusChanged;

        /// <summary>
        /// Fired when application focus status changes.
        /// </summary>
        public event Action<bool> OnApplicationFocusStatusChanged;

        #endregion

        #region Properties

        public GameState CurrentState => _currentState;
        public GameState PreviousState => _previousState;
        public MatchInfo CurrentMatch => _currentMatchInfo;
        public bool IsInMatch => _currentState == GameState.InGame && _currentMatchInfo.IsActive;

        #endregion

        #region Unity Lifecycle

        protected override void OnInitialized()
        {
            base.OnInitialized();

            Application.targetFrameRate = _targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Update()
        {
            if (_currentState == GameState.InGame && _currentMatchInfo.IsActive)
            {
                _currentMatchInfo.ElapsedTime += Time.deltaTime;
                if (_currentMatchInfo.RemainingTime > 0f)
                {
                    _currentMatchInfo.RemainingTime = Mathf.Max(0f, _currentMatchInfo.RemainingTime - Time.deltaTime);
                }
            }
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            ChangeState(GameState.Disconnected);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            OnApplicationPauseStatusChanged?.Invoke(pauseStatus);

            if (pauseStatus)
            {
                // Entering background
                if (_currentState == GameState.InGame)
                {
                    ChangeState(GameState.Paused);
                }
            }
            else
            {
                // Resuming foreground
                if (_currentState == GameState.Paused)
                {
                    ChangeState(GameState.InGame);
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            OnApplicationFocusStatusChanged?.Invoke(hasFocus);
        }

        #endregion

        #region State Management

        /// <summary>
        /// Transitions the game to a new lifecycle state if different from current.
        /// </summary>
        /// <param name="newState">The target state to switch to.</param>
        public void ChangeState(GameState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            _previousState = _currentState;
            _currentState = newState;

            Debug.Log($"[GameManager] Game State changed: {_previousState} -> {_currentState}");

            OnGameStateChanged?.Invoke(_previousState, _currentState);
            EventBus.Publish(new GameStateChangedEvent(_previousState, _currentState));
        }

        #endregion

        #region Match Management

        /// <summary>
        /// Starts tracking a new match session.
        /// </summary>
        public void StartMatch(string matchId, string mapName, float matchDuration, int initialPlayerCount)
        {
            _currentMatchInfo = new MatchInfo
            {
                MatchId = matchId,
                MapName = mapName,
                ElapsedTime = 0f,
                RemainingTime = matchDuration,
                CurrentPlayerCount = initialPlayerCount,
                IsActive = true
            };

            ChangeState(GameState.InGame);
            OnMatchInfoUpdated?.Invoke(_currentMatchInfo);
            EventBus.Publish(new MatchStartedEvent(matchId, matchDuration, initialPlayerCount));
        }

        /// <summary>
        /// Updates the current active player count in match.
        /// </summary>
        public void UpdatePlayerCount(int playerCount)
        {
            if (!_currentMatchInfo.IsActive) return;

            _currentMatchInfo.CurrentPlayerCount = playerCount;
            OnMatchInfoUpdated?.Invoke(_currentMatchInfo);
        }

        /// <summary>
        /// Ends the current active match.
        /// </summary>
        public void EndMatch(bool localPlayerExtracted)
        {
            if (!_currentMatchInfo.IsActive) return;

            float duration = _currentMatchInfo.ElapsedTime;
            string matchId = _currentMatchInfo.MatchId;

            _currentMatchInfo.IsActive = false;
            ChangeState(GameState.MatchEnd);

            OnMatchInfoUpdated?.Invoke(_currentMatchInfo);
            EventBus.Publish(new MatchEndedEvent(matchId, localPlayerExtracted, duration));
        }

        /// <summary>
        /// Resets the current match state back to empty.
        /// </summary>
        public void ResetMatch()
        {
            _currentMatchInfo = MatchInfo.Empty;
            OnMatchInfoUpdated?.Invoke(_currentMatchInfo);
        }

        #endregion
    }
}
