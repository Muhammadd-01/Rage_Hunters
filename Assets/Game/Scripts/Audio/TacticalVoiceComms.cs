using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace RageHunters.Audio
{
    /// <summary>
    /// Production-grade tactical voice communication and radio transmission manager.
    /// Synchronizes synthesized voice audio with an animated military HUD transmission banner,
    /// dynamic radio squelches, audio frequency visualizers, and typewriter subtitles.
    /// </summary>
    public class TacticalVoiceComms : Core.Singleton<TacticalVoiceComms>
    {
        private GameObject _commsBanner;
        private TextMeshProUGUI _speakerLabel;
        private TextMeshProUGUI _messageLabel;
        private TextMeshProUGUI _waveformLabel;
        private Coroutine _activeTransmission;
        private bool _isTransmitting;

        private readonly string[] _waveformFrames = new string[]
        {
            " ▃▅▆▇▆▅▃ ",
            "▃▅▇█▇▅▃  ",
            "▅▇█▇▆▅▃▂ ",
            "▇▆▅▃▂ ▂▃▅",
            "█▇▆▅▃  ▃▅",
            "▆▅▃▂ ▂▃▅▇"
        };

        protected override void OnInitialized()
        {
            CreateCommsHUD();
        }

        public void BindHUD(GameObject banner, TextMeshProUGUI speaker, TextMeshProUGUI message, TextMeshProUGUI waveform)
        {
            _commsBanner = banner;
            _speakerLabel = speaker;
            _messageLabel = message;
            _waveformLabel = waveform;
            if (_commsBanner != null) _commsBanner.SetActive(false);
        }

        private void CreateCommsHUD()
        {
            // If already created or bound, skip
            if (_commsBanner != null) return;

            var canvas = GameObject.Find("GameCanvas");
            if (canvas == null) return;

            var bannerGo = new GameObject("RadioCommsBanner");
            bannerGo.transform.SetParent(canvas.transform, false);
            var rt = bannerGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -125f);
            rt.sizeDelta = new Vector2(640f, 65f);

            var img = bannerGo.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.04f, 0.08f, 0.12f, 0.92f);

            // Waveform Graphic Text (Left)
            var waveGo = new GameObject("WaveformText");
            waveGo.transform.SetParent(bannerGo.transform, false);
            var waveRt = waveGo.AddComponent<RectTransform>();
            waveRt.anchorMin = new Vector2(0f, 0f);
            waveRt.anchorMax = new Vector2(0.18f, 1f);
            waveRt.offsetMin = Vector2.zero;
            waveRt.offsetMax = Vector2.zero;
            _waveformLabel = waveGo.AddComponent<TextMeshProUGUI>();
            _waveformLabel.text = "▃▅▇█▇▅▃";
            _waveformLabel.fontSize = 18;
            _waveformLabel.alignment = TextAlignmentOptions.Center;
            _waveformLabel.color = new Color(0.2f, 0.85f, 0.95f);

            // Speaker Tag (Top Right)
            var spkGo = new GameObject("SpeakerTag");
            spkGo.transform.SetParent(bannerGo.transform, false);
            var spkRt = spkGo.AddComponent<RectTransform>();
            spkRt.anchorMin = new Vector2(0.19f, 0.5f);
            spkRt.anchorMax = new Vector2(1f, 1f);
            spkRt.offsetMin = new Vector2(8f, 0f);
            spkRt.offsetMax = new Vector2(-10f, 0f);
            _speakerLabel = spkGo.AddComponent<TextMeshProUGUI>();
            _speakerLabel.text = "[HQ TACTICAL COMMAND]";
            _speakerLabel.fontSize = 13;
            _speakerLabel.alignment = TextAlignmentOptions.Left;
            _speakerLabel.color = new Color(0.95f, 0.75f, 0.15f);

            // Message Body (Bottom Right)
            var msgGo = new GameObject("MessageText");
            msgGo.transform.SetParent(bannerGo.transform, false);
            var msgRt = msgGo.AddComponent<RectTransform>();
            msgRt.anchorMin = new Vector2(0.19f, 0f);
            msgRt.anchorMax = new Vector2(1f, 0.55f);
            msgRt.offsetMin = new Vector2(8f, 0f);
            msgRt.offsetMax = new Vector2(-10f, 0f);
            _messageLabel = msgGo.AddComponent<TextMeshProUGUI>();
            _messageLabel.text = "Radio channel open.";
            _messageLabel.fontSize = 14;
            _messageLabel.alignment = TextAlignmentOptions.Left;
            _messageLabel.color = Color.white;

            _commsBanner = bannerGo;
            _commsBanner.SetActive(false);
        }

        #region Public Dispatch API

        public static void Transmit(string speaker, string message, SoundType soundType, float duration = 3.2f, Color? speakerColor = null)
        {
            if (Instance == null) return;
            Instance.PlayComms(speaker, message, soundType, duration, speakerColor);
        }

        public void PlayComms(string speaker, string message, SoundType soundType, float duration = 3.2f, Color? speakerColor = null)
        {
            CreateCommsHUD();

            if (_activeTransmission != null)
            {
                StopCoroutine(_activeTransmission);
            }

            _activeTransmission = StartCoroutine(TransmissionRoutine(speaker, message, soundType, duration, speakerColor ?? new Color(0.95f, 0.75f, 0.15f)));
        }

        private IEnumerator TransmissionRoutine(string speaker, string message, SoundType soundType, float duration, Color speakerColor)
        {
            _isTransmitting = true;

            // 1. Radio squelch click
            SoundFXService.Play(SoundType.RadioStaticSquelch, 0.7f);
            yield return new WaitForSeconds(0.08f);

            // 2. Play voiced audio stinger
            SoundFXService.Play(soundType, 0.95f);

            if (_commsBanner != null)
            {
                _commsBanner.SetActive(true);
                if (_speakerLabel != null)
                {
                    _speakerLabel.text = $"[ {speaker.ToUpper()} ]";
                    _speakerLabel.color = speakerColor;
                }
                if (_messageLabel != null)
                {
                    _messageLabel.text = message;
                }
            }

            // 3. Animate audio waveform and typewriter text
            float elapsed = 0f;
            int frameIdx = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (_waveformLabel != null)
                {
                    frameIdx = (frameIdx + 1) % _waveformFrames.Length;
                    _waveformLabel.text = _waveformFrames[frameIdx];
                }
                yield return new WaitForSeconds(0.08f);
            }

            // 4. End squelch
            SoundFXService.Play(SoundType.RadioStaticSquelch, 0.5f);

            if (_commsBanner != null)
            {
                _commsBanner.SetActive(false);
            }

            _isTransmitting = false;
            _activeTransmission = null;
        }

        #endregion

        #region Preset Tactical Cues

        public void CueMatchStart()
        {
            PlayComms("HQ COMMAND // DISPATCH", "Operation live. Infiltrate office sector, eliminate targets, and secure extraction.", SoundType.Voice_MatchStart, 3.8f, new Color(0.2f, 0.85f, 0.95f));
        }

        public void CueThreatSpotted(string villainName)
        {
            PlayComms("TACTICAL SENSOR", $"ALERT: High-threat hostile [{villainName}] detected in your sector!", SoundType.Voice_ThreatDetected, 3.2f, new Color(0.95f, 0.2f, 0.2f));
        }

        public void CueLowHealth()
        {
            PlayComms("BIO-MONITOR // CRITICAL", "Vital signs dropping rapidly! Inject trauma medkit immediately!", SoundType.Voice_LowHealth, 2.8f, new Color(1f, 0.15f, 0.15f));
        }

        public void CueExtractionInitiated()
        {
            PlayComms("EVAC TRANSPONDER", "Beacon active! Holding extraction grid for 10 seconds. Defend position!", SoundType.Voice_ExtractionInitiated, 4.0f, new Color(0.2f, 0.95f, 0.35f));
        }

        public void CueExtractionSuccess()
        {
            PlayComms("HQ COMMAND // EVAC", "Hostiles cleared. Evacuation confirmed! Outstanding survival performance.", SoundType.Voice_ExtractionSuccess, 4.5f, new Color(0.2f, 0.95f, 0.35f));
        }

        public void CueVillainTaunt(string villainName, string taunt)
        {
            PlayComms(villainName, $"\"{taunt}\"", SoundType.Voice_VillainLaugh, 2.8f, new Color(0.85f, 0.2f, 0.85f));
        }

        #endregion
    }
}
