using System;
using UnityEngine;
using RageHunters.Core;

namespace RageHunters.Player
{
    /// <summary>
    /// Manages runtime player health, stamina, damage mitigation, death, and respawn.
    /// Emits local C# events and publishes to the global EventBus.
    /// Handles stamina consumption during sprinting and automatic regeneration.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerStats : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Health Settings")]
        [SerializeField]
        [Tooltip("Maximum health pool for the player.")]
        private float _maxHealth = GameConstants.PlayerDefaults.DefaultMaxHealth;

        [SerializeField]
        [Tooltip("Initial health when spawned.")]
        private float _initialHealth = GameConstants.PlayerDefaults.DefaultMaxHealth;

        [Header("Stamina Settings")]
        [SerializeField]
        [Tooltip("Maximum stamina pool for the player.")]
        private float _maxStamina = 100f;

        [SerializeField]
        [Tooltip("Rate at which stamina regenerates per second.")]
        private float _staminaRegenRate = 15f;

        [SerializeField]
        [Tooltip("Stamina consumed per second while sprinting.")]
        private float _sprintDrainRate = 20f;

        [SerializeField]
        [Tooltip("Delay in seconds before stamina begins regenerating after sprint or consumption.")]
        private float _staminaRegenDelay = 1.0f;

        [SerializeField]
        [Tooltip("Minimum stamina required to initiate a sprint.")]
        private float _minSprintStaminaThreshold = 5f;

        [Header("Identity")]
        [SerializeField]
        [Tooltip("Optional local or network player identifier.")]
        private ulong _playerId = 0;

        [SerializeField]
        [Tooltip("Whether this component represents the local client player.")]
        private bool _isLocalPlayer = true;

        #endregion

        #region Private Fields

        private float _currentHealth;
        private float _currentStamina;
        private bool _isAlive = true;
        private bool _isSprinting;
        private float _lastStaminaUseTime;

        #endregion

        #region Properties

        /// <summary>
        /// Current health of the player.
        /// </summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>
        /// Maximum possible health of the player.
        /// </summary>
        public float MaxHealth => _maxHealth;

        /// <summary>
        /// Health represented as a normalized ratio [0.0, 1.0].
        /// </summary>
        public float HealthPercent => _maxHealth > 0f ? Mathf.Clamp01(_currentHealth / _maxHealth) : 0f;

        /// <summary>
        /// Current stamina of the player.
        /// </summary>
        public float CurrentStamina => _currentStamina;

        /// <summary>
        /// Maximum possible stamina of the player.
        /// </summary>
        public float MaxStamina => _maxStamina;

        /// <summary>
        /// Stamina represented as a normalized ratio [0.0, 1.0].
        /// </summary>
        public float StaminaPercent => _maxStamina > 0f ? Mathf.Clamp01(_currentStamina / _maxStamina) : 0f;

        /// <summary>
        /// Indicates whether the player is currently alive.
        /// </summary>
        public bool IsAlive => _isAlive;

        /// <summary>
        /// Indicates whether the player has enough stamina to sprint.
        /// </summary>
        public bool CanSprint => _isAlive && _currentStamina > _minSprintStaminaThreshold;

        /// <summary>
        /// Player ID associated with this character.
        /// </summary>
        public ulong PlayerId
        {
            get => _playerId;
            set => _playerId = value;
        }

        /// <summary>
        /// Whether this stats instance belongs to the local player.
        /// </summary>
        public bool IsLocalPlayer
        {
            get => _isLocalPlayer;
            set => _isLocalPlayer = value;
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when health changes (currentHealth, maxHealth).
        /// </summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>
        /// Fired when stamina changes (currentStamina, maxStamina).
        /// </summary>
        public event Action<float, float> OnStaminaChanged;

        /// <summary>
        /// Fired when the player takes damage (amount, currentHealth).
        /// </summary>
        public event Action<float, float> OnDamageTaken;

        /// <summary>
        /// Fired when the player dies.
        /// </summary>
        public event Action OnDeath;

        /// <summary>
        /// Fired when the player respawns.
        /// </summary>
        public event Action OnRespawn;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _currentHealth = Mathf.Clamp(_initialHealth, 0f, _maxHealth);
            _currentStamina = _maxStamina;
            _isAlive = _currentHealth > 0f;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
        }

