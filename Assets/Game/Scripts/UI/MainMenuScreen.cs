using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RageHunters.Core;

namespace RageHunters.UI
{
    /// <summary>
    /// Primary navigation hub screen for RAGE HUNTERS.
    /// Provides access to game mode selection, villain customization, player loadout,
    /// settings menu, and application quit.
    /// </summary>
    public sealed class MainMenuScreen : UIScreen
    {
        #region Serialized Fields

        [Header("Menu Navigation Buttons")]
        [SerializeField]
        [Tooltip("Button to navigate to Game Mode selection.")]
        private Button _playButton;

        [SerializeField]
        [Tooltip("Button to navigate to Villain Creator / Character Customization.")]
        private Button _villainCreatorButton;

        [SerializeField]
        [Tooltip("Button to navigate to Player Loadout.")]
        private Button _loadoutButton;

        [SerializeField]
        [Tooltip("Button to navigate to Game Settings.")]
        private Button _settingsButton;

        [SerializeField]
        [Tooltip("Button to exit the application.")]
        private Button _quitButton;

        [Header("Player Header Profile (Optional)")]
        [SerializeField]
        [Tooltip("Text displaying player nickname.")]
        private TMP_Text _playerNameText;

        [SerializeField]
        [Tooltip("Text displaying player level.")]
        private TMP_Text _playerLevelText;

        [SerializeField]
        [Tooltip("Text displaying player currency / bounty tokens.")]
        private TMP_Text _currencyText;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            BindButtons();
        }

        private void Start()
        {
            RefreshPlayerProfileHeader();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnbindButtons();
        }

        #endregion

        #region Button Setup & Binding

        private void BindButtons()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayClicked);
            }

            if (_villainCreatorButton != null)
            {
                _villainCreatorButton.onClick.AddListener(OnVillainCreatorClicked);
            }

            if (_loadoutButton != null)
            {
                _loadoutButton.onClick.AddListener(OnLoadoutClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void UnbindButtons()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
            }

            if (_villainCreatorButton != null)
            {
                _villainCreatorButton.onClick.RemoveListener(OnVillainCreatorClicked);
            }

            if (_loadoutButton != null)
            {
                _loadoutButton.onClick.RemoveListener(OnLoadoutClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }

        #endregion

        #region Button Action Handlers

        private void OnPlayClicked()
        {
            PlayAudioFeedback();
            Debug.Log("[MainMenuScreen] Play clicked. Opening GameModeScreen.");

            if (UIManager.HasInstance)
            {
                UIManager.Instance.Show<GameModeScreen>(immediate: false, hideCurrent: true);
            }
        }

        private void OnVillainCreatorClicked()
        {
            PlayAudioFeedback();
            Debug.Log("[MainMenuScreen] Villain Creator clicked. Opening villain customization.");

            // Navigation point for Villain Creator system milestone
        }

        private void OnLoadoutClicked()
        {
            PlayAudioFeedback();
            Debug.Log("[MainMenuScreen] Loadout clicked. Opening weapon and gear loadout.");

            // Navigation point for Loadout system milestone
        }

        private void OnSettingsClicked()
        {
            PlayAudioFeedback();
            Debug.Log("[MainMenuScreen] Settings clicked. Opening SettingsScreen.");

            if (UIManager.HasInstance)
            {
                UIManager.Instance.Show<SettingsScreen>(immediate: false, hideCurrent: false);
            }
        }

        private void OnQuitClicked()
        {
            PlayAudioFeedback();
            Debug.Log("[MainMenuScreen] Quit clicked. Terminating application.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void PlayAudioFeedback()
        {
            if (UIManager.HasInstance)
            {
                UIManager.Instance.PlayClickSound();
            }
        }

        #endregion

        #region Profile Display

        /// <summary>
        /// Updates the profile header with player stats.
        /// </summary>
        public void RefreshPlayerProfileHeader(string playerName = "HUNTER_01", int level = 1, int currency = 500)
        {
            if (_playerNameText != null)
            {
                _playerNameText.text = playerName;
            }

            if (_playerLevelText != null)
            {
                _playerLevelText.text = $"LVL {level}";
            }

            if (_currencyText != null)
            {
                _currencyText.text = $"{currency} CR";
            }
        }

        #endregion

        #region Back Navigation

        public override bool OnBackButtonPressed()
        {
            // On main menu, pressing back prompts quit or exits
            OnQuitClicked();
            return true;
        }

        #endregion
    }
}
