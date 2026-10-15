using System;
using UnityEngine;

namespace RageHunters.SaveSystem
{
    /// <summary>
    /// Manages local save/load operations using PlayerPrefs (initial implementation).
    /// Designed for future migration to cloud save (PlayFab).
    /// </summary>
    public class SaveManager : Core.Singleton<SaveManager>
    {
        #region Constants

        private const string KEY_PLAYER_LEVEL = "Save_PlayerLevel";
        private const string KEY_PLAYER_XP = "Save_PlayerXP";
        private const string KEY_TOTAL_XP = "Save_TotalXP";
        private const string KEY_MATCHES = "Save_Matches";
        private const string KEY_WINS = "Save_Wins";
        private const string KEY_KILLS = "Save_Kills";
        private const string KEY_DEATHS = "Save_Deaths";
        private const string KEY_PLAYTIME = "Save_PlayTime";
        private const string KEY_VILLAINS = "Save_Villains";
        private const string KEY_GUEST_ID = "Save_GuestId";

        #endregion

        #region Events

        public event Action OnSaveCompleted;
        public event Action OnLoadCompleted;

        #endregion

        #region Properties

        /// <summary>
        /// Returns a persistent guest ID for unauthenticated sessions.
        /// Generated once and stored locally.
        /// </summary>
        public string GuestId
        {
            get
            {
                string id = PlayerPrefs.GetString(KEY_GUEST_ID, "");
                if (string.IsNullOrEmpty(id))
                {
                    id = "guest_" + Guid.NewGuid().ToString("N")[..12];
                    PlayerPrefs.SetString(KEY_GUEST_ID, id);
                    PlayerPrefs.Save();
                }
                return id;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Saves current progression data to local storage.
        /// </summary>
        public void SaveProgression()
        {
            var pm = Progression.ProgressionManager.Instance;
            if (pm == null)
            {
                Debug.LogWarning("[SaveManager] ProgressionManager not available.");
                return;
            }

            PlayerPrefs.SetInt(KEY_PLAYER_LEVEL, pm.CurrentLevel);
            PlayerPrefs.SetInt(KEY_PLAYER_XP, pm.CurrentXP);
            PlayerPrefs.SetInt(KEY_TOTAL_XP, pm.TotalXP);
            PlayerPrefs.SetInt(KEY_MATCHES, pm.TotalMatchesPlayed);
            PlayerPrefs.SetInt(KEY_WINS, pm.TotalWins);
            PlayerPrefs.SetInt(KEY_KILLS, pm.TotalKills);
            PlayerPrefs.SetInt(KEY_DEATHS, pm.TotalDeaths);
            PlayerPrefs.SetFloat(KEY_PLAYTIME, pm.TotalPlayTime);
            PlayerPrefs.Save();

            OnSaveCompleted?.Invoke();
            Debug.Log("[SaveManager] Progression saved locally.");
        }

        /// <summary>
        /// Loads progression data from local storage into ProgressionManager.
        /// </summary>
        public void LoadProgression()
        {
            var pm = Progression.ProgressionManager.Instance;
            if (pm == null)
            {
                Debug.LogWarning("[SaveManager] ProgressionManager not available.");
                return;
            }

            pm.LoadProgression(
                level: PlayerPrefs.GetInt(KEY_PLAYER_LEVEL, 1),
                xp: PlayerPrefs.GetInt(KEY_PLAYER_XP, 0),
                totalXP: PlayerPrefs.GetInt(KEY_TOTAL_XP, 0),
                matches: PlayerPrefs.GetInt(KEY_MATCHES, 0),
                wins: PlayerPrefs.GetInt(KEY_WINS, 0),
                kills: PlayerPrefs.GetInt(KEY_KILLS, 0),
                deaths: PlayerPrefs.GetInt(KEY_DEATHS, 0),
                playTime: PlayerPrefs.GetFloat(KEY_PLAYTIME, 0f)
            );

            OnLoadCompleted?.Invoke();
            Debug.Log("[SaveManager] Progression loaded from local storage.");
        }

        /// <summary>
        /// Saves a villain configuration as JSON to local storage.
        /// </summary>
        public void SaveVillainData(string villainJson)
        {
            PlayerPrefs.SetString(KEY_VILLAINS, villainJson);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads villain data JSON from local storage.
        /// </summary>
        public string LoadVillainData()
        {
            return PlayerPrefs.GetString(KEY_VILLAINS, "[]");
        }

        /// <summary>
        /// Clears all saved data (for account deletion / reset).
        /// </summary>
        public void ClearAllData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] All local data cleared.");
        }

        /// <summary>
        /// Checks whether any local save data exists.
        /// </summary>
        public bool HasSaveData()
        {
            return PlayerPrefs.HasKey(KEY_PLAYER_LEVEL);
        }

        #endregion
    }
}
