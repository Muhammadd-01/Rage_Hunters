using System;
using System.Collections.Generic;
using UnityEngine;

namespace RageHunters.Missions
{
    /// <summary>
    /// Manages active missions, objectives, and extraction logic during a match.
    /// </summary>
    public class MissionManager : Core.Singleton<MissionManager>
    {
        #region Serialized Fields

        [Header("Mission Configuration")]
        [SerializeField] private float _matchDuration = 600f;
        [SerializeField] private int _objectivesRequired = 3;

        #endregion

        #region Private Fields

        private float _matchTimer;
        private bool _matchActive;
        private int _objectivesCompleted;
        private List<ObjectiveData> _activeObjectives = new();

        #endregion

        #region Events

        public event Action<float> OnMatchTimerUpdated;
        public event Action OnMatchStarted;
        public event Action<bool> OnMatchEnded;
        public event Action<ObjectiveData> OnObjectiveCompleted;
        public event Action OnAllObjectivesCompleted;
        public event Action OnExtractionAvailable;

        #endregion

        #region Properties

        public float MatchTimeRemaining => Mathf.Max(0f, _matchDuration - _matchTimer);
        public bool IsMatchActive => _matchActive;
        public int ObjectivesCompleted => _objectivesCompleted;
        public int ObjectivesRequired => _objectivesRequired;
        public bool ExtractionAvailable => _objectivesCompleted >= _objectivesRequired;
        public IReadOnlyList<ObjectiveData> ActiveObjectives => _activeObjectives;

        #endregion

        #region Public API

        /// <summary>
        /// Starts a new match with the specified duration and objectives.
        /// </summary>
        public void StartMatch(float duration = -1f, int objectivesRequired = -1)
        {
            if (duration > 0) _matchDuration = duration;
            if (objectivesRequired > 0) _objectivesRequired = objectivesRequired;

            _matchTimer = 0f;
            _objectivesCompleted = 0;
            _matchActive = true;
            _activeObjectives.Clear();

            OnMatchStarted?.Invoke();
            Core.EventBus.Publish(new Core.MatchStartedEvent("solo", _matchDuration, 1));

            Debug.Log($"[MissionManager] Match started. Duration: {_matchDuration}s, Objectives: {_objectivesRequired}");
        }

        /// <summary>
        /// Ends the current match with the given outcome.
        /// </summary>
        public void EndMatch(bool playerWon)
        {
            if (!_matchActive) return;

            _matchActive = false;
            OnMatchEnded?.Invoke(playerWon);
            Core.EventBus.Publish(new Core.MatchEndedEvent("solo", playerWon, _matchTimer));

            Debug.Log($"[MissionManager] Match ended. Won: {playerWon}, Time: {_matchTimer:F1}s");
        }

        /// <summary>
        /// Registers completion of an objective.
        /// </summary>
        public void CompleteObjective(ObjectiveData objective)
        {
            if (!_matchActive) return;

            _objectivesCompleted++;
            objective.IsCompleted = true;
            OnObjectiveCompleted?.Invoke(objective);

            Debug.Log($"[MissionManager] Objective completed: {objective.Title} ({_objectivesCompleted}/{_objectivesRequired})");

            if (_objectivesCompleted >= _objectivesRequired)
            {
                OnAllObjectivesCompleted?.Invoke();
                OnExtractionAvailable?.Invoke();
                Debug.Log("[MissionManager] All objectives completed — extraction available!");
            }
        }

        /// <summary>
        /// Called when the player reaches the extraction point.
        /// </summary>
        public void PlayerExtracted()
        {
            if (!_matchActive) return;

            if (ExtractionAvailable)
            {
                Core.EventBus.Publish(new Core.ExtractionCompletedEvent(0, "main"));
                EndMatch(true);
            }
            else
            {
                Debug.Log("[MissionManager] Cannot extract — objectives incomplete.");
            }
        }

        /// <summary>
        /// Called when the player dies.
        /// </summary>
        public void PlayerDied()
        {
            if (!_matchActive) return;
            EndMatch(false);
        }

        /// <summary>
        /// Adds an objective to the active list.
        /// </summary>
        public void AddObjective(ObjectiveData objective)
        {
            _activeObjectives.Add(objective);
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!_matchActive) return;

            _matchTimer += Time.deltaTime;
            OnMatchTimerUpdated?.Invoke(MatchTimeRemaining);

            if (_matchTimer >= _matchDuration)
            {
                EndMatch(false);
            }
        }

        #endregion
    }

    /// <summary>
    /// Data container for a mission objective.
    /// </summary>
    [Serializable]
    public class ObjectiveData
    {
        public string Title;
        public string Description;
        public ObjectiveType Type;
        public bool IsCompleted;
        public Vector3 Location;

        public enum ObjectiveType
        {
            CollectItem,
            EliminateTarget,
            ReachLocation,
            Survive,
            Interact
        }
    }
}
