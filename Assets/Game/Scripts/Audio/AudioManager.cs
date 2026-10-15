using System.Collections.Generic;
using UnityEngine;

namespace RageHunters.Audio
{
    /// <summary>
    /// Central audio manager handling music, SFX, and ambient audio with volume controls.
    /// </summary>
    public class AudioManager : Core.Singleton<AudioManager>
    {
        #region Serialized Fields

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _ambienceSource;
        [SerializeField] private int _sfxPoolSize = 8;

        [Header("Default Volumes")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.8f;

        #endregion

        #region Private Fields

        private List<AudioSource> _sfxPool = new();
        private int _nextSfxIndex;

        private const string PREF_MASTER = "Audio_Master";
        private const string PREF_MUSIC = "Audio_Music";
        private const string PREF_SFX = "Audio_SFX";

        #endregion

        #region Properties

        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;

        #endregion

        #region Unity Lifecycle

        protected override void OnInitialized()
        {
            LoadVolumeSettings();
            InitializeSFXPool();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Plays a one-shot sound effect at the listener position.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetNextSFXSource();
            source.clip = clip;
            source.volume = _sfxVolume * _masterVolume * volumeScale;
            source.spatialBlend = 0f;
            source.Play();
        }

        /// <summary>
        /// Plays a 3D positional sound effect.
        /// </summary>
        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetNextSFXSource();
            source.transform.position = position;
            source.clip = clip;
            source.volume = _sfxVolume * _masterVolume * volumeScale;
            source.spatialBlend = 1f;
            source.Play();
        }

        /// <summary>
        /// Plays background music with optional crossfade.
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (_musicSource == null || clip == null) return;

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = _musicVolume * _masterVolume;
            _musicSource.Play();
        }

        /// <summary>
        /// Stops the currently playing music.
        /// </summary>
        public void StopMusic()
        {
            if (_musicSource != null)
                _musicSource.Stop();
        }

        /// <summary>
        /// Plays ambient/environmental audio.
        /// </summary>
        public void PlayAmbience(AudioClip clip, bool loop = true)
        {
            if (_ambienceSource == null || clip == null) return;

            _ambienceSource.clip = clip;
            _ambienceSource.loop = loop;
            _ambienceSource.volume = _sfxVolume * _masterVolume * 0.6f;
            _ambienceSource.Play();
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            PlayerPrefs.SetFloat(PREF_MASTER, _masterVolume);
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            PlayerPrefs.SetFloat(PREF_MUSIC, _musicVolume);
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            PlayerPrefs.SetFloat(PREF_SFX, _sfxVolume);
        }

        #endregion

        #region Private Helpers

        private void InitializeSFXPool()
        {
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var go = new GameObject($"SFX_Source_{i}");
                go.transform.SetParent(transform);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _sfxPool.Add(source);
            }
        }

        private AudioSource GetNextSFXSource()
        {
            if (_sfxPool.Count == 0) InitializeSFXPool();

            AudioSource source = _sfxPool[_nextSfxIndex];
            _nextSfxIndex = (_nextSfxIndex + 1) % _sfxPool.Count;
            return source;
        }

        private void LoadVolumeSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(PREF_MASTER, _masterVolume);
            _musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC, _musicVolume);
            _sfxVolume = PlayerPrefs.GetFloat(PREF_SFX, _sfxVolume);
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null)
                _musicSource.volume = _musicVolume * _masterVolume;

            if (_ambienceSource != null)
                _ambienceSource.volume = _sfxVolume * _masterVolume * 0.6f;
        }

        #endregion
    }
}
