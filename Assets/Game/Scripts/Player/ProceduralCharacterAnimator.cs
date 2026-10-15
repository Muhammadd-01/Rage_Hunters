using System;
using UnityEngine;
using RageHunters.Audio;

namespace RageHunters.Player
{
    /// <summary>
    /// Production-grade procedural animation system for character models.
    /// Provides dynamic breathing, natural head sway, velocity-driven walk/run strides,
    /// 3-hit melee combat combos, airborne jump physics, crouch sneak postures,
    /// sprint leaning, hit flinches, villain threat displays, and death collapse kinematics.
    /// </summary>
    public class ProceduralCharacterAnimator : MonoBehaviour
    {
        #region Bone / Part References

        [Header("Model Structure")]
        [SerializeField] private Transform _torso;
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _leftArm;
        [SerializeField] private Transform _rightArm;
        [SerializeField] private Transform _leftLeg;
        [SerializeField] private Transform _rightLeg;
        [SerializeField] private Transform _weapon;

        #endregion

        #region Motion Parameters

        [Header("Gait Settings")]
        [SerializeField] private float _walkFrequency = 5.2f;
        [SerializeField] private float _sprintFrequency = 8.8f;
        [SerializeField] private float _crouchFrequency = 3.6f;
        [SerializeField] private float _legSwingAngle = 32f;
        [SerializeField] private float _armSwingAngle = 36f;
        [SerializeField] private float _torsoBobAmount = 0.05f;

        #endregion

        #region State Fields

        private CharacterController _characterController;
        private Vector3 _initialTorsoPos;
        private Quaternion _initialTorsoRot;
        private Quaternion _initialHeadRot;
        private Quaternion _initialLeftArmRot;
        private Quaternion _initialRightArmRot;
        private Quaternion _initialLeftLegRot;
        private Quaternion _initialRightLegRot;
        private Quaternion _initialWeaponRot;

        private float _strideCycle;
        private float _lastStepHalfCycle;

        // Combat Combo System
        private bool _isAttacking;
        private float _attackTime;
        private float _attackDuration = 0.32f;
        private int _currentComboStep = 0;
        private float _lastAttackTimestamp;
        private const float COMBO_WINDOW = 0.75f;

        // Airborne & Jump Physics
        private bool _wasGrounded = true;
        private float _landingSquashTime;
        private const float SQUASH_DURATION = 0.22f;

        // Crouch State
        private bool _isCrouching;

        // Flinch & Pain
        private float _flinchTime;
        private float _flinchDuration = 0.22f;

        // Villain Threat Roar & Death
        private bool _isRoaring;
        private float _roarTime;
        private bool _isDead;
        private float _deathTime;

        #endregion

        public bool IsDead => _isDead;
        public bool IsCrouching => _isCrouching;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            AutoDetectBones();
            CacheInitialTransforms();
        }

        public void AutoDetectBones()
        {
            var model = transform.Find("Model");
            if (model == null) return;

            if (_torso == null) _torso = model.Find("Torso");
            if (_head == null) _head = model.Find("Head");
            if (_leftArm == null) _leftArm = model.Find("Arm_L");
            if (_rightArm == null) _rightArm = model.Find("Arm_R");
            if (_leftLeg == null) _leftLeg = model.Find("Leg_L");
            if (_rightLeg == null) _rightLeg = model.Find("Leg_R");

            if (_rightArm != null && _weapon == null)
            {
                _weapon = _rightArm.Find("Baton");
            }
        }

        private void CacheInitialTransforms()
        {
            if (_torso != null)
            {
                _initialTorsoPos = _torso.localPosition;
                _initialTorsoRot = _torso.localRotation;
            }
            if (_head != null) _initialHeadRot = _head.localRotation;
            if (_leftArm != null) _initialLeftArmRot = _leftArm.localRotation;
            if (_rightArm != null) _initialRightArmRot = _rightArm.localRotation;
            if (_leftLeg != null) _initialLeftLegRot = _leftLeg.localRotation;
            if (_rightLeg != null) _initialRightLegRot = _rightLeg.localRotation;
            if (_weapon != null) _initialWeaponRot = _weapon.localRotation;
        }

