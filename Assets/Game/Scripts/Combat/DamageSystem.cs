using UnityEngine;

namespace RageHunters.Combat
{
    /// <summary>
    /// Attacker attributes used in damage calculations.
    /// </summary>
    public struct AttackerStats
    {
        public float AttackPower;
        public float CriticalChance;
        public float CriticalMultiplier;
        public float ArmorPenetration;

        public static AttackerStats Default => new()
        {
            AttackPower = 1.0f,
            CriticalChance = 0.05f,
            CriticalMultiplier = 1.5f,
            ArmorPenetration = 0f
        };
    }

    /// <summary>
    /// Defender attributes used in damage mitigation calculations.
    /// </summary>
    public struct DefenderStats
    {
        public float Armor;
        public float DamageResistance; // 0 to 1 percentage reduction

        public static DefenderStats Default => new()
        {
            Armor = 0f,
            DamageResistance = 0f
        };
    }

    /// <summary>
    /// Result payload of a damage calculation.
    /// </summary>
    public readonly struct DamageCalculationResult
    {
        public float FinalDamage { get; }
        public bool IsCritical { get; }
        public float MitigatedDamage { get; }
        public float BodyPartMultiplier { get; }

        public DamageCalculationResult(float finalDamage, bool isCritical, float mitigatedDamage, float bodyPartMultiplier)
        {
            FinalDamage = finalDamage;
            IsCritical = isCritical;
            MitigatedDamage = mitigatedDamage;
            BodyPartMultiplier = bodyPartMultiplier;
        }
    }

    /// <summary>
    /// Static formula-driven damage calculation system supporting melee and ranged combat,
    /// armor mitigation, critical hits, and body part multipliers.
    /// </summary>
    public static class DamageSystem
    {
        #region Constants

        private const float ArmorCoefficient = 50f; // Armor curve parameter
        private const float MinDamageFloor = 1.0f;

        #endregion

        #region Public API

        /// <summary>
        /// Calculates damage inflicted by an attack based on attacker stats, defender stats, weapon data,
        /// distance, and body part hit multiplier.
        /// </summary>
        public static DamageCalculationResult CalculateDamage(
            AttackerStats attacker,
            DefenderStats defender,
            WeaponDataSO weapon,
            float distance = 0f,
            float bodyPartMultiplier = 1.0f)
        {
            float baseDamage = weapon != null ? weapon.BaseDamage : 10f;
            float weaponCritChance = weapon != null ? weapon.CriticalChance : 0.05f;
            float weaponCritMultiplier = weapon != null ? weapon.CriticalMultiplier : 1.5f;
            WeaponAttackType attackType = weapon != null ? weapon.AttackType : WeaponAttackType.Melee;

            return CalculateRawDamage(
                baseDamage,
                attackType,
                attacker,
                defender,
                weaponCritChance,
                weaponCritMultiplier,
                distance,
                weapon != null ? weapon.Range : 2.0f,
                bodyPartMultiplier);
        }

        /// <summary>
        /// Simple calculation overload without WeaponDataSO for direct damage applications (e.g. basic AI claw/punch).
        /// </summary>
        public static float CalculateSimpleDamage(
            float rawDamage,
            AttackerStats attacker,
            DefenderStats defender,
            float bodyPartMultiplier = 1.0f)
        {
            var result = CalculateRawDamage(
                rawDamage,
                WeaponAttackType.Melee,
                attacker,
                defender,
                attacker.CriticalChance,
                attacker.CriticalMultiplier,
                0f,
                10f,
                bodyPartMultiplier);

            return result.FinalDamage;
        }

        #endregion

        #region Calculation Logic

        private static DamageCalculationResult CalculateRawDamage(
            float baseDamage,
            WeaponAttackType attackType,
            AttackerStats attacker,
            DefenderStats defender,
            float critChance,
            float critMultiplier,
            float distance,
            float effectiveRange,
            float bodyPartMultiplier)
        {
            // 1. Base power scaling
            float damage = baseDamage * Mathf.Max(0.1f, attacker.AttackPower);

            // 2. Ranged distance falloff
            if (attackType == WeaponAttackType.Ranged && distance > effectiveRange)
            {
                float excessDistance = distance - effectiveRange;
                float falloffFactor = Mathf.Clamp01(1f - (excessDistance / Mathf.Max(1f, effectiveRange * 1.5f)));
                damage *= Mathf.Max(0.3f, falloffFactor);
            }

            // 3. Critical strike roll
            float totalCritChance = Mathf.Clamp01(attacker.CriticalChance + critChance);
            bool isCritical = Random.value < totalCritChance;
            if (isCritical)
            {
                float effectiveCritMultiplier = Mathf.Max(1.0f, attacker.CriticalMultiplier * critMultiplier);
                damage *= effectiveCritMultiplier;
            }

            // 4. Body part multiplier (e.g., headshot = 2.0x, limb = 0.75x)
            damage *= Mathf.Max(0.1f, bodyPartMultiplier);

            // 5. Armor reduction: Armor mitigation curve with penetration
            float effectiveArmor = Mathf.Max(0f, defender.Armor * (1f - Mathf.Clamp01(attacker.ArmorPenetration)));
            float armorReductionPercent = effectiveArmor / (effectiveArmor + ArmorCoefficient);
            float preMitigationDamage = damage;
            damage *= (1f - armorReductionPercent);

            // 6. Flat damage resistance
            damage *= (1f - Mathf.Clamp(defender.DamageResistance, 0f, 0.95f));

            float finalDamage = Mathf.Max(MinDamageFloor, Mathf.Round(damage * 10f) / 10f);
            float mitigatedDamage = Mathf.Max(0f, preMitigationDamage - finalDamage);

            return new DamageCalculationResult(finalDamage, isCritical, mitigatedDamage, bodyPartMultiplier);
        }

        #endregion
    }
}
