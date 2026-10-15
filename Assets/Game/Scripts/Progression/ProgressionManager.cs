using System;
using UnityEngine;

namespace RageHunters.Progression
{
    /// <summary>
    /// Manages player XP, level progression, and match statistics.
    /// </summary>
    public class ProgressionManager : Core.Singleton<ProgressionManager>
    {
        #region Constants

        private const int MAX_LEVEL = 50;
        private const int BASE_XP_PER_LEVEL = 200;
        private const float XP_SCALING = 1.15f;

        // XP awards
        public const int XP_OBJECTIVE_COMPLETE = 100;
        public const int XP_VILLAIN_ELIMINATED = 75;
        public const int XP_EXTRACTION_SUCCESS = 150;
        public const int XP_SURVIVE_5_MIN = 25;
        public const int XP_LOOT_COLLECTED = 10;
        public const int XP_TEAM_REVIVE = 50;

        #endregion

        #region Events

        public event Action<int> OnXPGained;
        public event Action<int> OnLevelUp;
        public event Action<MatchStats> OnMatchStatsRecorded;

        #endregion

        #region Properties

        public int CurrentLevel { get; private set; } = 1;
        public int CurrentXP { get; private set; }
        public int TotalXP { get; private set; }
        public int XPToNextLevel => CalculateXPForLevel(CurrentLevel + 1);
        public float LevelProgress => (float)CurrentXP / Mathf.Max(1, XPToNextLevel);

        // Lifetime stats
        public int TotalMatchesPlayed { get; private set; }
        public int TotalWins { get; private set; }
        public int TotalKills { get; private set; }
        public int TotalDeaths { get; private set; }
        public float TotalPlayTime { get; private set; }

        #endregion

        #region Public API

        /// <summary>
        /// Awards XP and handles level-up if threshold is exceeded.
        /// </summary>
        public void AddXP(int amount)
        {
            if (amount <= 0) return;

            CurrentXP += amount;
            TotalXP += amount;
            OnXPGained?.Invoke(amount);

            // Check for level-ups
            while (CurrentLevel < MAX_LEVEL && CurrentXP >= XPToNextLevel)
            {
                CurrentXP -= XPToNextLevel;
                CurrentLevel++;
                OnLevelUp?.Invoke(CurrentLevel);
                Debug.Log($"[Progression] Level up! Now level {CurrentLevel}");
            }
        }

        /// <summary>
        /// Records end-of-match statistics and awards XP.
        /// </summary>
        public void RecordMatchStats(MatchStats stats)
        {
            TotalMatchesPlayed++;
            TotalKills += stats.Kills;
            TotalPlayTime += stats.SurvivalTime;

            if (stats.Won)
                TotalWins++;
            else
                TotalDeaths++;

            // Calculate XP rewards
            int xpEarned = 0;
            xpEarned += stats.ObjectivesCompleted * XP_OBJECTIVE_COMPLETE;
            xpEarned += stats.Kills * XP_VILLAIN_ELIMINATED;
            xpEarned += stats.ItemsCollected * XP_LOOT_COLLECTED;

            if (stats.Extracted)
                xpEarned += XP_EXTRACTION_SUCCESS;

            int survivalBonuses = Mathf.FloorToInt(stats.SurvivalTime / 300f);
            xpEarned += survivalBonuses * XP_SURVIVE_5_MIN;

            stats.XPEarned = xpEarned;
            AddXP(xpEarned);

            OnMatchStatsRecorded?.Invoke(stats);
            Debug.Log($"[Progression] Match recorded: {xpEarned} XP earned");
        }

        /// <summary>
        /// Loads saved progression data.
        /// </summary>
        public void LoadProgression(int level, int xp, int totalXP, int matches, int wins, int kills, int deaths, float playTime)
        {
            CurrentLevel = Mathf.Clamp(level, 1, MAX_LEVEL);
            CurrentXP = Mathf.Max(0, xp);
            TotalXP = Mathf.Max(0, totalXP);
            TotalMatchesPlayed = matches;
            TotalWins = wins;
            TotalKills = kills;
            TotalDeaths = deaths;
            TotalPlayTime = playTime;
        }

        #endregion

        #region Helpers

        private int CalculateXPForLevel(int level)
        {
            return Mathf.RoundToInt(BASE_XP_PER_LEVEL * Mathf.Pow(XP_SCALING, level - 1));
        }

        #endregion
    }

    /// <summary>
    /// Match statistics data.
    /// </summary>
    [Serializable]
    public struct MatchStats
    {
        public bool Won;
        public bool Extracted;
        public int Kills;
        public int Deaths;
        public int ObjectivesCompleted;
        public int ItemsCollected;
        public float SurvivalTime;
        public int XPEarned;
        public string MapName;
        public string GameMode;
    }
}