        private void Update()
        {
            if (_isDead)
            {
                UpdateDeathCollapse();
                return;
            }

            if (_isRoaring)
            {
                UpdateRoar();
                return;
            }

            float speed = _characterController != null ? _characterController.velocity.magnitude : 0f;
            bool isMoving = speed > 0.15f;
            bool isSprinting = speed > 5.5f;
            bool isGrounded = _characterController == null || _characterController.isGrounded;

            UpdateAirborneAndLanding(isGrounded);
            UpdateLocomotion(isMoving, isSprinting, isGrounded);
            UpdateIdleBreathing(isMoving, isGrounded);
            UpdateCombatCombo();
            UpdateFlinch();
        }

        #region Locomotion & Footsteps

        private void UpdateAirborneAndLanding(bool isGrounded)
        {
            // Just landed
            if (!_wasGrounded && isGrounded)
            {
                _landingSquashTime = SQUASH_DURATION;
                SoundFXService.Play(SoundType.Footstep, 0.75f);
            }
            _wasGrounded = isGrounded;

            if (_landingSquashTime > 0f)
            {
                _landingSquashTime -= Time.deltaTime;
            }
        }

        private void UpdateLocomotion(bool isMoving, bool isSprinting, bool isGrounded)
        {
            if (!isGrounded)
            {
                // In-Air Airborne Pose: Legs tucked backward, arms out for balance
                if (_leftLeg != null)
                    _leftLeg.localRotation = Quaternion.Slerp(_leftLeg.localRotation, _initialLeftLegRot * Quaternion.Euler(-28f, 0f, 0f), 10f * Time.deltaTime);
                if (_rightLeg != null)
                    _rightLeg.localRotation = Quaternion.Slerp(_rightLeg.localRotation, _initialRightLegRot * Quaternion.Euler(-28f, 0f, 0f), 10f * Time.deltaTime);

                if (!_isAttacking)
                {
                    if (_leftArm != null)
                        _leftArm.localRotation = Quaternion.Slerp(_leftArm.localRotation, _initialLeftArmRot * Quaternion.Euler(-20f, 0f, -25f), 10f * Time.deltaTime);
                    if (_rightArm != null)
                        _rightArm.localRotation = Quaternion.Slerp(_rightArm.localRotation, _initialRightArmRot * Quaternion.Euler(-20f, 0f, 25f), 10f * Time.deltaTime);
                }
                return;
            }

            if (isMoving)
            {
                float freq = _isCrouching ? _crouchFrequency : (isSprinting ? _sprintFrequency : _walkFrequency);
                _strideCycle += Time.deltaTime * freq;

                float swingPhase = Mathf.Sin(_strideCycle);
                float legAngle = swingPhase * _legSwingAngle * (isSprinting ? 1.35f : (_isCrouching ? 0.7f : 1.0f));
                float armAngle = -swingPhase * _armSwingAngle * (isSprinting ? 1.45f : (_isCrouching ? 0.6f : 1.0f));

                // Alternate Leg swings
                if (_leftLeg != null)
                    _leftLeg.localRotation = _initialLeftLegRot * Quaternion.Euler(legAngle, 0f, 0f);
                if (_rightLeg != null)
                    _rightLeg.localRotation = _initialRightLegRot * Quaternion.Euler(-legAngle, 0f, 0f);

                // Counter-phase Arm swings (unless attacking)
                if (!_isAttacking)
                {
                    if (_leftArm != null)
                        _leftArm.localRotation = _initialLeftArmRot * Quaternion.Euler(armAngle, 0f, 0f);
                    if (_rightArm != null)
                        _rightArm.localRotation = _initialRightArmRot * Quaternion.Euler(-armAngle, 0f, 0f);
                }

                // Vertical Torso Bobbing + Lean
                if (_torso != null)
                {
                    float bob = Mathf.Abs(Mathf.Sin(_strideCycle)) * _torsoBobAmount;
                    float forwardLean = _isCrouching ? 22f : (isSprinting ? 16f : 5f);
                    float crouchHeightDrop = _isCrouching ? -0.28f : 0f;

                    // Landing squash addition
                    if (_landingSquashTime > 0f)
                    {
                        bob -= (_landingSquashTime / SQUASH_DURATION) * 0.12f;
                    }

                    _torso.localPosition = _initialTorsoPos + new Vector3(0f, bob + crouchHeightDrop, 0f);
                    _torso.localRotation = _initialTorsoRot * Quaternion.Euler(forwardLean, 0f, 0f);
                }

                // Footstep sound trigger at ground strike
                float currentHalfCycle = Mathf.Floor(_strideCycle / Mathf.PI);
                if (currentHalfCycle > _lastStepHalfCycle)
                {
                    _lastStepHalfCycle = currentHalfCycle;
                    if (!_isCrouching)
                    {
                        SoundFXService.Play(SoundType.Footstep, isSprinting ? 0.75f : 0.45f);
                    }
                }
            }
            else
            {
                // Return to rest posture
                float crouchHeightDrop = _isCrouching ? -0.28f : 0f;
                float squashDrop = (_landingSquashTime > 0f) ? -(_landingSquashTime / SQUASH_DURATION) * 0.12f : 0f;

                if (_torso != null)
                {
                    _torso.localPosition = Vector3.Lerp(_torso.localPosition, _initialTorsoPos + new Vector3(0f, crouchHeightDrop + squashDrop, 0f), 10f * Time.deltaTime);
                    _torso.localRotation = Quaternion.Slerp(_torso.localRotation, _initialTorsoRot * Quaternion.Euler(_isCrouching ? 18f : 0f, 0f, 0f), 10f * Time.deltaTime);
                }
                if (_leftLeg != null)
                    _leftLeg.localRotation = Quaternion.Slerp(_leftLeg.localRotation, _initialLeftLegRot, 10f * Time.deltaTime);
                if (_rightLeg != null)
                    _rightLeg.localRotation = Quaternion.Slerp(_rightLeg.localRotation, _initialRightLegRot, 10f * Time.deltaTime);

                if (!_isAttacking)
                {
                    if (_leftArm != null)
                        _leftArm.localRotation = Quaternion.Slerp(_leftArm.localRotation, _initialLeftArmRot, 10f * Time.deltaTime);
                    if (_rightArm != null)
                        _rightArm.localRotation = Quaternion.Slerp(_rightArm.localRotation, _initialRightArmRot, 10f * Time.deltaTime);
                }
            }
        }

