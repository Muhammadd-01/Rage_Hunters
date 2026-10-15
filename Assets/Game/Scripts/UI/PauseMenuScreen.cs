using UnityEngine;
using UnityEngine.UI;
using RageHunters.Core;

namespace RageHunters.UI
{
    /// <summary>
    /// Modal overlay displayed when gameplay is paused.
    /// Provides options to resume match, adjust settings, or quit to main menu.
    /// Pauses engine timescale during solo matches.
    /// </summary>
    public sealed class PauseMenuScreen : UIScreen
    {
        #region Serialized Fields

        [Header("Pause Buttons")]
        [SerializeField]
        [Tooltip("Button to unpause and return to active gameplay.")]
        private Button _resumeButton;

        [SerializeField]
        [Tooltip("Button to open audio/video/control settings screen.")]
        private Button _settingsButton;

        [SerializeField]
        [Tooltip("Button to abandon current match and return to Main Menu.")]
        private Button _quitToMenuButton;

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Whether this pause screen should alter Time.timeScale to 0 during pause.")]
        private bool _pauseTimeScale = true;

        #endregion

        #region Private Fields

        private float _previousTimeScale = 1f;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            BindButtons();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnbindButtons();
            RestoreTimeScale();
        }

        #endregion

        #region Screen Hooks

        protected override void OnShown()
        {
            base.OnShown();

            if (_pauseTimeScale)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            RestoreTimeScale();
        }

        private void RestoreTimeScale()
        {
            if (_pauseTimeScale)
            {
                Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;
            }
        }

        #endregion

        #region Button Setup

        private void BindButtons()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (_quitToMenuButton != null)
            {
                _quitToMenuButton.onClick.AddListener(OnQuitToMenuClicked);
            }
        }

        private void UnbindButtons()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(OnResumeClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (_quitToMenuButton != null)
            {
                _quitToMenuButton.onClick.RemoveListener(OnQuitToMenuClicked);
            }
        }

        #endregion

        #region Button Action Handlers

        private void OnResumeClicked()
        {
            PlayClickSound();
            Debug.Log("[PauseMenuScreen] Resuming match.");

            RestoreTimeScale();

            if (GameManager.HasInstance && GameManager.Instance.CurrentState == GameManager.GameState.Paused)
            {
                GameManager.Instance.ChangeState(GameManager.GameState.InGame);
            }

            if (UIManager.HasInstance)
            {
                UIManager.Instance.Hide<PauseMenuScreen>(immediate: false);
            }
            else
            {
                Hide(immediate: false);
            }
        }

        private void OnSettingsClicked()
        {
            PlayClickSound();
            Debug.Log("[PauseMenuScreen] Opening settings overlay.");

            if (UIManager.HasInstance)
            {
                UIManager.Instance.Show<SettingsScreen>(immediate: false, hideCurrent: false);
            }
        }

        private void OnQuitToMenuClicked()
        {
            PlayClickSound();
            Debug.Log("[PauseMenuScreen] Quitting match back to Main Menu.");

            RestoreTimeScale();

            if (GameManager.HasInstance)
            {
                GameManager.Instance.EndMatch(localPlayerExtracted: false);
                GameManager.Instance.ResetMatch();
            }

            if (SceneLoader.HasInstance)
            {
                if (UIManager.HasInstance)
                {
                    UIManager.Instance.Show<LoadingScreen>(immediate: true, hideCurrent: true);
                }

                SceneLoader.Instance.LoadScene(GameConstants.Scenes.MainMenu, null, () =>
                {
                    if (GameManager.HasInstance)
                    {
                        GameManager.Instance.ChangeState(GameManager.GameState.MainMenu);
                    }
                });
            }
            else
            {
                if (UIManager.HasInstance)
                {
                    UIManager.Instance.HideAllScreens(immediate: true);
                    UIManager.Instance.Show<MainMenuScreen>(immediate: false);
                }
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
            // Back button while paused resumes the match
            OnResumeClicked();
            return true;
        }

        #endregion
    }
}
