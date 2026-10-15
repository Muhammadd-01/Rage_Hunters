using System;
using System.Collections.Generic;
using UnityEngine;

namespace RageHunters.Audio
{
    public enum SoundType
    {
        Footstep,
        AttackSwing,
        HitImpact,
        DamageTaken,
        VillainAlertStinger,
        VillainGrowl,
        Heartbeat,
        ExtractionSiren,
        ExtractionSuccess,
        LootPickup,
        UIClick,
        UIBack,
        ObjectiveComplete,
        LevelUpFanfare,
        DarkAtmosphereDrone,
        RadioStaticSquelch,
        Voice_MatchStart,
        Voice_ThreatDetected,
        Voice_LowHealth,
        Voice_ExtractionInitiated,
        Voice_ExtractionSuccess,
        Voice_PlayerGrunt,
        Voice_PlayerPain,
        Voice_VillainRoar,
        Voice_VillainLaugh,
        Voice_VillainDeath,
        MedkitUse,
        KeycardSwipe,
        ComboHeavySmash
    }

    /// <summary>
    /// Production-grade procedural audio synthesis engine for RAGE HUNTERS.
    /// Synthesizes authentic sound effects, ambient tension drones, combat impacts,
    /// and radio voice cues entirely in memory using float PCM buffers,
    /// ensuring zero external audio file dependencies.
    /// </summary>
    public class SoundFXService : Core.Singleton<SoundFXService>
    {
        private const int SAMPLE_RATE = 44100;
        private readonly Dictionary<SoundType, AudioClip> _clipCache = new();
        private AudioSource _ambientSource;
        private AudioSource _heartbeatSource;

        protected override void OnInitialized()
        {
            GenerateAllClips();
            SetupContinuousSources();
            PlayAtmosphere();
        }

        #region Public Playback API

        public static void Play(SoundType type, float volume = 1.0f)
        {
            if (Instance == null) return;
            var clip = Instance.GetClip(type);
            if (clip != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(clip, volume);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(clip, Vector3.zero, volume);
                }
            }
        }