        private void UpdateIdleBreathing(bool isMoving, bool isGrounded)
        {
            if (isMoving || !isGrounded) return;

            float breath = Mathf.Sin(Time.time * 2.4f);
            if (_torso != null)
            {
                _torso.localPosition = Vector3.Lerp(_torso.localPosition, _initialTorsoPos + new Vector3(0f, breath * 0.018f + (_isCrouching ? -0.28f : 0f), 0f), 8f * Time.deltaTime);
                _torso.localRotation = Quaternion.Slerp(_torso.localRotation, _initialTorsoRot * Quaternion.Euler((_isCrouching ? 18f : 0f) + breath * 1.5f, 0f, 0f), 8f * Time.deltaTime);
            }
            if (_head != null)
            {
                // Dynamic look-around scanning
                float glanceYaw = Mathf.Sin(Time.time * 0.65f) * 6f;
                _head.localRotation = Quaternion.Slerp(_head.localRotation, _initialHeadRot * Quaternion.Euler(-breath * 2f, glanceYaw, 0f), 8f * Time.deltaTime);
            }
        }

        public void SetCrouching(bool crouching)
        {
            _isCrouching = crouching;
        }

        public void TriggerJump()
        {
            SoundFXService.Play(SoundType.Voice_PlayerGrunt, 0.7f);
        }

        #endregion

        #region 3-Hit Combat Combo System

