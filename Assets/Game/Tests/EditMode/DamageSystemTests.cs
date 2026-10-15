#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using RageHunters.Combat;

namespace RageHunters.Tests.EditMode
{
    public class DamageSystemTests
    {
        [Test]
        public void CalculateSimpleDamage_ZeroArmor_ReturnsExpectedBaseDamage()
        {
            var attacker = AttackerStats.Default;
            attacker.AttackPower = 1.0f;
            attacker.CriticalChance = 0f; // Disable crit for deterministic test
            attacker.ArmorPenetration = 0f;

            var defender = DefenderStats.Default;
            defender.Armor = 0f;
            defender.DamageResistance = 0f;

            float damage = DamageSystem.CalculateSimpleDamage(50f, attacker, defender);

            Assert.AreEqual(50f, damage, 0.1f, "Damage should match base damage when defender has 0 armor and resistance.");
        }

        [Test]
        public void CalculateSimpleDamage_WithArmor_MitigatesDamage()
        {
            var attacker = AttackerStats.Default;
            attacker.AttackPower = 1.0f;
            attacker.CriticalChance = 0f;
            attacker.ArmorPenetration = 0f;

            var defender = DefenderStats.Default;
            defender.Armor = 50f; // At ArmorCoefficient = 50, 50/(50+50) = 50% mitigation
            defender.DamageResistance = 0f;

            float damage = DamageSystem.CalculateSimpleDamage(100f, attacker, defender);

            Assert.AreEqual(50f, damage, 1.0f, "50 armor should mitigate approximately 50% of 100 incoming damage.");
        }

        [Test]
        public void CalculateSimpleDamage_WithArmorPenetration_BypassesMitigation()
        {
            var attacker = AttackerStats.Default;
            attacker.AttackPower = 1.0f;
            attacker.CriticalChance = 0f;
            attacker.ArmorPenetration = 1.0f; // 100% armor penetration

            var defender = DefenderStats.Default;
            defender.Armor = 100f;
            defender.DamageResistance = 0f;

            float damage = DamageSystem.CalculateSimpleDamage(80f, attacker, defender);

            Assert.AreEqual(80f, damage, 0.1f, "100% armor penetration should bypass all defender armor.");
        }

        [Test]
        public void CalculateSimpleDamage_HeadshotMultiplier_IncreasesDamage()
        {
            var attacker = AttackerStats.Default;
            attacker.AttackPower = 1.0f;
            attacker.CriticalChance = 0f;

            var defender = DefenderStats.Default;

            float bodyDamage = DamageSystem.CalculateSimpleDamage(40f, attacker, defender, 1.0f);
            float headDamage = DamageSystem.CalculateSimpleDamage(40f, attacker, defender, 2.0f);

            Assert.AreEqual(bodyDamage * 2f, headDamage, 0.1f, "Headshot (2.0x) should double final damage.");
        }

        [Test]
        public void CalculateSimpleDamage_MinimumFloor_NeverBelowOne()
        {
            var attacker = AttackerStats.Default;
            attacker.AttackPower = 0.1f;
            attacker.CriticalChance = 0f;

            var defender = DefenderStats.Default;
            defender.Armor = 1000f;
            defender.DamageResistance = 0.95f;

            float damage = DamageSystem.CalculateSimpleDamage(0.01f, attacker, defender);

            Assert.GreaterOrEqual(damage, 1.0f, "Damage should never fall below the minimum damage floor of 1.0.");
        }
    }
}
#endif
