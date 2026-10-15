using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RageHunters.UI
{
    /// <summary>
    /// Comprehensive options menu featuring tabbed settings:
    /// Graphics (quality presets, shadows, target framerate),
    /// Audio (master, SFX, and music volume sliders),
    /// Controls (look sensitivity, invert Y toggle).
    /// All preferences persist across sessions using PlayerPrefs.
    /// </summary>
    public sealed class SettingsScreen : UIScreen
    {
        #region Constants & PlayerPrefs Keys

        private const string PrefKeyQuality = "RH_Settings_Quality";
        private const string PrefKeyShadows = "RH_Settings_Shadows";
        private const string PrefKeyTargetFps = "RH_Settings_TargetFPS";
        private const string PrefKeyMasterVol = "RH_Settings_MasterVol";
        private const string PrefKeySfxVol = "RH_Settings_SfxVol";
        private const string PrefKeyMusicVol = "RH_Settings_MusicVol";
        private const string PrefKeyLookSensitivity = "RH_Settings_LookSensitivity";
        private const string PrefKeyInvertY = "RH_Settings_InvertY";

        private const float DefaultMasterVol = 1.0f;
        private const float DefaultSfxVol = 1.0f;
        private const float DefaultMusicVol = 0.8f;
        private const float DefaultLookSensitivity = 2.0f;
        private const int DefaultTargetFps = 60;

        #endregion

        #region Serialized Fields

        [Header("Tab Navigation")]
        [SerializeField]
        [Tooltip("Button to select the Graphics configuration tab.")]
        private Button _graphicsTabButton;

        [SerializeField]
        [Tooltip("Button to select the Audio configuration tab.")]
        private Button _audioTabButton;

        [SerializeField]
        [Tooltip("Button to select the Controls configuration tab.")]
        private Button _controlsTabButton;

        [Header("Tab Panels")]
        [SerializeField]
        [Tooltip("Panel containing Graphics settings.")]
        private GameObject _graphicsPanel;

        [SerializeField]
        [Tooltip("Panel containing Audio settings.")]
        private GameObject _audioPanel;

        [SerializeField]
        [Tooltip("Panel containing Controls settings.")]
        private GameObject _controlsPanel;

        [Header("Graphics Tab Controls")]
        [SerializeField]
        [Tooltip("Dropdown for selecting quality preset (Low, Medium, High).")]
        private TMP_Dropdown _qualityDropdown;

        [SerializeField]
        [Tooltip("Toggle to enable or disable dynamic shadows.")]
        private Toggle _shadowsToggle;

        [SerializeField]
        [Tooltip("Button to set 30 FPS mode.")]
        private Button _fps30Button;

        [SerializeField]
        [Tooltip("Button to set 60 FPS mode.")]
        private Button _fps60Button;

        [SerializeField]
        [Tooltip("Text displaying current target framerate.")]
        private TMP_Text _fpsText;

        [Header("Audio Tab Controls")]
        [SerializeField]
        [Tooltip("Slider for master audio volume (0-1).")]
        private Slider _masterVolumeSlider;

        [SerializeField]
        [Tooltip("Text readout for master audio volume.")]
        private TMP_Text _masterVolumeText;

        [SerializeField]
        [Tooltip("Slider for SFX volume (0-1).")]
        private Slider _sfxVolumeSlider;

        [SerializeField]
        [Tooltip("Text readout for SFX volume.")]
        private TMP_Text _sfxVolumeText;

        [SerializeField]
        [Tooltip("Slider for music volume (0-1).")]
        private Slider _musicVolumeSlider;

        [SerializeField]
        [Tooltip("Text readout for music volume.")]
        private TMP_Text _musicVolumeText;

        [Header("Controls Tab Controls")]
        [SerializeField]
        [Tooltip("Slider for camera look sensitivity.")]
        private Slider _sensitivitySlider;

        [SerializeField]
        [Tooltip("Text readout for camera sensitivity.")]
        private TMP_Text _sensitivityText;

        [SerializeField]
        [Tooltip("Toggle to invert vertical camera look axis.")]
        private Toggle _invertYToggle;

        [Header("Footer Actions")]
        [SerializeField]
        [Tooltip("Button to close or return from settings.")]
        private Button _closeButton;

        [SerializeField]
        [Tooltip("Button to reset all settings to defaults.")]
        private Button _resetDefaultsButton;

        #endregion

        #region Private Fields

        private int _currentTargetFps = 60;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            BindTabs();
            BindControls();
            LoadSettings();
            SwitchTab(TabType.Graphics);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnbindTabs();
            UnbindControls();
        }

        #endregion

        #region Tab Management

        public enum TabType
        {
            Graphics,
            Audio,
            Controls
        }

        private void BindTabs()
        {
            if (_graphicsTabButton != null)
            {
                _graphicsTabButton.onClick.AddListener(() => SwitchTab(TabType.Graphics));
            }

            if (_audioTabButton != null)
            {
                _audioTabButton.onClick.AddListener(() => SwitchTab(TabType.Audio));
            }

            if (_controlsTabButton != null)
            {
                _controlsTabButton.onClick.AddListener(() => SwitchTab(TabType.Controls));
            }
        }

        private void UnbindTabs()
        {
            if (_graphicsTabButton != null)
            {
                _graphicsTabButton.onClick.RemoveAllListeners();
            }

            if (_audioTabButton != null)
            {
                _audioTabButton.onClick.RemoveAllListeners();
            }

            if (_controlsTabButton != null)
            {
                _controlsTabButton.onClick.RemoveAllListeners();
            }
        }

        /// <summary>
        /// Switches active visible settings panel tab.
        /// </summary>
        public void SwitchTab(TabType tab)
        {
            if (UIManager.HasInstance)
            {
                UIManager.Instance.PlayClickSound();
            }

            if (_graphicsPanel != null) _graphicsPanel.SetActive(tab == TabType.Graphics);
            if (_audioPanel != null) _audioPanel.SetActive(tab == TabType.Audio);
            if (_controlsPanel != null) _controlsPanel.SetActive(tab == TabType.Controls);
        }

        #endregion

        #region Control Bindings

        private void BindControls()
        {
            if (_qualityDropdown != null)
            {
                _qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }

            if (_shadowsToggle != null)
            {
                _shadowsToggle.onValueChanged.AddListener(OnShadowsToggled);
            }

            if (_fps30Button != null)
            {
                _fps30Button.onClick.AddListener(() => OnFpsChanged(30));
            }

            if (_fps60Button != null)
            {
                _fps60Button.onClick.AddListener(() => OnFpsChanged(60));
            }

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (_invertYToggle != null)
            {
                _invertYToggle.onValueChanged.AddListener(OnInvertYToggled);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (_resetDefaultsButton != null)
            {
                _resetDefaultsButton.onClick.AddListener(ResetToDefaults);
            }
        }

        private void UnbindControls()
        {
            if (_qualityDropdown != null)
            {
                _qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
            }

            if (_shadowsToggle != null)
            {
                _shadowsToggle.onValueChanged.RemoveListener(OnShadowsToggled);
            }

            if (_fps30Button != null)
            {
                _fps30Button.onClick.RemoveAllListeners();
            }

            if (_fps60Button != null)
            {
                _fps60Button.onClick.RemoveAllListeners();
            }

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            }

            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
            }

            if (_invertYToggle != null)
            {
                _invertYToggle.onValueChanged.RemoveListener(OnInvertYToggled);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }

            if (_resetDefaultsButton != null)
            {
                _resetDefaultsButton.onClick.RemoveListener(ResetToDefaults);
            }
        }

        #endregion

        #region Setting Handlers

        private void OnQualityChanged(int index)
        {
            QualitySettings.SetQualityLevel(index, applyExpensiveChanges: true);
            PlayerPrefs.SetInt(PrefKeyQuality, index);
            PlayerPrefs.Save();
        }

        private void OnShadowsToggled(bool enabled)
        {
            QualitySettings.shadows = enabled ? ShadowQuality.All : ShadowQuality.Disable;
            PlayerPrefs.SetInt(PrefKeyShadows, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnFpsChanged(int targetFps)
        {
            _currentTargetFps = targetFps;
            Application.targetFrameRate = _currentTargetFps;
            PlayerPrefs.SetInt(PrefKeyTargetFps, _currentTargetFps);
            PlayerPrefs.Save();

            if (_fpsText != null)
            {
                _fpsText.text = $"{_currentTargetFps} FPS";
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(PrefKeyMasterVol, value);
            PlayerPrefs.Save();

            if (_masterVolumeText != null)
            {
                _masterVolumeText.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        private void OnSfxVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat(PrefKeySfxVol, value);
            PlayerPrefs.Save();

            if (_sfxVolumeText != null)
            {
                _sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        private void OnMusicVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat(PrefKeyMusicVol, value);
            PlayerPrefs.Save();

            if (_musicVolumeText != null)
            {
                _musicVolumeText.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        private void OnSensitivityChanged(float value)
        {
            PlayerPrefs.SetFloat(PrefKeyLookSensitivity, value);
            PlayerPrefs.Save();

            if (_sensitivityText != null)
            {
                _sensitivityText.text = value.ToString("F1");
            }
        }

        private void OnInvertYToggled(bool inverted)
        {
            PlayerPrefs.SetInt(PrefKeyInvertY, inverted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnCloseClicked()
        {
            if (UIManager.HasInstance)
            {
                UIManager.Instance.PlayClickSound();
                UIManager.Instance.NavigateBack();
            }
            else
            {
                Hide(immediate: false);
            }
        }

        #endregion

        #region Save / Load / Reset

        /// <summary>
        /// Loads all saved settings from PlayerPrefs and applies them.
        /// </summary>
        public void LoadSettings()
        {
            // Quality
            int defaultQuality = QualitySettings.GetQualityLevel();
            int quality = PlayerPrefs.GetInt(PrefKeyQuality, defaultQuality);
            if (_qualityDropdown != null)
            {
                _qualityDropdown.SetValueWithoutNotify(quality);
            }
            QualitySettings.SetQualityLevel(quality, applyExpensiveChanges: false);

            // Shadows
            bool shadows = PlayerPrefs.GetInt(PrefKeyShadows, 1) == 1;
            if (_shadowsToggle != null)
            {
                _shadowsToggle.SetIsOnWithoutNotify(shadows);
            }
            QualitySettings.shadows = shadows ? ShadowQuality.All : ShadowQuality.Disable;

            // Target FPS
            _currentTargetFps = PlayerPrefs.GetInt(PrefKeyTargetFps, DefaultTargetFps);
            Application.targetFrameRate = _currentTargetFps;
            if (_fpsText != null)
            {
                _fpsText.text = $"{_currentTargetFps} FPS";
            }

            // Audio
            float masterVol = PlayerPrefs.GetFloat(PrefKeyMasterVol, DefaultMasterVol);
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.SetValueWithoutNotify(masterVol);
            }
            AudioListener.volume = masterVol;
            if (_masterVolumeText != null)
            {
                _masterVolumeText.text = $"{Mathf.RoundToInt(masterVol * 100f)}%";
            }

            float sfxVol = PlayerPrefs.GetFloat(PrefKeySfxVol, DefaultSfxVol);
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.SetValueWithoutNotify(sfxVol);
            }
            if (_sfxVolumeText != null)
            {
                _sfxVolumeText.text = $"{Mathf.RoundToInt(sfxVol * 100f)}%";
            }

            float musicVol = PlayerPrefs.GetFloat(PrefKeyMusicVol, DefaultMusicVol);
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.SetValueWithoutNotify(musicVol);
            }
            if (_musicVolumeText != null)
            {
                _musicVolumeText.text = $"{Mathf.RoundToInt(musicVol * 100f)}%";
            }

            // Controls
            float sensitivity = PlayerPrefs.GetFloat(PrefKeyLookSensitivity, DefaultLookSensitivity);
            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.SetValueWithoutNotify(sensitivity);
            }
            if (_sensitivityText != null)
            {
                _sensitivityText.text = sensitivity.ToString("F1");
            }

            bool invertY = PlayerPrefs.GetInt(PrefKeyInvertY, 0) == 1;
            if (_invertYToggle != null)
            {
                _invertYToggle.SetIsOnWithoutNotify(invertY);
            }
        }

        /// <summary>
        /// Resets all settings to their factory default values.
        /// </summary>
        public void ResetToDefaults()
        {
            if (UIManager.HasInstance)
            {
                UIManager.Instance.PlayClickSound();
            }

            PlayerPrefs.DeleteKey(PrefKeyQuality);
            PlayerPrefs.DeleteKey(PrefKeyShadows);
            PlayerPrefs.DeleteKey(PrefKeyTargetFps);
            PlayerPrefs.DeleteKey(PrefKeyMasterVol);
            PlayerPrefs.DeleteKey(PrefKeySfxVol);
            PlayerPrefs.DeleteKey(PrefKeyMusicVol);
            PlayerPrefs.DeleteKey(PrefKeyLookSensitivity);
            PlayerPrefs.DeleteKey(PrefKeyInvertY);
            PlayerPrefs.Save();

            LoadSettings();
        }

        #endregion

        #region Back Navigation

        public override bool OnBackButtonPressed()
        {
            OnCloseClicked();
            return true;
        }

        #endregion
    }
}