        public int TriggerAttackAnimation()
        {
            if (Time.time - _lastAttackTimestamp < COMBO_WINDOW)
            {
                _currentComboStep = (_currentComboStep + 1) % 3;
            }
            else
            {
                _currentComboStep = 0;
            }

            _lastAttackTimestamp = Time.time;
            _isAttacking = true;
            _attackTime = 0f;
            _attackDuration = (_currentComboStep == 2) ? 0.48f : 0.28f; // Combo 3 has heavy windup

            if (_currentComboStep == 2)
            {
                SoundFXService.Play(SoundType.ComboHeavySmash, 1.0f);
                SoundFXService.Play(SoundType.Voice_PlayerGrunt, 0.95f);
            }
            else
            {
                SoundFXService.Play(SoundType.AttackSwing, 0.9f);
            }

            return _currentComboStep + 1; // Return 1, 2, or 3
        }

        private void UpdateCombatCombo()
        {
            if (!_isAttacking) return;

            _attackTime += Time.deltaTime;
            float t = Mathf.Clamp01(_attackTime / _attackDuration);

            switch (_currentComboStep)
            {
                case 0:
                    // Combo 1: Fast Horizontal Right Slash
                    AnimateHorizontalSlash(t);
                    break;
                case 1:
                    // Combo 2: Rising Backhand Diagonal Uppercut
                    AnimateBackhandUppercut(t);
                    break;
                case 2:
                    // Combo 3: Heavy Overhead Two-Handed Executioner Slam
                    AnimateHeavySmash(t);
                    break;
            }

            if (_attackTime >= _attackDuration)
            {
                _isAttacking = false;
            }
        }

        private void AnimateHorizontalSlash(float t)
        {
            if (_rightArm == null) return;
            if (t < 0.3f)
            {
                float p = t / 0.3f;
                _rightArm.localRotation = _initialRightArmRot * Quaternion.Euler(-60f * p, 45f * p, -20f * p);
            }
            else if (t < 0.75f)
            {
                float p = (t - 0.3f) / 0.45f;
                _rightArm.localRotation = _initialRightArmRot * Quaternion.Euler(Mathf.Lerp(-60f, 75f, p), Mathf.Lerp(45f, -50f, p), -15f);
            }
            else
            {
                float p = (t - 0.75f) / 0.25f;
                _rightArm.localRotation = Quaternion.Slerp(_rightArm.localRotation, _initialRightArmRot, p);
            }
        }

        private void AnimateBackhandUppercut(float t)
        {
            if (_rightArm == null) return;
            if (t < 0.35f)
            {
                float p = t / 0.35f;
                _rightArm.localRotation = _initialRightArmRot * Quaternion.Euler(40f * p, -50f * p, 10f * p);
            }
            else if (t < 0.75f)
            {
                float p = (t - 0.35f) / 0.40f;
                _rightArm.localRotation = _initialRightArmRot * Quaternion.Euler(Mathf.Lerp(40f, -80f, p), Mathf.Lerp(-50f, 35f, p), 30f);
            }
            else
            {
                float p = (t - 0.75f) / 0.25f;
                _rightArm.localRotation = Quaternion.Slerp(_rightArm.localRotation, _initialRightArmRot, p);
            }
        }

        private void AnimateHeavySmash(float t)
        {
            if (t < 0.4f)
            {
                // High Overhead Windup with both arms
                float p = t / 0.4f;
                if (_rightArm != null)
                    _rightArm.localRotation = _initialRightArmRot * Quaternion.Euler(-115f * p, 15f * p, 0f);
                if (_leftArm != null)
                    _leftArm.localRotation = _initialLeftArmRot * Quaternion.Euler(-110f * p, -15f * p, 0f);
                if (_torso != null)
                    _torso.localRotation = _initialTorsoRot * Quaternion.Euler(-18f * p, 0f, 0f);
            }
            else if (t < 0.75f)
            {
                // Violent Ground Slam Downward
                float p = (t - 0.4f) / 0.35f;
                if (_rightArm != null)
                    _rightArm.localRotation = _initialRightArmRot * Quaternion.Euler(Mathf.Lerp(-115f, 65f, p), 0f, 0f);
                if (_leftArm != null)
                    _leftArm.localRotation = _initialLeftArmRot * Quaternion.Euler(Mathf.Lerp(-110f, 60f, p), 0f, 0f);
                if (_torso != null)
                    _torso.localRotation = _initialTorsoRot * Quaternion.Euler(Mathf.Lerp(-18f, 25f, p), 0f, 0f);
            }
            else
            {
                // Recovery
                float p = (t - 0.75f) / 0.25f;
                if (_rightArm != null)
                    _rightArm.localRotation = Quaternion.Slerp(_rightArm.localRotation, _initialRightArmRot, p);
                if (_leftArm != null)
                    _leftArm.localRotation = Quaternion.Slerp(_leftArm.localRotation, _initialLeftArmRot, p);
                if (_torso != null)
                    _torso.localRotation = Quaternion.Slerp(_torso.localRotation, _initialTorsoRot, p);
            }
        }