        private void Update()
        {
            if (!_isAlive) return;

            UpdateStamina(Time.deltaTime);
        }

        #endregion

        #region Stamina Management

        private void UpdateStamina(float deltaTime)
        {
            if (_isSprinting)
            {
                // Drain stamina while sprinting
                float drainAmount = _sprintDrainRate * deltaTime;
                _currentStamina = Mathf.Max(0f, _currentStamina - drainAmount);
                _lastStaminaUseTime = Time.time;
                OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);

                if (_currentStamina <= 0f)
                {
                    _isSprinting = false;
                }
            }
            else
            {
                // Regenerate stamina after delay
                if (Time.time >= _lastStaminaUseTime + _staminaRegenDelay && _currentStamina < _maxStamina)
                {
                    float regenAmount = _staminaRegenRate * deltaTime;
                    _currentStamina = Mathf.Min(_maxStamina, _currentStamina + regenAmount);
                    OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
                }
            }
        }

        /// <summary>
        /// Updates the sprint activity state. When true, stamina drains; when false, stamina can regenerate.
        /// </summary>
        public void SetSprinting(bool sprinting)
        {
            if (!_isAlive)
            {
                _isSprinting = false;
                return;
            }

            if (sprinting && _currentStamina > 0f)
            {
                _isSprinting = true;
            }
            else
            {
                _isSprinting = false;
            }
        }

        /// <summary>
        /// Consumes a fixed chunk of stamina (e.g. for jump, melee attack, dodge).
        /// Returns true if sufficient stamina was available and consumed.
        /// </summary>
        public bool TryConsumeStamina(float amount)
        {
            if (!_isAlive || _currentStamina < amount)
            {
                return false;
            }

            _currentStamina = Mathf.Max(0f, _currentStamina - amount);
            _lastStaminaUseTime = Time.time;
            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
            return true;
        }

        /// <summary>
        /// Directly drains stamina by the specified amount.
        /// </summary>
        public void DrainStamina(float amount)
        {
            if (!_isAlive || amount <= 0f) return;

            _currentStamina = Mathf.Max(0f, _currentStamina - amount);
            _lastStaminaUseTime = Time.time;
            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);

            if (_currentStamina <= 0f)
            {
                _isSprinting = false;
            }
        }

        #endregion

        #region Health & Damage

        /// <summary>
        /// Applies damage to the player.
        /// </summary>
        /// <param name="amount">Amount of incoming damage.</param>
        /// <param name="attackerId">Optional ID of the damage dealer.</param>
        /// <param name="damageSource">Description or source of the damage.</param>
        public void TakeDamage(float amount, ulong attackerId = 0, string damageSource = "Combat")
        {
            if (!_isAlive || amount <= 0f) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnDamageTaken?.Invoke(amount, _currentHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            // Publish global damage event
            EventBus.Publish(new DamageDealtEvent(
                attackerId,
                _playerId,
                amount,
                isCritical: false,
                hitPoint: transform.position
            ));

            if (_currentHealth <= 0f)
            {
                Die(attackerId, damageSource);
            }
        }

        /// <summary>
        /// Restores player health up to the maximum capacity.
        /// </summary>
        public void Heal(float amount)
        {
            if (!_isAlive || amount <= 0f) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// Kills the player entity and fires death events.
        /// </summary>
        public void Die(ulong killerId = 0, string damageSource = "Unknown")
        {
            if (!_isAlive) return;

            _isAlive = false;
            _currentHealth = 0f;
            _isSprinting = false;

            OnDeath?.Invoke();

            // Publish global death event
            EventBus.Publish(new PlayerDiedEvent(
                victimId: _playerId,
                killerId: killerId,
                damageSource: damageSource
            ));
        }

        /// <summary>
        /// Respawns the player, resets health and stamina to full, and optionally relocates to spawn position.
        /// </summary>
        public void Respawn(Vector3? spawnPosition = null)
        {
            _isAlive = true;
            _currentHealth = _maxHealth;
            _currentStamina = _maxStamina;
            _isSprinting = false;

            if (spawnPosition.HasValue)
            {
                transform.position = spawnPosition.Value;
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
            OnRespawn?.Invoke();

            // Publish global spawn event
            EventBus.Publish(new PlayerSpawnedEvent(
                _playerId,
                gameObject.name,
                transform.position,
                _isLocalPlayer
            ));
        }

        #endregion
    }
}
