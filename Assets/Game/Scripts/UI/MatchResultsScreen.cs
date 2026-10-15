using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace RageHunters.UI
{
    /// <summary>
    /// Displays match results after a game session ends.
    /// Shows outcome, XP earned, items collected, survival time, and kills.
    /// </summary>
    public class MatchResultsScreen : UIScreen
    {
        #region Serialized Fields

        [Header("Result Display")]
        [SerializeField] private TextMeshProUGUI _outcomeText;
        [SerializeField] private TextMeshProUGUI _xpEarnedText;
        [SerializeField] private TextMeshProUGUI _itemsCollectedText;
        [SerializeField] private TextMeshProUGUI _survivalTimeText;
        [SerializeField] private TextMeshProUGUI _killsText;
        [SerializeField] private TextMeshProUGUI _rankText;

        [Header("Buttons")]
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _playAgainButton;

        [Header("Visual")]
        [SerializeField] private Image _outcomeIcon;
        [SerializeField] private Color _victoryColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _defeatColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color _extractedColor = new Color(0.2f, 0.6f, 0.9f);

        #endregion

        #region Data

        /// <summary>
        /// Data structure for match results display.
        /// </summary>
        public struct MatchResultData
        {
            public MatchOutcome Outcome;
            public int XPEarned;
            public int ItemsCollected;
            public float SurvivalTimeSeconds;
            public int Kills;
            public int Rank;
            public int TotalPlayers;
        }

        public enum MatchOutcome
        {
            Extracted,
            Eliminated,
            Victory,
            Defeat,
            TimedOut
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinuePressed);

            if (_playAgainButton != null)
                _playAgainButton.onClick.AddListener(OnPlayAgainPressed);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_continueButton != null)
                _continueButton.onClick.RemoveListener(OnContinuePressed);

            if (_playAgainButton != null)
                _playAgainButton.onClick.RemoveListener(OnPlayAgainPressed);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Populates the results screen with match data and shows it.
        /// </summary>
        public void ShowResults(MatchResultData data)
        {
            PopulateUI(data);
            Show();
        }

        #endregion

        #region UI Population

        private void PopulateUI(MatchResultData data)
        {
            if (_outcomeText != null)
            {
                _outcomeText.text = GetOutcomeString(data.Outcome);
                _outcomeText.color = GetOutcomeColor(data.Outcome);
            }

            if (_xpEarnedText != null)
                _xpEarnedText.text = $"+{data.XPEarned} XP";

            if (_itemsCollectedText != null)
                _itemsCollectedText.text = data.ItemsCollected.ToString();

            if (_survivalTimeText != null)
            {
                int minutes = Mathf.FloorToInt(data.SurvivalTimeSeconds / 60f);
                int seconds = Mathf.FloorToInt(data.SurvivalTimeSeconds % 60f);
                _survivalTimeText.text = $"{minutes:00}:{seconds:00}";
            }

            if (_killsText != null)
                _killsText.text = data.Kills.ToString();

            if (_rankText != null)
            {
                if (data.TotalPlayers > 0)
                    _rankText.text = $"#{data.Rank} / {data.TotalPlayers}";
                else
                    _rankText.text = "";
            }

            if (_outcomeIcon != null)
                _outcomeIcon.color = GetOutcomeColor(data.Outcome);
        }

        private string GetOutcomeString(MatchOutcome outcome)
        {
            return outcome switch
            {
                MatchOutcome.Extracted => "EXTRACTED",
                MatchOutcome.Eliminated => "ELIMINATED",
                MatchOutcome.Victory => "VICTORY",
                MatchOutcome.Defeat => "DEFEAT",
                MatchOutcome.TimedOut => "TIME'S UP",
                _ => "MATCH OVER"
            };
        }

        private Color GetOutcomeColor(MatchOutcome outcome)
        {
            return outcome switch
            {
                MatchOutcome.Extracted => _extractedColor,
                MatchOutcome.Victory => _victoryColor,
                MatchOutcome.Eliminated => _defeatColor,
                MatchOutcome.Defeat => _defeatColor,
                MatchOutcome.TimedOut => _defeatColor,
                _ => Color.white
            };
        }

        #endregion

        #region Button Handlers

        private void OnContinuePressed()
        {
            Hide();
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.ChangeState(Core.GameManager.GameState.MainMenu);
            }
            if (Core.SceneLoader.Instance != null)
            {
                Core.SceneLoader.Instance.LoadScene(Core.GameConstants.Scenes.MainMenu);
            }
        }

        private void OnPlayAgainPressed()
        {
            Hide();
            if (Core.SceneLoader.Instance != null)
            {
                Core.SceneLoader.Instance.LoadScene(Core.GameConstants.Scenes.GameMap);
            }
        }

        #endregion

        #region Back Navigation

        public override bool OnBackButtonPressed()
        {
            OnContinuePressed();
            return true;
        }

        #endregion
    }
}
