using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RageHunters.Core;

namespace RageHunters.UI
{
    /// <summary>
    /// Screen allowing selection between available game modes:
    /// Solo Survival, Private Lobby, and Online Multiplayer Survival.
    /// </summary>
    public sealed class GameModeScreen : UIScreen
    {
        #region Serialized Fields

        [Header("Mode Buttons")]
        [SerializeField]
        [Tooltip("Button to launch instant Solo Survival match.")]
        private Button _soloSurvivalButton;

        [SerializeField]
        [Tooltip("Button to open Private Lobby host/join.")]
        private Button _privateLobbyButton;

        [SerializeField]
        [Tooltip("Button for Online Survival matchmaking (currently locked).")]
        private Button _onlineSurvivalButton;

        [SerializeField]
        [Tooltip("Return button to navigate back to Main Menu.")]
        private Button _backButton;

        [Header("Badges & Labels")]
        [SerializeField]
        [Tooltip("Badge or label marking Online Survival as Coming Soon.")]
        private GameObject _onlineComingSoonBadge;

        [SerializeField]
        [Tooltip("Status text for displaying mode information or notices.")]
        private TMP_Text _modeNoticeText;

        [Header("Match Parameters")]
        [SerializeField]
        [Tooltip("Target scene to load when launching Solo Survival.")]
        private string _gameMapSceneName = GameConstants.Scenes.GameMap;

        [SerializeField]
        [Tooltip("Match duration for Solo Survival in seconds.")]
        private float _soloMatchDuration = GameConstants.MatchDefaults.DefaultMatchDurationSeconds;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            BindButtons();
            ConfigureOnlineLockedState();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnbindButtons();
        }

        #endregion

        #region Button Setup

        private void BindButtons()
        {
            if (_soloSurvivalButton != null)
            {
                _soloSurvivalButton.onClick.AddListener(OnSoloSurvivalClicked);
            }

            if (_privateLobbyButton != null)
            {
                _privateLobbyButton.onClick.AddListener(OnPrivateLobbyClicked);
            }

            if (_onlineSurvivalButton != null)
            {
                _onlineSurvivalButton.onClick.AddListener(OnOnlineSurvivalClicked);
            }

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackClicked);
            }
        }

        private void UnbindButtons()
        {
            if (_soloSurvivalButton != null)
            {
                _soloSurvivalButton.onClick.RemoveListener(OnSoloSurvivalClicked);
            }

            if (_privateLobbyButton != null)
            {
                _privateLobbyButton.onClick.RemoveListener(OnPrivateLobbyClicked);
            }

            if (_onlineSurvivalButton != null)
            {
                _onlineSurvivalButton.onClick.RemoveListener(OnOnlineSurvivalClicked);
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
            }
        }

        private void ConfigureOnlineLockedState()
        {
            if (_onlineComingSoonBadge != null)
            {
                _onlineComingSoonBadge.SetActive(true);
            }

            if (_onlineSurvivalButton != null)
            {
                // Kept interactable so players can click and receive the "Coming Soon" prompt, or disabled
                _onlineSurvivalButton.interactable = true;
            }
        }

        #endregion

        #region Mode Actions

        private void OnSoloSurvivalClicked()
        {
            PlayClickSound();
            Debug.Log("[GameModeScreen] Solo Survival selected. Launching solo match immediately...");

            string matchId = $"SOLO_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

            if (GameManager.HasInstance)
            {
                GameManager.Instance.StartMatch(matchId, _gameMapSceneName, _soloMatchDuration, initialPlayerCount: 1);
            }

            if (SceneLoader.HasInstance)
            {
                if (UIManager.HasInstance)
                {
                    UIManager.Instance.Show<LoadingScreen>(immediate: false, hideCurrent: true);
                }

                SceneLoader.Instance.LoadScene(_gameMapSceneName, null, () =>
                {
                    Debug.Log("[GameModeScreen] Solo Survival scene loaded.");
                    if (GameManager.HasInstance)
                    {
                        GameManager.Instance.ChangeState(GameManager.GameState.InGame);
                    }
                });
            }
            else
            {
                // Fallback direct UI transition if running in single scene prototype
                if (UIManager.HasInstance)
                {
                    UIManager.Instance.Show<HUDScreen>(immediate: false, hideCurrent: true);
                }
            }
        }

        private void OnPrivateLobbyClicked()
        {
            PlayClickSound();
            Debug.Log("[GameModeScreen] Private Lobby selected.");

            if (_modeNoticeText != null)
            {
                _modeNoticeText.text = "Private Lobby: Matchmaking lobby system connecting...";
            }
        }

        private void OnOnlineSurvivalClicked()
        {
            PlayClickSound();
            Debug.Log("[GameModeScreen] Online Survival selected (Locked: Coming Soon).");

            if (_modeNoticeText != null)
            {
                _modeNoticeText.text = "Online Survival mode will be unlocked in the next update!";
            }
        }

        private void OnBackClicked()
        {
            PlayClickSound();
            if (UIManager.HasInstance)
            {
                UIManager.Instance.NavigateBack();
            }
        }

        private void PlayClickSound()
        {
            if (UIManager.HasInstance)
            {
                UIManager.Instance.PlayClickSound();
            }
        }

        #endregion

        #region Back Navigation

        public override bool OnBackButtonPressed()
        {
            OnBackClicked();
            return true;
        }

        #endregion
    }
}
