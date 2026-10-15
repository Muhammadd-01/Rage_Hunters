using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RageHunters.Core;

namespace RageHunters.UI
{
    /// <summary>
    /// Core in-game HUD overlay for active combat and survival gameplay.
    /// Displays health and stamina meters, match countdown, active objective,
    /// contextual interaction prompts, ammo and inventory counts, minimap container,
    /// and a dynamic kill feed.
    /// </summary>
    public sealed class HUDScreen : UIScreen
    {
        #region Serialized Fields

        [Header("Player Vitals (Top-Left)")]
        [SerializeField]
        [Tooltip("Health meter bar slider.")]
        private Slider _healthBar;

        [SerializeField]
        [Tooltip("Health meter numerical readout.")]
        private TMP_Text _healthText;

        [SerializeField]
        [Tooltip("Stamina meter bar slider.")]
        private Slider _staminaBar;

        [SerializeField]
        [Tooltip("Stamina meter numerical readout.")]
        private TMP_Text _staminaText;

        [Header("Minimap (Top-Right)")]
        [SerializeField]
        [Tooltip("Minimap UI parent / mask container.")]
        private RectTransform _minimapContainer;

        [SerializeField]
        [Tooltip("Minimap radar placeholder graphic.")]
        private Image _minimapPlaceholder;

        [Header("Objectives & Timer (Top-Center)")]
        [SerializeField]
        [Tooltip("Text displaying current match objective or mission directive.")]
        private TMP_Text _objectiveText;

        [SerializeField]
        [Tooltip("Text displaying remaining match survival time.")]
        private TMP_Text _matchTimerText;

        [Header("Contextual Interaction (Center)")]
        [SerializeField]
        [Tooltip("Root GameObject for contextual interaction prompts.")]
        private GameObject _interactionPromptRoot;

        [SerializeField]
        [Tooltip("Text displaying interaction label (e.g., 'Tap to Loot Supply Crate').")]
        private TMP_Text _interactionPromptText;

        [Header("Weapons & Items (Bottom-Right)")]
        [SerializeField]
        [Tooltip("Text displaying active weapon ammo counter [current / reserve].")]
        private TMP_Text _ammoText;

        [SerializeField]
        [Tooltip("Icon displaying active weapon thumbnail.")]
        private Image _currentWeaponIcon;

        [SerializeField]
        [Tooltip("Text displaying quick-use item or grenade count.")]
        private TMP_Text _itemCountText;

        [Header("Kill Feed (Right Side)")]
        [SerializeField]
        [Tooltip("Container holding kill feed text entries.")]
        private RectTransform _killFeedContainer;

        [SerializeField]
        [Tooltip("Prefab or template item for kill feed line entry.")]
        private TMP_Text _killFeedEntryPrefab;

        [SerializeField]
        [Tooltip("Maximum kill feed entries displayed simultaneously.")]
        private int _maxKillFeedEntries = 5;

        [SerializeField]
        [Tooltip("Lifetime of each kill feed message in seconds.")]
        private float _killFeedEntryLifetime = 4.0f;

        [Header("HUD Actions")]
        [SerializeField]
        [Tooltip("Button to pause gameplay and open pause menu.")]
        private Button _pauseButton;

        #endregion

        #region Private Fields

        private readonly List<KillFeedItem> _activeKillFeedItems = new();
        private float _remainingMatchTime;
        private bool _isTimerActive;

        private struct KillFeedItem
        {
            public TMP_Text TextElement;
            public float RemainingTime;
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(OnPauseClicked);
            }

