using System;
using System.Collections.Generic;
using UnityEngine;

namespace RageHunters.Villains
{
    /// <summary>
    /// Manages creation, saving, loading, editing, and deletion of custom villain configurations.
    /// All villain data is fictional — no real-person targeting is permitted.
    /// </summary>
    public class VillainCreator : Core.Singleton<VillainCreator>
    {
        #region Constants

        public const int MAX_VILLAINS = 20;
        public const int MAX_NICKNAME_LENGTH = 20;
        public const int MAX_BACKSTORY_LENGTH = 200;

        #endregion

        #region Events

        public event Action<VillainConfig> OnVillainCreated;
        public event Action<VillainConfig> OnVillainUpdated;
        public event Action<string> OnVillainDeleted;
        public event Action OnVillainsLoaded;

        #endregion

        #region Properties

        public IReadOnlyList<VillainConfig> SavedVillains => _savedVillains;
        public int VillainCount => _savedVillains.Count;

        #endregion

        #region Private Fields

        private List<VillainConfig> _savedVillains = new();

        private static readonly string[] _blockedWords = new string[]
        {
            // Basic profanity filter — extend as needed
            "kill", "murder", "die", "hate", "racist", "sexist"
        };

        #endregion

        #region Public API

        /// <summary>
        /// Creates and saves a new villain configuration.
        /// Returns null if validation fails.
        /// </summary>
        public VillainConfig CreateVillain(
            string nickname,
            VillainArchetype archetype,
            DifficultyLevel difficulty,
            string backstory = "",
            float aggressiveness = 0.5f,
            float stealthiness = 0.5f,
            float intelligence = 0.5f)
        {
            // Validation
            if (_savedVillains.Count >= MAX_VILLAINS)
            {
                Debug.LogWarning("[VillainCreator] Maximum villain limit reached.");
                return null;
            }

            string sanitized = SanitizeNickname(nickname);
            if (string.IsNullOrEmpty(sanitized))
            {
                Debug.LogWarning("[VillainCreator] Invalid nickname.");
                return null;
            }

            var config = new VillainConfig
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                Nickname = sanitized,
                Archetype = archetype,
                Difficulty = difficulty,
                Backstory = SanitizeText(backstory, MAX_BACKSTORY_LENGTH),
                Aggressiveness = Mathf.Clamp01(aggressiveness),
                Stealthiness = Mathf.Clamp01(stealthiness),
                Intelligence = Mathf.Clamp01(intelligence),
                CreatedAt = DateTime.UtcNow.ToString("o")
            };

            _savedVillains.Add(config);
            Save();
            OnVillainCreated?.Invoke(config);
            Debug.Log($"[VillainCreator] Created villain: {config.Nickname} ({config.Archetype})");
            return config;
        }

        /// <summary>
        /// Updates an existing villain configuration.
        /// </summary>
        public bool UpdateVillain(VillainConfig updated)
        {
            int index = _savedVillains.FindIndex(v => v.Id == updated.Id);
            if (index < 0) return false;

            updated.Nickname = SanitizeNickname(updated.Nickname);
            updated.Backstory = SanitizeText(updated.Backstory, MAX_BACKSTORY_LENGTH);
            _savedVillains[index] = updated;
            Save();
            OnVillainUpdated?.Invoke(updated);
            return true;
        }

        /// <summary>
        /// Deletes a villain by ID.
        /// </summary>
        public bool DeleteVillain(string villainId)
        {
            int index = _savedVillains.FindIndex(v => v.Id == villainId);
            if (index < 0) return false;

            _savedVillains.RemoveAt(index);
            Save();
            OnVillainDeleted?.Invoke(villainId);
            return true;
        }

        /// <summary>
        /// Duplicates an existing villain with a new ID.
        /// </summary>
        public VillainConfig DuplicateVillain(string villainId)
        {
            var original = _savedVillains.Find(v => v.Id == villainId);
            if (original == null || _savedVillains.Count >= MAX_VILLAINS) return null;

            var copy = original.Clone();
            copy.Id = Guid.NewGuid().ToString("N")[..8];
            copy.Nickname = copy.Nickname + " Copy";
            if (copy.Nickname.Length > MAX_NICKNAME_LENGTH)
                copy.Nickname = copy.Nickname[..MAX_NICKNAME_LENGTH];

            _savedVillains.Add(copy);
            Save();
            OnVillainCreated?.Invoke(copy);
            return copy;
        }

        /// <summary>
        /// Gets a villain by ID.
        /// </summary>
        public VillainConfig GetVillain(string villainId)
        {
            return _savedVillains.Find(v => v.Id == villainId);
        }

        /// <summary>
        /// Loads villains from local storage.
        /// </summary>
        public void LoadVillains()
        {
            if (SaveSystem.SaveManager.Instance == null) return;

            string json = SaveSystem.SaveManager.Instance.LoadVillainData();
            if (!string.IsNullOrEmpty(json) && json != "[]")
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<VillainListWrapper>(json);
                    if (wrapper?.Villains != null)
                        _savedVillains = wrapper.Villains;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[VillainCreator] Failed to load villains: {e.Message}");
                    _savedVillains = new List<VillainConfig>();
                }
            }

            OnVillainsLoaded?.Invoke();
        }

        #endregion

        #region Private Helpers

        private void Save()
        {
            if (SaveSystem.SaveManager.Instance == null) return;

            var wrapper = new VillainListWrapper { Villains = _savedVillains };
            string json = JsonUtility.ToJson(wrapper, false);
            SaveSystem.SaveManager.Instance.SaveVillainData(json);
        }

        private string SanitizeNickname(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            string trimmed = input.Trim();
            if (trimmed.Length > MAX_NICKNAME_LENGTH)
                trimmed = trimmed[..MAX_NICKNAME_LENGTH];

            // Basic profanity check
            string lower = trimmed.ToLowerInvariant();
            foreach (string blocked in _blockedWords)
            {
                if (lower.Contains(blocked))
                {
                    Debug.LogWarning($"[VillainCreator] Nickname rejected: contains blocked word.");
                    return null;
                }
            }

            return trimmed;
        }

        private string SanitizeText(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return "";
            string trimmed = input.Trim();
            return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Complete villain configuration data.
    /// </summary>
    [Serializable]
    public class VillainConfig
    {
        public string Id;
        public string Nickname;
        public VillainArchetype Archetype;
        public DifficultyLevel Difficulty;
        public string Backstory;

        [Range(0f, 1f)] public float Aggressiveness;
        [Range(0f, 1f)] public float Stealthiness;
        [Range(0f, 1f)] public float Intelligence;

        // Appearance (indices into model/color/accessory arrays)
        public int ModelIndex;
        public int ColorSchemeIndex;
        public int AccessoryIndex;

        public string CreatedAt;

        public VillainConfig Clone()
        {
            return new VillainConfig
            {
                Id = Id,
                Nickname = Nickname,
                Archetype = Archetype,
                Difficulty = Difficulty,
                Backstory = Backstory,
                Aggressiveness = Aggressiveness,
                Stealthiness = Stealthiness,
                Intelligence = Intelligence,
                ModelIndex = ModelIndex,
                ColorSchemeIndex = ColorSchemeIndex,
                AccessoryIndex = AccessoryIndex,
                CreatedAt = CreatedAt
            };
        }
    }

    public enum VillainArchetype
    {
        OfficeTyrant,
        Backstabber,
        Manipulator,
        FakeFriend,
        RuthlessRival,
        Mastermind
    }

    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard,
        Nightmare
    }

    [Serializable]
    internal class VillainListWrapper
    {
        public List<VillainConfig> Villains = new();
    }

    #endregion
}
