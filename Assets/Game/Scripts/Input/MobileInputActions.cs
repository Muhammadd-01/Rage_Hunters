using System;
using System.Collections.Generic;
using UnityEngine;

namespace RageHunters.Input
{
    /// <summary>
    /// Preset layouts for mobile touch controls.
    /// </summary>
    public enum TouchLayoutPreset
    {
        Default = 0,
        Compact,
        LeftHanded,
        Custom
    }

    /// <summary>
    /// Serializable representation of a single touch button's layout position and size.
    /// </summary>
    [Serializable]
    public struct TouchButtonLayoutData
    {
        public TouchButtonType ButtonType;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
        public float Scale;

        public TouchButtonLayoutData(TouchButtonType type, Vector2 anchoredPos, Vector2 size, float scale = 1f)
        {
            ButtonType = type;
            AnchoredPosition = anchoredPos;
            SizeDelta = size;
            Scale = scale;
        }
    }

    /// <summary>
    /// Static helper class containing mobile input configuration keys, default values,
    /// layout presets, and utility functions for touch input management.
    /// </summary>
    public static class MobileInputActions
    {
        #region PlayerPrefs Keys

        public const string PrefKeySensitivityX = "RH_Touch_SensitivityX";
        public const string PrefKeySensitivityY = "RH_Touch_SensitivityY";
        public const string PrefKeyInvertY = "RH_Touch_InvertY";
        public const string PrefKeyInvertX = "RH_Touch_InvertX";
        public const string PrefKeyDeadZone = "RH_Touch_JoystickDeadZone";
        public const string PrefKeyJoystickType = "RH_Touch_JoystickType";
        public const string PrefKeyTouchOpacity = "RH_Touch_HUDOpacity";
        public const string PrefKeyLayoutPreset = "RH_Touch_LayoutPreset";
        public const string PrefKeyCustomLayoutPrefix = "RH_Touch_ButtonPos_";

        #endregion

        #region Default Values

        public const float DefaultSensitivityX = 0.12f;
        public const float DefaultSensitivityY = 0.12f;
        public const bool DefaultInvertY = false;
        public const bool DefaultInvertX = false;
        public const float DefaultJoystickDeadZone = 0.1f;
        public const float DefaultTouchOpacity = 0.85f;
        public const JoystickType DefaultJoystickType = JoystickType.Fixed;
        public const TouchLayoutPreset DefaultPreset = TouchLayoutPreset.Default;

        #endregion

        #region Platform Utilities

        /// <summary>
        /// Indicates whether the game is running on a touch-first mobile platform or editor mobile emulation.
        /// </summary>
        public static bool IsMobilePlatform
        {
            get
            {
#if UNITY_ANDROID || UNITY_IOS
                return true;
#else
                return UnityEngine.InputSystem.Touchscreen.current != null;
#endif
            }
        }

        /// <summary>
        /// Indicates if touch controls should be actively displayed.
        /// Can be forced for testing or debugging on standalone builds.
        /// </summary>
        public static bool ForceMobileTouchControls { get; set; } = false;

        /// <summary>
        /// Checks whether mobile touch controls should be enabled and visible.
        /// </summary>
        public static bool ShouldEnableTouchControls()
        {
            return ForceMobileTouchControls || IsMobilePlatform;
        }

        #endregion

        #region Serialization Helpers

        /// <summary>
        /// Saves a button's anchored position to PlayerPrefs.
        /// </summary>
        public static void SaveButtonPosition(TouchButtonType buttonType, Vector2 anchoredPos)
        {
            string key = $"{PrefKeyCustomLayoutPrefix}{buttonType}";
            PlayerPrefs.SetFloat($"{key}_X", anchoredPos.x);
            PlayerPrefs.SetFloat($"{key}_Y", anchoredPos.y);
        }

        /// <summary>
        /// Attempts to load a saved button position from PlayerPrefs.
        /// </summary>
        public static bool TryLoadButtonPosition(TouchButtonType buttonType, out Vector2 anchoredPos)
        {
            string key = $"{PrefKeyCustomLayoutPrefix}{buttonType}";
            if (PlayerPrefs.HasKey($"{key}_X") && PlayerPrefs.HasKey($"{key}_Y"))
            {
                anchoredPos = new Vector2(
                    PlayerPrefs.GetFloat($"{key}_X"),
                    PlayerPrefs.GetFloat($"{key}_Y")
                );
                return true;
            }

            anchoredPos = Vector2.zero;
            return false;
        }

        /// <summary>
        /// Clears all custom layout positions saved in PlayerPrefs.
        /// </summary>
        public static void ClearCustomLayout()
        {
            foreach (TouchButtonType type in Enum.GetValues(typeof(TouchButtonType)))
            {
                string key = $"{PrefKeyCustomLayoutPrefix}{type}";
                PlayerPrefs.DeleteKey($"{key}_X");
                PlayerPrefs.DeleteKey($"{key}_Y");
            }
        }

        #endregion
    }
}