        public static void PlayAtPosition(SoundType type, Vector3 position, float volume = 1.0f)
        {
            if (Instance == null) return;
            var clip = Instance.GetClip(type);
            if (clip != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFXAtPosition(clip, position, volume);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(clip, position, volume);
                }
            }
        }

        public void PlayAtmosphere()
        {
            if (_ambientSource == null) return;
            var clip = GetClip(SoundType.DarkAtmosphereDrone);
            if (clip != null)
            {
                _ambientSource.clip = clip;
                _ambientSource.loop = true;
                _ambientSource.volume = 0.35f;
                _ambientSource.Play();
            }
        }

        public void SetHeartbeatActive(bool active, float speedMultiplier = 1.0f)
        {
            if (_heartbeatSource == null) return;

            if (active)
            {
                if (!_heartbeatSource.isPlaying)
                {
                    _heartbeatSource.clip = GetClip(SoundType.Heartbeat);
                    _heartbeatSource.loop = true;
                    _heartbeatSource.volume = 0.65f;
                    _heartbeatSource.pitch = speedMultiplier;
                    _heartbeatSource.Play();
                }
                else
                {
                    _heartbeatSource.pitch = Mathf.Lerp(_heartbeatSource.pitch, speedMultiplier, Time.deltaTime * 3f);
                }
            }
            else
            {
                if (_heartbeatSource.isPlaying)
                {
                    _heartbeatSource.Stop();
                }
            }
        }

        #endregion

        #region Synthesis Pipeline

        public AudioClip GetClip(SoundType type)
        {
            if (_clipCache.TryGetValue(type, out var clip)) return clip;
            clip = SynthesizeClip(type);
            if (clip != null) _clipCache[type] = clip;
            return clip;
        }

        private void GenerateAllClips()
        {
            foreach (SoundType type in Enum.GetValues(typeof(SoundType)))
            {
                if (!_clipCache.ContainsKey(type))
                {
                    _clipCache[type] = SynthesizeClip(type);
                }
            }
            Debug.Log($"[SoundFXService] Synthesized {_clipCache.Count} procedural audio assets into memory.");
        }

        private void SetupContinuousSources()
        {
            var ambGo = new GameObject("Continuous_Atmosphere");
            ambGo.transform.SetParent(transform);
            _ambientSource = ambGo.AddComponent<AudioSource>();
            _ambientSource.playOnAwake = false;

            var hbGo = new GameObject("Continuous_Heartbeat");
            hbGo.transform.SetParent(transform);
            _heartbeatSource = hbGo.AddComponent<AudioSource>();
            _heartbeatSource.playOnAwake = false;
        }

        private AudioClip SynthesizeClip(SoundType type)
        {
            switch (type)
            {
                case SoundType.Footstep:
                    return GenerateFootstep();

                case SoundType.AttackSwing:
                    return GenerateAttackSwing();

                case SoundType.HitImpact:
                    return GenerateHitImpact();

                case SoundType.DamageTaken:
                    return GenerateDamageTaken();

                case SoundType.VillainAlertStinger:
                    return GenerateAlertStinger();

                case SoundType.VillainGrowl:
                    return GenerateVillainGrowl();

                case SoundType.Heartbeat:
                    return GenerateHeartbeat();

                case SoundType.ExtractionSiren:
                    return GenerateExtractionSiren();

                case SoundType.ExtractionSuccess:
                    return GenerateExtractionSuccess();

                case SoundType.LootPickup:
                    return GenerateLootPickup();

                case SoundType.UIClick:
                    return GenerateUIClick();

                case SoundType.UIBack:
                    return GenerateUIBack();

                case SoundType.ObjectiveComplete:
                    return GenerateObjectiveComplete();

                case SoundType.LevelUpFanfare:
                    return GenerateLevelUpFanfare();

                case SoundType.DarkAtmosphereDrone:
                    return GenerateDarkAtmosphere();

                case SoundType.RadioStaticSquelch:
                    return GenerateRadioStatic();

                case SoundType.Voice_MatchStart:
                    return GenerateVoiceMatchStart();

                case SoundType.Voice_ThreatDetected:
                    return GenerateVoiceThreatDetected();

                case SoundType.Voice_LowHealth:
                    return GenerateVoiceLowHealth();

                case SoundType.Voice_ExtractionInitiated:
                    return GenerateVoiceExtractionInitiated();

                case SoundType.Voice_ExtractionSuccess:
                    return GenerateVoiceExtractionSuccess();

                case SoundType.Voice_PlayerGrunt:
                    return GenerateVoicePlayerGrunt();

                case SoundType.Voice_PlayerPain:
                    return GenerateVoicePlayerPain();

                case SoundType.Voice_VillainRoar:
                    return GenerateVoiceVillainRoar();

                case SoundType.Voice_VillainLaugh:
                    return GenerateVoiceVillainLaugh();

                case SoundType.Voice_VillainDeath:
                    return GenerateVoiceVillainDeath();

                case SoundType.MedkitUse:
                    return GenerateMedkitUse();

                case SoundType.KeycardSwipe:
                    return GenerateKeycardSwipe();

                case SoundType.ComboHeavySmash:
                    return GenerateComboHeavySmash();

                default:
                    return GenerateUIClick();
            }
        }

        #endregion

        #region Waveform Generators

        private AudioClip GenerateFootstep()
        {
            float duration = 0.12f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float noise = (UnityEngine.Random.value * 2f - 1f);
                float env = Mathf.Exp(-progress * 24f);
                float thud = Mathf.Sin(2f * Mathf.PI * 75f * t) * 0.7f;
                data[i] = (noise * 0.3f + thud) * env * 0.6f;
            }

            var clip = AudioClip.Create("SFX_Footstep", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateAttackSwing()
        {
            float duration = 0.22f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float progress = (float)i / samples;
                float freq = Mathf.Lerp(450f, 120f, progress);
                float t = (float)i / SAMPLE_RATE;
                float env = Mathf.Sin(progress * Mathf.PI);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.4f;
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                data[i] = (wave + noise) * env * 0.7f;
            }

            var clip = AudioClip.Create("SFX_AttackSwing", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateHitImpact()
        {
            float duration = 0.28f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float env = Mathf.Exp(-progress * 16f);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.5f;
                float sub = Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.8f;
                float mid = Mathf.Sin(2f * Mathf.PI * 190f * t) * 0.4f;
                data[i] = (noise + sub + mid) * env * 0.9f;
            }

            var clip = AudioClip.Create("SFX_HitImpact", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateDamageTaken()
        {
            float duration = 0.35f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float env = Mathf.Exp(-progress * 12f);
                float grunt = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 90f, progress) * t) * 0.7f;
                float crunch = (UnityEngine.Random.value * 2f - 1f) * 0.3f;
                data[i] = (grunt + crunch) * env * 0.8f;
            }

            var clip = AudioClip.Create("SFX_DamageTaken", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateAlertStinger()
        {
            float duration = 0.6f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float env = Mathf.Exp(-progress * 4f);
                // Dissonant suspense minor second chord (330Hz and 349Hz)
                float f1 = Mathf.Sin(2f * Mathf.PI * 330f * t);
                float f2 = Mathf.Sin(2f * Mathf.PI * 349f * t);
                float sub = Mathf.Sin(2f * Mathf.PI * 82f * t) * 0.6f;
                data[i] = (f1 * 0.4f + f2 * 0.4f + sub) * env * 0.75f;
            }

            var clip = AudioClip.Create("SFX_AlertStinger", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateVillainGrowl()
        {
            float duration = 0.75f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float env = Mathf.Sin(progress * Mathf.PI);
                float mod = Mathf.Sin(2f * Mathf.PI * 18f * t);
                float voice = Mathf.Sin(2f * Mathf.PI * (110f + mod * 20f) * t);
                float sub = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.5f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.15f;
                data[i] = (voice * 0.5f + sub + noise) * env * 0.7f;
            }

            var clip = AudioClip.Create("SFX_VillainGrowl", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateHeartbeat()
        {
            float duration = 0.85f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;

                // Thump 1 at t = 0
                float p1 = Mathf.Clamp01(t / 0.18f);
                float env1 = (t < 0.18f) ? Mathf.Sin(p1 * Mathf.PI) : 0f;
                float thud1 = Mathf.Sin(2f * Mathf.PI * 50f * t) * env1;

                // Thump 2 at t = 0.22
                float t2 = t - 0.22f;
                float p2 = Mathf.Clamp01(t2 / 0.16f);
                float env2 = (t2 >= 0f && t2 < 0.16f) ? Mathf.Sin(p2 * Mathf.PI) : 0f;
                float thud2 = Mathf.Sin(2f * Mathf.PI * 58f * t) * env2 * 0.8f;

                data[i] = (thud1 + thud2) * 0.85f;
            }

            var clip = AudioClip.Create("SFX_Heartbeat", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateExtractionSiren()
        {
            float duration = 1.0f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float freq = 650f + Mathf.Sin(2f * Mathf.PI * 1.5f * t) * 220f;
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                data[i] = wave * 0.5f;
            }

            var clip = AudioClip.Create("SFX_ExtractionSiren", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateExtractionSuccess()
        {
            float duration = 1.6f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            // Ascending triumphant arpeggio: C4, E4, G4, C5
            float[] notes = { 261.63f, 329.63f, 392.00f, 523.25f };
            float noteDur = duration / notes.Length;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                int noteIndex = Mathf.Clamp((int)(t / noteDur), 0, notes.Length - 1);
                float noteT = t - (noteIndex * noteDur);
                float freq = notes[noteIndex];
                float env = Mathf.Exp(-noteT * 3.5f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
                data[i] = wave * env * 0.6f;
            }

            var clip = AudioClip.Create("SFX_ExtractionSuccess", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateLootPickup()
        {
            float duration = 0.35f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float freq = Mathf.Lerp(600f, 1400f, progress);
                float env = Mathf.Exp(-progress * 6f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t);
                data[i] = wave * env * 0.5f;
            }

            var clip = AudioClip.Create("SFX_LootPickup", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateUIClick()
        {
            float duration = 0.06f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float env = Mathf.Exp(-progress * 30f);
                float wave = Mathf.Sin(2f * Mathf.PI * 1800f * t);
                data[i] = wave * env * 0.35f;
            }

            var clip = AudioClip.Create("SFX_UIClick", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateUIBack()
        {
            float duration = 0.08f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float env = Mathf.Exp(-progress * 25f);
                float wave = Mathf.Sin(2f * Mathf.PI * 750f * t);
                data[i] = wave * env * 0.35f;
            }

            var clip = AudioClip.Create("SFX_UIBack", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateObjectiveComplete()
        {
            float duration = 0.65f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float env = Mathf.Exp(-progress * 4f);
                float f1 = Mathf.Sin(2f * Mathf.PI * 523.25f * t); // C5
                float f2 = Mathf.Sin(2f * Mathf.PI * 659.25f * t); // E5
                data[i] = (f1 + f2) * 0.5f * env * 0.65f;
            }

            var clip = AudioClip.Create("SFX_ObjectiveComplete", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateLevelUpFanfare()
        {
            float duration = 1.2f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            float[] chord = { 440f, 554.37f, 659.25f, 880f }; // A Major

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float env = Mathf.Exp(-progress * 3f);
                float sum = 0f;
                foreach (var f in chord)
                {
                    sum += Mathf.Sin(2f * Mathf.PI * f * t);
                }
                data[i] = (sum / chord.Length) * env * 0.7f;
            }

            var clip = AudioClip.Create("SFX_LevelUpFanfare", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateDarkAtmosphere()
        {
            float duration = 4.0f; // 4 second seamless tension loop
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                // Deep subterranean sub drone (48Hz and 52Hz for slow beating)
                float sub1 = Mathf.Sin(2f * Mathf.PI * 48f * t) * 0.5f;
                float sub2 = Mathf.Sin(2f * Mathf.PI * 51.5f * t) * 0.4f;
                // Distant eerie resonance
                float res = Mathf.Sin(2f * Mathf.PI * 115f * t + Mathf.Sin(2f * Mathf.PI * 0.25f * t)) * 0.2f;
                // Low-pass filtered noise
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.08f;
                data[i] = (sub1 + sub2 + res + noise) * 0.5f;
            }

            var clip = AudioClip.Create("AMB_DarkAtmosphere", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateRadioStatic()
        {
            float duration = 0.18f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float env = Mathf.Sin(progress * Mathf.PI);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.7f;
                float click = (i < 80 || i > samples - 80) ? (UnityEngine.Random.value * 2f - 1f) : 0f;
                data[i] = (noise + click) * env * 0.6f;
            }
            var clip = AudioClip.Create("VOX_RadioStatic", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateVoiceMatchStart()
        {
            // Radio beep + Synthetic military dispatcher formant sequence
            float duration = 0.95f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float env = 0f;
                float wave = 0f;

                if (t < 0.12f)
                {
                    // Intro dual-chirp (1600Hz & 2100Hz)
                    wave = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.4f;
                    env = Mathf.Sin((t / 0.12f) * Mathf.PI);
                }
                else
                {
                    // Synthesized tactical dispatcher speech formant pulses
                    float p = (t - 0.12f) / 0.83f;
                    float f0 = 130f + Mathf.Sin(t * 18f) * 15f; // Glottal pulse frequency
                    float f1 = 550f;  // Formant 1
                    float f2 = 1600f; // Formant 2
                    float glottal = (t * f0 % 1.0f) > 0.5f ? 1f : -1f;
                    float formant = (Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.4f);
                    float speechEnv = Mathf.Abs(Mathf.Sin(p * Mathf.PI * 4f)) * Mathf.Exp(-p * 1.5f);
                    wave = glottal * formant * speechEnv;
                    env = 0.65f;
                }
                data[i] = wave * env;
            }
            var clip = AudioClip.Create("VOX_MatchStart", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateVoiceThreatDetected()
        {
            float duration = 0.8f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float freq = Mathf.PingPong(t * 12f, 1f) > 0.5f ? 880f : 660f;
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f;
                float formant = Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.3f;
                float env = Mathf.Exp(-t * 2.5f);
                data[i] = (tone + formant) * env * 0.8f;
            }
            var clip = AudioClip.Create("VOX_ThreatDetected", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateVoiceLowHealth()
        {
            float duration = 0.7f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                // Medical pulse double-beep
                float beepPhase = (t % 0.35f) / 0.35f;
                float beep = (beepPhase < 0.3f) ? Mathf.Sin(2f * Mathf.PI * 1400f * t) : 0f;
                float lowHum = Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.2f;
                data[i] = (beep * 0.6f + lowHum) * 0.75f;
            }
            var clip = AudioClip.Create("VOX_LowHealth", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateVoiceExtractionInitiated()
        {
            float duration = 1.1f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float siren = Mathf.Sin(2f * Mathf.PI * (500f + Mathf.Sin(t * 10f) * 200f) * t) * 0.5f;
                float pulse = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.3f;
                float env = Mathf.Sin(progress * Mathf.PI);
                data[i] = (siren + pulse) * env * 0.7f;
            }
            var clip = AudioClip.Create("VOX_ExtractionInitiated", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateVoiceExtractionSuccess()
        {
            float duration = 1.4f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            float[] chord = { 523.25f, 659.25f, 783.99f, 1046.5f }; // C Major triumph
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float sum = 0f;
                foreach (var f in chord)
                {
                    sum += Mathf.Sin(2f * Mathf.PI * f * t);
                }
                float radio = (UnityEngine.Random.value * 2f - 1f) * 0.05f;
                float env = Mathf.Exp(-progress * 2.2f);
                data[i] = ((sum / chord.Length) * 0.8f + radio) * env * 0.85f;
            }
            var clip = AudioClip.Create("VOX_ExtractionSuccess", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateVoicePlayerGrunt()
        {
            float duration = 0.18f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float pitch = Mathf.Lerp(140f, 95f, progress);
                float vocal = Mathf.Sin(2f * Mathf.PI * pitch * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.25f;
                float env = Mathf.Exp(-progress * 14f);
                data[i] = (vocal * 0.7f + noise) * env * 0.7f;
            }
            var clip = AudioClip.Create("VOX_PlayerGrunt", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateVoicePlayerPain()
        {
            float duration = 0.24f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float pitch = Mathf.Lerp(220f, 130f, progress);
                float vocal = Mathf.Sin(2f * Mathf.PI * pitch * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.35f;
                float env = Mathf.Exp(-progress * 11f);
                data[i] = (vocal * 0.6f + noise) * env * 0.85f;
            }
            var clip = AudioClip.Create("VOX_PlayerPain", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateVoiceVillainRoar()
        {
            float duration = 0.75f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float pitch = Mathf.Lerp(90f, 65f, progress) + Mathf.Sin(t * 30f) * 8f;
                float sub = Mathf.Sin(2f * Mathf.PI * pitch * t);
                float saw = (t * pitch % 1f) * 2f - 1f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.4f;
                float env = Mathf.Sin(progress * Mathf.PI);
                data[i] = (sub * 0.5f + saw * 0.35f + noise) * env * 0.9f;
            }
            var clip = AudioClip.Create("VOX_VillainRoar", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateVoiceVillainLaugh()
        {
            float duration = 0.85f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                // 3 staccato laughter bursts
                float haPhase = (t % 0.28f) / 0.28f;
                float burstEnv = Mathf.Sin(haPhase * Mathf.PI);
                float pitch = 160f - (t * 40f);
                float vocal = Mathf.Sin(2f * Mathf.PI * pitch * t) + Mathf.Sin(2f * Mathf.PI * (pitch * 2.1f) * t) * 0.4f;
                data[i] = vocal * burstEnv * 0.75f;
            }
            var clip = AudioClip.Create("VOX_VillainLaugh", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateVoiceVillainDeath()
        {
            float duration = 1.1f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float pitch = Mathf.Lerp(120f, 40f, progress);
                float vocal = Mathf.Sin(2f * Mathf.PI * pitch * t);
                float rattle = (UnityEngine.Random.value * 2f - 1f) * 0.35f * (1f - progress);
                float env = Mathf.Exp(-progress * 2.5f);
                data[i] = (vocal * 0.7f + rattle) * env * 0.85f;
            }
            var clip = AudioClip.Create("VOX_VillainDeath", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateMedkitUse()
        {
            float duration = 0.45f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                // Syringe hiss + medical chime
                float hiss = (UnityEngine.Random.value * 2f - 1f) * 0.3f * Mathf.Exp(-progress * 6f);
                float chime = Mathf.Sin(2f * Mathf.PI * 1200f * t) * Mathf.Exp(-progress * 4f) * 0.4f;
                data[i] = (hiss + chime) * 0.8f;
            }
            var clip = AudioClip.Create("SFX_MedkitUse", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateKeycardSwipe()
        {
            float duration = 0.22f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float freq = (t < 0.1f) ? 1760f : 2349.3f; // High access beeps
                float env = Mathf.Exp(-((t % 0.11f) / 0.11f) * 8f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.65f;
            }
            var clip = AudioClip.Create("SFX_KeycardSwipe", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateComboHeavySmash()
        {
            float duration = 0.55f;
            int samples = (int)(SAMPLE_RATE * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float progress = (float)i / samples;
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(65f, 25f, progress) * t) * 0.9f;
                float metal = Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.3f;
                float debris = (UnityEngine.Random.value * 2f - 1f) * 0.5f;
                float env = Mathf.Exp(-progress * 7f);
                data[i] = (sub + metal + debris) * env * 0.95f;
            }
            var clip = AudioClip.Create("SFX_ComboHeavySmash", samples, 1, SAMPLE_RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion
    }
}
