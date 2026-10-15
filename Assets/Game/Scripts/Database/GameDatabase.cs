using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RageHunters.Database
{
    /// <summary>
    /// Robust structured local database for RAGE HUNTERS.
    /// Persists player profiles, custom created villains, match histories,
    /// inventory records, and game settings to durable local storage on Android/iOS/PC.
    /// </summary>
    public class GameDatabase : Core.Singleton<GameDatabase>
    {
        private const string DB_FILENAME = "rage_hunters_db.json";
        private const int DB_VERSION = 1;

        private string _dbPath;
        private DatabasePayload _data;
        private bool _isLoaded;

        public event Action OnDatabaseLoaded;
        public event Action OnDatabaseSaved;

        #region Public Properties

        public PlayerProfileRecord Profile => _data?.Profile;
        public IReadOnlyList<VillainRecord> Villains => _data?.Villains ?? (IReadOnlyList<VillainRecord>)Array.Empty<VillainRecord>();
        public IReadOnlyList<MatchRecord> MatchHistory => _data?.MatchHistory ?? (IReadOnlyList<MatchRecord>)Array.Empty<MatchRecord>();
        public IReadOnlyList<InventoryItemRecord> Inventory => _data?.Inventory ?? (IReadOnlyList<InventoryItemRecord>)Array.Empty<InventoryItemRecord>();

        #endregion

        #region Initialization

        protected override void OnInitialized()
        {
            _dbPath = Path.Combine(Application.persistentDataPath, DB_FILENAME);
            LoadDatabase();
        }

        public void LoadDatabase()
        {
            try
            {
                if (File.Exists(_dbPath))
                {
                    string json = File.ReadAllText(_dbPath);
                    _data = JsonUtility.FromJson<DatabasePayload>(json);

                    if (_data == null || _data.Version != DB_VERSION)
                    {
                        Debug.LogWarning("[GameDatabase] Database missing or outdated version. Initializing defaults.");
                        InitializeDefaultDatabase();
                    }
                }
                else
                {
                    Debug.Log("[GameDatabase] No existing database found. Creating fresh database.");
                    InitializeDefaultDatabase();
                }

                _isLoaded = true;
                OnDatabaseLoaded?.Invoke();
                Debug.Log($"[GameDatabase] Loaded successfully from: {_dbPath} (Villains: {_data.Villains.Count}, Matches: {_data.MatchHistory.Count})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameDatabase] Error loading database: {ex.Message}. Falling back to defaults.");
                InitializeDefaultDatabase();
                _isLoaded = true;
            }
        }

        public void SaveDatabase()
        {
            if (_data == null) return;

            try
            {
                _data.LastSavedAt = DateTime.UtcNow.ToString("o");
                string json = JsonUtility.ToJson(_data, true);
                File.WriteAllText(_dbPath, json);
                OnDatabaseSaved?.Invoke();
                Debug.Log("[GameDatabase] Database saved to disk successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameDatabase] Failed to save database to {_dbPath}: {ex.Message}");
            }
        }

        private void InitializeDefaultDatabase()
        {
            _data = new DatabasePayload
            {
                Version = DB_VERSION,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                LastSavedAt = DateTime.UtcNow.ToString("o"),
                Profile = new PlayerProfileRecord
                {
                    PlayerId = "survivor_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    PlayerName = "Hunter One",
                    Level = 1,
                    CurrentXP = 0,
                    TotalXP = 0,
                    MatchesPlayed = 0,
                    MatchesWon = 0,
                    Kills = 0,
                    Deaths = 0,
                    SurvivalTimeSeconds = 0f,
                    Credits = 500
                },
                Villains = new List<VillainRecord>
                {
                    new VillainRecord
                    {
                        Id = "vil_tyrant",
                        Name = "The Corporate Tyrant",
                        Archetype = "OfficeTyrant",
                        Difficulty = "Normal",
                        Aggressiveness = 0.85f,
                        Stealthiness = 0.2f,
                        Intelligence = 0.6f,
                        BaseHealth = 120f,
                        MoveSpeed = 4.0f,
                        Backstory = "Fictional micromanager who rules the floor with an iron fist.",
                        ModelColorHex = "#DD2222",
                        CreatedAt = DateTime.UtcNow.ToString("o")
                    },
                    new VillainRecord
                    {
                        Id = "vil_backstabber",
                        Name = "The Smiling Betrayer",
                        Archetype = "Backstabber",
                        Difficulty = "Hard",
                        Aggressiveness = 0.5f,
                        Stealthiness = 0.9f,
                        Intelligence = 0.8f,
                        BaseHealth = 90f,
                        MoveSpeed = 4.8f,
                        Backstory = "Fictional rival who strikes silently from behind when you least expect it.",
                        ModelColorHex = "#9922DD",
                        CreatedAt = DateTime.UtcNow.ToString("o")
                    },
                    new VillainRecord
                    {
                        Id = "vil_manipulator",
                        Name = "The Gaslighter",
                        Archetype = "Manipulator",
                        Difficulty = "Nightmare",
                        Aggressiveness = 0.4f,
                        Stealthiness = 0.75f,
                        Intelligence = 0.95f,
                        BaseHealth = 100f,
                        MoveSpeed = 4.2f,
                        Backstory = "Fictional psychological schemer who sets deadly traps and ambushes.",
                        ModelColorHex = "#2299DD",
                        CreatedAt = DateTime.UtcNow.ToString("o")
                    }
                },
                MatchHistory = new List<MatchRecord>(),
                Inventory = new List<InventoryItemRecord>
                {
                    new InventoryItemRecord { ItemId = "wpn_baton", ItemName = "Tactical Baton", ItemType = "Weapon", Quantity = 1, IsEquipped = true, SlotIndex = 0 },
                    new InventoryItemRecord { ItemId = "med_kit", ItemName = "Emergency Medkit", ItemType = "Consumable", Quantity = 2, IsEquipped = false, SlotIndex = 1 },
                    new InventoryItemRecord { ItemId = "key_card", ItemName = "Executive Keycard", ItemType = "KeyItem", Quantity = 1, IsEquipped = false, SlotIndex = 2 }
                }
            };

            SaveDatabase();
        }

        #endregion

        #region Profile Operations

        public void UpdatePlayerProfile(string playerName = null, int? addXP = null, bool? matchWon = null, int? kills = null, float? survivalTime = null)
        {
            if (_data?.Profile == null) return;

            var p = _data.Profile;
            if (!string.IsNullOrEmpty(playerName)) p.PlayerName = playerName;

            if (addXP.HasValue && addXP.Value > 0)
            {
                p.CurrentXP += addXP.Value;
                p.TotalXP += addXP.Value;

                // Level up check
                int xpNeeded = p.Level * 200;
                while (p.CurrentXP >= xpNeeded && p.Level < 50)
                {
                    p.CurrentXP -= xpNeeded;
                    p.Level++;
                    p.Credits += p.Level * 100;
                    Debug.Log($"[GameDatabase] Level up! Player reached Level {p.Level}");
                }
            }

            if (matchWon.HasValue)
            {
                p.MatchesPlayed++;
                if (matchWon.Value) p.MatchesWon++;
                else p.Deaths++;
            }

            if (kills.HasValue) p.Kills += kills.Value;
            if (survivalTime.HasValue) p.SurvivalTimeSeconds += survivalTime.Value;

            SaveDatabase();
        }

        #endregion

        #region Villain Operations

        public bool InsertVillain(VillainRecord villain)
        {
            if (_data == null || villain == null) return false;

            if (string.IsNullOrEmpty(villain.Id))
            {
                villain.Id = "vil_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            }

            villain.CreatedAt = DateTime.UtcNow.ToString("o");
            _data.Villains.Add(villain);
            SaveDatabase();
            return true;
        }

        public bool UpdateVillain(VillainRecord villain)
        {
            if (_data == null || villain == null) return false;

            int index = _data.Villains.FindIndex(v => v.Id == villain.Id);
            if (index < 0) return false;

            _data.Villains[index] = villain;
            SaveDatabase();
            return true;
        }

        public bool DeleteVillain(string villainId)
        {
            if (_data == null) return false;

            int removed = _data.Villains.RemoveAll(v => v.Id == villainId);
            if (removed > 0)
            {
                SaveDatabase();
                return true;
            }
            return false;
        }

        public VillainRecord GetVillain(string villainId)
        {
            return _data?.Villains.Find(v => v.Id == villainId);
        }

        #endregion

        #region Match History Operations

        public void AddMatchRecord(MatchRecord match)
        {
            if (_data == null || match == null) return;

            if (string.IsNullOrEmpty(match.MatchId))
            {
                match.MatchId = "match_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            }

            match.Timestamp = DateTime.UtcNow.ToString("o");
            _data.MatchHistory.Insert(0, match); // Latest first

            // Keep maximum 50 match logs locally
            if (_data.MatchHistory.Count > 50)
            {
                _data.MatchHistory.RemoveRange(50, _data.MatchHistory.Count - 50);
            }

            SaveDatabase();
        }

        #endregion

        #region Reset

        public void ResetDatabase()
        {
            InitializeDefaultDatabase();
            Debug.Log("[GameDatabase] Database has been completely reset to defaults.");
        }

        #endregion
    }

    #region Record Data Classes

    [Serializable]
    public class DatabasePayload
    {
        public int Version;
        public string CreatedAt;
        public string LastSavedAt;
        public PlayerProfileRecord Profile;
        public List<VillainRecord> Villains = new();
        public List<MatchRecord> MatchHistory = new();
        public List<InventoryItemRecord> Inventory = new();
    }

    [Serializable]
    public class PlayerProfileRecord
    {
        public string PlayerId;
        public string PlayerName;
        public int Level;
        public int CurrentXP;
        public int TotalXP;
        public int MatchesPlayed;
        public int MatchesWon;
        public int Kills;
        public int Deaths;
        public float SurvivalTimeSeconds;
        public int Credits;
    }

    [Serializable]
    public class VillainRecord
    {
        public string Id;
        public string Name;
        public string Archetype;
        public string Difficulty;
        public float Aggressiveness;
        public float Stealthiness;
        public float Intelligence;
        public float BaseHealth;
        public float MoveSpeed;
        public string Backstory;
        public string ModelColorHex;
        public string CreatedAt;
    }

    [Serializable]
    public class MatchRecord
    {
        public string MatchId;
        public string Timestamp;
        public string GameMode;
        public string MapName;
        public string Outcome; // "Extracted", "Eliminated", "Timeout"
        public float DurationSeconds;
        public int Kills;
        public int XPEarned;
        public bool Extracted;
    }

    [Serializable]
    public class InventoryItemRecord
    {
        public string ItemId;
        public string ItemName;
        public string ItemType;
        public int Quantity;
        public bool IsEquipped;
        public int SlotIndex;
    }

    #endregion
}
