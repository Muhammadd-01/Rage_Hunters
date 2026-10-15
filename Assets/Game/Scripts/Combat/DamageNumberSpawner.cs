using System;
using UnityEngine;
using TMPro;
using RageHunters.Audio;

namespace RageHunters.Combat
{
    /// <summary>
    /// Spawns floating combat damage numbers, hit feedback, and critical strike indicators
    /// in world space with procedural physics bounce and fade animation.
    /// </summary>
    public class DamageNumberSpawner : Core.Singleton<DamageNumberSpawner>
    {
        public static void SpawnDamageNumber(Vector3 worldPosition, float damageAmount, bool isCritical = false)
        {
            if (Instance == null)
            {
                var go = new GameObject("DamageNumberSpawner");
                go.AddComponent<DamageNumberSpawner>();
            }

            Instance.CreatePopup(worldPosition, damageAmount, isCritical);
        }

        private void CreatePopup(Vector3 worldPosition, float damageAmount, bool isCritical)
        {
            var popupGo = new GameObject("DamagePopup");
            popupGo.transform.position = worldPosition + Vector3.up * 1.2f + new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0f, UnityEngine.Random.Range(-0.3f, 0.3f));

            var tmp = popupGo.AddComponent<TextMeshPro>();
            tmp.text = isCritical ? $"CRIT!\n-{Mathf.RoundToInt(damageAmount)}" : $"-{Mathf.RoundToInt(damageAmount)}";
            tmp.fontSize = isCritical ? 4.5f : 3.0f;
            tmp.fontStyle = isCritical ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = isCritical ? new Color(1f, 0.2f, 0.2f) : new Color(1f, 0.85f, 0.2f);

            var animator = popupGo.AddComponent<FloatingDamageNumberAnimator>();
            animator.Init(isCritical);

            // Audio feedback
            SoundFXService.PlayAtPosition(SoundType.HitImpact, worldPosition, isCritical ? 1.0f : 0.75f);
        }
    }

    internal class FloatingDamageNumberAnimator : MonoBehaviour
    {
        private TextMeshPro _text;
        private float _lifetime = 0.85f;
        private float _age;
        private Vector3 _velocity;
        private Transform _camTransform;

        public void Init(bool isCritical)
        {
            _text = GetComponent<TextMeshPro>();
            _velocity = new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), isCritical ? 2.8f : 2.0f, UnityEngine.Random.Range(-0.4f, 0.4f));
            if (UnityEngine.Camera.main != null)
            {
                _camTransform = UnityEngine.Camera.main.transform;
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float progress = _age / _lifetime;

            // Float upward and outward
            transform.position += _velocity * Time.deltaTime;
            _velocity.y -= 2.0f * Time.deltaTime; // gentle gravity drag

            // Face camera billboard
            if (_camTransform != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _camTransform.position);
            }

            // Punchy scale pop and alpha fadeout
            if (_text != null)
            {
                float scale = progress < 0.2f ? Mathf.Lerp(0.5f, 1.25f, progress / 0.2f) : Mathf.Lerp(1.25f, 0.8f, (progress - 0.2f) / 0.8f);
                transform.localScale = Vector3.one * scale;

                Color c = _text.color;
                c.a = Mathf.Clamp01(1f - progress);
                _text.color = c;
            }

            if (_age >= _lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