        #endregion

        #region Flinch, Roar, and Death Kinematics

        public void TriggerFlinch()
        {
            _flinchTime = _flinchDuration;
            SoundFXService.Play(SoundType.DamageTaken, 0.85f);
            SoundFXService.Play(SoundType.Voice_PlayerPain, 0.7f);
        }

        private void UpdateFlinch()
        {
            if (_flinchTime <= 0f) return;

            _flinchTime -= Time.deltaTime;
            float flinchFactor = Mathf.Clamp01(_flinchTime / _flinchDuration);

            if (_torso != null)
            {
                _torso.localRotation *= Quaternion.Euler(-20f * flinchFactor, 0f, 0f);
            }
            if (_head != null)
            {
                _head.localRotation *= Quaternion.Euler(-28f * flinchFactor, 0f, 0f);
            }
        }

        public void TriggerVillainRoar()
        {
            _isRoaring = true;
            _roarTime = 0.85f;
            SoundFXService.Play(SoundType.Voice_VillainRoar, 1.0f);
        }

        private void UpdateRoar()
        {
            _roarTime -= Time.deltaTime;
            float p = Mathf.Sin(_roarTime * Mathf.PI);

            if (_head != null)
                _head.localRotation = _initialHeadRot * Quaternion.Euler(-35f * p, UnityEngine.Random.Range(-3f, 3f) * p, 0f);
            if (_leftArm != null)
                _leftArm.localRotation = _initialLeftArmRot * Quaternion.Euler(-45f * p, 0f, -40f * p);
            if (_rightArm != null)
                _rightArm.localRotation = _initialRightArmRot * Quaternion.Euler(-45f * p, 0f, 40f * p);

            if (_roarTime <= 0f)
            {
                _isRoaring = false;
            }
        }

        public void TriggerDeathCollapse()
        {
            _isDead = true;
            _deathTime = 0f;
            SoundFXService.Play(SoundType.Voice_VillainDeath, 1.0f);
        }

        private void UpdateDeathCollapse()
        {
            _deathTime += Time.deltaTime;
            float p = Mathf.Clamp01(_deathTime / 1.1f);

            // Torso drops and tilts backward onto floor
            if (_torso != null)
            {
                _torso.localPosition = Vector3.Lerp(_initialTorsoPos, _initialTorsoPos + new Vector3(0f, -0.75f, -0.4f), p);
                _torso.localRotation = Quaternion.Slerp(_initialTorsoRot, _initialTorsoRot * Quaternion.Euler(-80f, 0f, 15f), p);
            }
            if (_head != null)
            {
                _head.localRotation = Quaternion.Slerp(_initialHeadRot, _initialHeadRot * Quaternion.Euler(-30f, 25f, 0f), p);
            }
            if (_leftLeg != null)
                _leftLeg.localRotation = Quaternion.Slerp(_initialLeftLegRot, _initialLeftLegRot * Quaternion.Euler(30f, 0f, -20f), p);
            if (_rightLeg != null)
                _rightLeg.localRotation = Quaternion.Slerp(_rightLeg.localRotation, _initialRightLegRot * Quaternion.Euler(15f, 0f, 25f), p);
        }

        #endregion
    }
}
