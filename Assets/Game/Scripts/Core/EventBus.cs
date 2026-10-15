using System;
using System.Collections.Generic;
using UnityEngine;

namespace RageHunters.Core
{
    /// <summary>
    /// Lightweight, type-safe, allocation-free event bus using struct events.
    /// Provides pub/sub decoupling across game systems.
    /// </summary>
    public static class EventBus
    {
        #region Internal Handler Storage

        private interface IEventBinding
        {
            void Clear();
        }

        private sealed class EventBinding<T> : IEventBinding where T : struct
        {
            public Action<T> Callbacks;

            public void Clear()
            {
                Callbacks = null;
            }
        }

        private static readonly Dictionary<Type, IEventBinding> _bindings = new();
        private static readonly object _lock = new();

        #endregion

        #region Public API

        /// <summary>
        /// Subscribes a listener callback to event type T.
        /// </summary>
        public static void Subscribe<T>(Action<T> callback) where T : struct
        {
            if (callback == null) return;

            lock (_lock)
            {
                var type = typeof(T);
                if (!_bindings.TryGetValue(type, out var binding))
                {
                    var newBinding = new EventBinding<T>();
                    newBinding.Callbacks += callback;
                    _bindings[type] = newBinding;
                }
                else
                {
                    var typedBinding = (EventBinding<T>)binding;
                    typedBinding.Callbacks += callback;
                }
            }
        }

        /// <summary>
        /// Unsubscribes a listener callback from event type T.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> callback) where T : struct
        {
            if (callback == null) return;

            lock (_lock)
            {
                var type = typeof(T);
                if (_bindings.TryGetValue(type, out var binding))
                {
                    var typedBinding = (EventBinding<T>)binding;
                    typedBinding.Callbacks -= callback;
                }
            }
        }

        /// <summary>
        /// Publishes an event instance of type T to all subscribed listeners.
        /// </summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            Action<T> invocationList = null;

            lock (_lock)
            {
                var type = typeof(T);
                if (_bindings.TryGetValue(type, out var binding))
                {
                    invocationList = ((EventBinding<T>)binding).Callbacks;
                }
            }

            try
            {
                invocationList?.Invoke(eventData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventBus] Exception caught while executing event handler for {typeof(T).Name}: {ex}");
            }
        }

        /// <summary>
        /// Clears all subscribed listeners for all event types.
        /// Typically invoked on match cleanup or game reset.
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                foreach (var binding in _bindings.Values)
                {
                    binding.Clear();
                }
                _bindings.Clear();
            }
        }

        #endregion
    }

    #region Concrete Event Definitions

    /// <summary>
    /// Fired when the high-level game state transitions (e.g. MainMenu -> InGame).
    /// </summary>
    public readonly struct GameStateChangedEvent
    {
        public readonly GameManager.GameState PreviousState;
        public readonly GameManager.GameState NewState;

        public GameStateChangedEvent(GameManager.GameState previousState, GameManager.GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    /// <summary>
    /// Fired when a player entity is spawned into the match.
    /// </summary>
    public readonly struct PlayerSpawnedEvent
    {
        public readonly ulong PlayerId;
        public readonly string PlayerName;
        public readonly Vector3 Position;
        public readonly bool IsLocalPlayer;

        public PlayerSpawnedEvent(ulong playerId, string playerName, Vector3 position, bool isLocalPlayer)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            Position = position;
            IsLocalPlayer = isLocalPlayer;
        }
    }

    /// <summary>
    /// Fired when a player is killed in the match.
    /// </summary>
    public readonly struct PlayerDiedEvent
    {
        public readonly ulong VictimId;
        public readonly ulong KillerId;
        public readonly string DamageSource;

        public PlayerDiedEvent(ulong victimId, ulong killerId, string damageSource)
        {
            VictimId = victimId;
            KillerId = killerId;
            DamageSource = damageSource;
        }
    }

    /// <summary>
    /// Fired when a match officially starts.
    /// </summary>
    public readonly struct MatchStartedEvent
    {
        public readonly string MatchId;
        public readonly float DurationSeconds;
        public readonly int PlayerCount;

        public MatchStartedEvent(string matchId, float durationSeconds, int playerCount)
        {
            MatchId = matchId;
            DurationSeconds = durationSeconds;
            PlayerCount = playerCount;
        }
    }

    /// <summary>
    /// Fired when a match concludes.
    /// </summary>
    public readonly struct MatchEndedEvent
    {
        public readonly string MatchId;
        public readonly bool LocalPlayerExtracted;
        public readonly float MatchDuration;

        public MatchEndedEvent(string matchId, bool localPlayerExtracted, float matchDuration)
        {
            MatchId = matchId;
            LocalPlayerExtracted = localPlayerExtracted;
            MatchDuration = matchDuration;
        }
    }

    /// <summary>
    /// Fired when an item is collected from the world.
    /// </summary>
    public readonly struct ItemPickedUpEvent
    {
        public readonly ulong PlayerId;
        public readonly string ItemId;
        public readonly int Quantity;

        public ItemPickedUpEvent(ulong playerId, string itemId, int quantity)
        {
            PlayerId = playerId;
            ItemId = itemId;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// Fired when damage is dealt between combatants.
    /// </summary>
    public readonly struct DamageDealtEvent
    {
        public readonly ulong SourceId;
        public readonly ulong TargetId;
        public readonly float Amount;
        public readonly bool IsCritical;
        public readonly Vector3 HitPoint;

        public DamageDealtEvent(ulong sourceId, ulong targetId, float amount, bool isCritical, Vector3 hitPoint)
        {
            SourceId = sourceId;
            TargetId = targetId;
            Amount = amount;
            IsCritical = isCritical;
            HitPoint = hitPoint;
        }
    }

    /// <summary>
    /// Fired when a player initiates extraction at an extraction beacon.
    /// </summary>
    public readonly struct ExtractionStartedEvent
    {
        public readonly ulong PlayerId;
        public readonly string ExtractionZoneId;
        public readonly float CountdownSeconds;

        public ExtractionStartedEvent(ulong playerId, string extractionZoneId, float countdownSeconds)
        {
            PlayerId = playerId;
            ExtractionZoneId = extractionZoneId;
            CountdownSeconds = countdownSeconds;
        }
    }

    /// <summary>
    /// Fired when an extraction successfully completes.
    /// </summary>
    public readonly struct ExtractionCompletedEvent
    {
        public readonly ulong PlayerId;
        public readonly string ExtractionZoneId;

        public ExtractionCompletedEvent(ulong playerId, string extractionZoneId)
        {
            PlayerId = playerId;
            ExtractionZoneId = extractionZoneId;
        }
    }

    #endregion
}
