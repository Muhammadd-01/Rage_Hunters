using UnityEngine;

namespace RageHunters.Combat
{
    /// <summary>
    /// Interface for any entity or object that can receive combat damage.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Current health points of the entity.
        /// </summary>
        float CurrentHealth { get; }

        /// <summary>
        /// Maximum health points of the entity.
        /// </summary>
        float MaxHealth { get; }

        /// <summary>
        /// Whether the entity is currently dead/destroyed.
        /// </summary>
        bool IsDead { get; }

        /// <summary>
        /// Apply damage to this entity.
        /// </summary>
        /// <param name="amount">Base or modified damage amount.</param>
        /// <param name="hitPoint">Impact coordinate in world space.</param>
        /// <param name="attacker">GameObject responsible for inflicting damage (can be null).</param>
        void TakeDamage(float amount, Vector3 hitPoint, GameObject attacker);
    }
}