            InitializePlaceholders();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            UpdateMatchTimer();
            UpdateKillFeed();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveListener(OnPauseClicked);
            }
        }

        #endregion

        #region Initialization & Placeholder Setup

        private void InitializePlaceholders()
        {
            SetHealth(GameConstants.PlayerDefaults.DefaultMaxHealth, GameConstants.PlayerDefaults.DefaultMaxHealth);
            SetStamina(100f, 100f);
            SetObjective("Locate extraction beacon before storm collapses.");
            SetMatchTime(GameConstants.MatchDefaults.DefaultMatchDurationSeconds);
            SetAmmo(30, 90);
            SetItemCount(3);
            HideInteractionPrompt();

            if (_killFeedEntryPrefab != null)
            {
                _killFeedEntryPrefab.gameObject.SetActive(false);
            }
        }

        private void SubscribeEvents()
        {
            EventBus.Subscribe<PlayerDiedEvent>(HandlePlayerDied);
            EventBus.Subscribe<DamageDealtEvent>(HandleDamageDealt);
            EventBus.Subscribe<MatchStartedEvent>(HandleMatchStarted);
            EventBus.Subscribe<MatchEndedEvent>(HandleMatchEnded);
        }

        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<PlayerDiedEvent>(HandlePlayerDied);
            EventBus.Unsubscribe<DamageDealtEvent>(HandleDamageDealt);
            EventBus.Unsubscribe<MatchStartedEvent>(HandleMatchStarted);
            EventBus.Unsubscribe<MatchEndedEvent>(HandleMatchEnded);
        }

        #endregion

        #region Public Vitals API

        /// <summary>
        /// Updates the HUD health bar and numerical readout.
        /// </summary>
        public void SetHealth(float current, float max)
        {
            float clampedMax = Mathf.Max(1f, max);
            float ratio = Mathf.Clamp01(current / clampedMax);

            if (_healthBar != null)
            {
                _healthBar.value = ratio;
            }

            if (_healthText != null)
            {
                _healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(clampedMax)}";
            }
        }

        /// <summary>
        /// Updates the HUD stamina bar and numerical readout.
        /// </summary>
        public void SetStamina(float current, float max)
        {
            float clampedMax = Mathf.Max(1f, max);
            float ratio = Mathf.Clamp01(current / clampedMax);

            if (_staminaBar != null)
            {
                _staminaBar.value = ratio;
            }

            if (_staminaText != null)
            {
                _staminaText.text = $"{Mathf.CeilToInt(current)}";
            }
        }

        #endregion

        #region Objectives & Match Timer

        /// <summary>
        /// Updates the match objective instruction string.
        /// </summary>
        public void SetObjective(string objective)
        {
            if (_objectiveText != null)
            {
                _objectiveText.text = objective ?? string.Empty;
            }
        }

        /// <summary>
        /// Sets the remaining match time in seconds and starts the countdown.
        /// </summary>
        public void SetMatchTime(float remainingSeconds)
        {
            _remainingMatchTime = Mathf.Max(0f, remainingSeconds);
            _isTimerActive = true;
            FormatAndDisplayTimer();
        }

        private void UpdateMatchTimer()
        {
            if (!_isTimerActive) return;

            if (GameManager.HasInstance && GameManager.Instance.CurrentMatch.IsActive)
            {
                _remainingMatchTime = GameManager.Instance.CurrentMatch.RemainingTime;
            }
            else
            {
                _remainingMatchTime = Mathf.Max(0f, _remainingMatchTime - Time.deltaTime);
            }

            FormatAndDisplayTimer();
        }

        private void FormatAndDisplayTimer()
        {
            if (_matchTimerText == null) return;

            int minutes = Mathf.FloorToInt(_remainingMatchTime / 60f);
            int seconds = Mathf.FloorToInt(_remainingMatchTime % 60f);
            _matchTimerText.text = $"{minutes:00}:{seconds:00}";
        }

        #endregion

        #region Contextual Interaction

        /// <summary>
        /// Shows the contextual interaction prompt in the center of the screen.
        /// </summary>
        public void ShowInteractionPrompt(string prompt)
        {
            if (_interactionPromptRoot != null)
            {
                _interactionPromptRoot.SetActive(true);
            }

            if (_interactionPromptText != null)
            {
                _interactionPromptText.text = prompt ?? string.Empty;
            }
        }

        /// <summary>
        /// Hides the contextual interaction prompt.
        /// </summary>
        public void HideInteractionPrompt()
        {
            if (_interactionPromptRoot != null)
            {
                _interactionPromptRoot.SetActive(false);
            }
        }

        #endregion

        #region Weapon & Inventory API

        /// <summary>
        /// Updates weapon ammunition indicators.
        /// </summary>
        public void SetAmmo(int currentMagazine, int reserveAmmo)
        {
            if (_ammoText != null)
            {
                _ammoText.text = $"{currentMagazine} / {reserveAmmo}";
            }
        }

        /// <summary>
        /// Updates weapon icon graphic.
        /// </summary>
        public void SetWeaponIcon(Sprite weaponIcon)
        {
            if (_currentWeaponIcon != null)
            {
                _currentWeaponIcon.sprite = weaponIcon;
                _currentWeaponIcon.gameObject.SetActive(weaponIcon != null);
            }
        }

        /// <summary>
        /// Updates auxiliary quick-item count.
        /// </summary>
        public void SetItemCount(int count)
        {
            if (_itemCountText != null)
            {
                _itemCountText.text = $"x{count}";
            }
        }

        #endregion

        #region Kill Feed API

        /// <summary>
        /// Appends a new combat event to the on-screen kill feed.
        /// </summary>
        public void AddKillFeedEntry(string killer, string victim, string weapon = "")
        {
            if (_killFeedContainer == null || _killFeedEntryPrefab == null) return;

            // Trim oldest if exceeding limit
            if (_activeKillFeedItems.Count >= _maxKillFeedEntries)
            {
                var oldest = _activeKillFeedItems[0];
                if (oldest.TextElement != null)
                {
                    Destroy(oldest.TextElement.gameObject);
                }
                _activeKillFeedItems.RemoveAt(0);
            }

            var entryObj = Instantiate(_killFeedEntryPrefab, _killFeedContainer);
            entryObj.gameObject.SetActive(true);

            string weaponDescriptor = string.IsNullOrEmpty(weapon) ? "eliminated" : $"[{weapon}]";
            entryObj.text = $"<color=#FF5555>{killer}</color> {weaponDescriptor} <color=#AAAAAA>{victim}</color>";

            _activeKillFeedItems.Add(new KillFeedItem
            {
                TextElement = entryObj,
                RemainingTime = _killFeedEntryLifetime
            });
        }

        private void UpdateKillFeed()
        {
            for (int i = _activeKillFeedItems.Count - 1; i >= 0; i--)
            {
                var item = _activeKillFeedItems[i];
                item.RemainingTime -= Time.deltaTime;

                if (item.RemainingTime <= 0f)
                {
                    if (item.TextElement != null)
                    {
                        Destroy(item.TextElement.gameObject);
                    }
                    _activeKillFeedItems.RemoveAt(i);
                }
                else
                {
                    _activeKillFeedItems[i] = item;
                }
            }
        }

        #endregion

        #region Event Handlers

        private void HandlePlayerDied(PlayerDiedEvent evt)
        {
            string killer = $"Player_{evt.KillerId}";
            string victim = $"Player_{evt.VictimId}";
            AddKillFeedEntry(killer, victim, evt.DamageSource);
        }

        private void HandleDamageDealt(DamageDealtEvent evt)
        {
            // Can be used for damage direction indicators or hit markers
        }

        private void HandleMatchStarted(MatchStartedEvent evt)
        {
            SetMatchTime(evt.DurationSeconds);
        }

        private void HandleMatchEnded(MatchEndedEvent evt)
        {
            _isTimerActive = false;
        }

        private void OnPauseClicked()
        {
            if (UIManager.HasInstance)
            {
                UIManager.Instance.PlayClickSound();
                UIManager.Instance.Show<PauseMenuScreen>(immediate: false, hideCurrent: false);
            }

            if (GameManager.HasInstance)
            {
                GameManager.Instance.ChangeState(GameManager.GameState.Paused);
            }
        }

        #endregion

        #region Back Navigation

        public override bool OnBackButtonPressed()
        {
            // Back button in gameplay toggles pause
            OnPauseClicked();
            return true;
        }

        #endregion
    }
}
