using UnityEngine;

namespace RageHunters.Core
{
    /// <summary>
    /// Centralized repository of static constants, tags, layers, scene names,
    /// animation parameter hashes, and default gameplay configuration values.
    /// </summary>
    public static class GameConstants
    {
        #region Scenes

        public static class Scenes
        {
            public const string Bootstrapper = "Bootstrapper";
            public const string MainMenu = "MainMenu";
            public const string LoadingScreen = "LoadingScreen";
            public const string Lobby = "Lobby";
            public const string GameMap = "GameMap";
            public const string ExtractionZone = "ExtractionZone";
        }

        #endregion

        #region Tags

        public static class Tags
        {
            public const string Player = "Player";
            public const string Enemy = "Enemy";
            public const string Weapon = "Weapon";
            public const string Loot = "Loot";
            public const string ExtractionPoint = "ExtractionPoint";
            public const string MainCamera = "MainCamera";
            public const string SpawnPoint = "SpawnPoint";
        }

        #endregion

        #region Layers

        public static class Layers
        {
            public const string Default = "Default";
            public const string TransparentFX = "TransparentFX";
            public const string IgnoreRaycast = "Ignore Raycast";
            public const string Water = "Water";
            public const string UI = "UI";
            public const string Player = "Player";
            public const string Enemy = "Enemy";
            public const string Environment = "Environment";
            public const string Interactable = "Interactable";
            public const string Hitbox = "Hitbox";

            // Layer Masks
            public static readonly int PlayerMask = LayerMask.GetMask(Player);
            public static readonly int EnemyMask = LayerMask.GetMask(Enemy);
            public static readonly int EnvironmentMask = LayerMask.GetMask(Environment);
            public static readonly int InteractableMask = LayerMask.GetMask(Interactable);
        }

        #endregion

        #region Animation Hashes

        public static class AnimParams
        {
            public static readonly int Speed = Animator.StringToHash("Speed");
            public static readonly int IsMoving = Animator.StringToHash("IsMoving");
            public static readonly int IsSprinting = Animator.StringToHash("IsSprinting");
            public static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
            public static readonly int Jump = Animator.StringToHash("Jump");
            public static readonly int Attack = Animator.StringToHash("Attack");
            public static readonly int Reload = Animator.StringToHash("Reload");
            public static readonly int Hit = Animator.StringToHash("Hit");
            public static readonly int Die = Animator.StringToHash("Die");
        }

        #endregion

        #region Gameplay Defaults

        public static class PlayerDefaults
        {
            public const float DefaultMaxHealth = 100f;
            public const float DefaultMaxArmor = 100f;
            public const float DefaultWalkSpeed = 4.0f;
            public const float DefaultRunSpeed = 7.0f;
            public const float DefaultSprintSpeed = 10.0f;
            public const float DefaultCrouchSpeed = 2.5f;
            public const float DefaultJumpForce = 5.0f;
            public const float DefaultGravity = -9.81f;
            public const float DefaultLookSensitivity = 2.0f;
        }

        public static class MatchDefaults
        {
            public const float DefaultMatchDurationSeconds = 600f; // 10 minutes
            public const float DefaultExtractionCountdownSeconds = 30f;
            public const int MaxPlayersPerMatch = 16;
            public const int MinPlayersToStart = 2;
        }

        public static class PoolingDefaults
        {
            public const int DefaultInitialPoolSize = 10;
            public const int DefaultMaxPoolSize = 50;
        }

        #endregion
    }
}
