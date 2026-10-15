using System;
using UnityEngine;

namespace RageHunters.Core
{
    /// <summary>
    /// Animates procedural world objects including holographic extraction beacons,
    /// levitating loot items with continuous rotation and light pulses,
    /// and atmospheric fluorescent office lighting flickers.
    /// </summary>
    public class ProceduralEnvironmentAnimator : MonoBehaviour
    {
        public enum TargetType
        {
            ExtractionBeacon,
            FloatingLoot,
            FlickeringLight
        }

        [SerializeField] private TargetType _targetType;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Light _light;
        private float _initialLightIntensity;
        private float _flickerTimer;

        public void Init(TargetType type)
        {
            _targetType = type;
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;

            _light = GetComponent<Light>();
            if (_light != null)
            {
                _initialLightIntensity = _light.intensity;
            }
        }

        private void Start()
        {
            if (_initialPosition == Vector3.zero && transform.position != Vector3.zero)
            {
                _initialPosition = transform.position;
                _initialRotation = transform.rotation;
            }

            if (_light == null)
            {
                _light = GetComponent<Light>();
                if (_light != null)
                {
                    _initialLightIntensity = _light.intensity;
                }
            }
        }

        private void Update()
        {
            switch (_targetType)
            {
                case TargetType.ExtractionBeacon:
                    AnimateExtractionBeacon();
                    break;

                case TargetType.FloatingLoot:
                    AnimateFloatingLoot();
                    break;

                case TargetType.FlickeringLight:
                    AnimateFlickeringLight();
                    break;
            }
        }

        private void AnimateExtractionBeacon()
        {
            // Rotate cylindrical beam and pulse height
            transform.Rotate(Vector3.up, 30f * Time.deltaTime, Space.World);
            float pulse = Mathf.Sin(Time.time * 3f) * 0.15f;
            transform.localScale = new Vector3(1f + pulse * 0.1f, 1f + pulse, 1f + pulse * 0.1f);
        }

        private void AnimateFloatingLoot()
        {
            // Sine wave floating hover bobbing
            float hover = Mathf.Sin(Time.time * 2.8f) * 0.12f;
            transform.position = _initialPosition + new Vector3(0f, hover, 0f);

            // 360-degree continuous rotation
            transform.Rotate(Vector3.up, 55f * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right, 20f * Time.deltaTime, Space.Self);
        }

        private void AnimateFlickeringLight()
        {
            if (_light == null) return;

            _flickerTimer -= Time.deltaTime;
            if (_flickerTimer <= 0f)
            {
                _flickerTimer = UnityEngine.Random.Range(0.08f, 0.45f);
                bool drop = UnityEngine.Random.value < 0.25f;
                _light.intensity = drop ? _initialLightIntensity * 0.3f : _initialLightIntensity;
            }
        }
    }
}
