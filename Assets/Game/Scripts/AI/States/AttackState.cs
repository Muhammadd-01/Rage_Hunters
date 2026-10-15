using UnityEngine;

namespace RageHunters.AI.States
{
    /// <summary>
    /// AI State: Enemy confronts target in melee/close range, executes attacks within cooldown intervals,
    /// and faces the opponent. Returns to Chase if the target retreats out of range.
    /// </summary>
    public class AttackState : AIState
    {
        #region Constructor

        public AttackState(AIController controller) : base(controller)
        {
        }

        #endregion

        #region State Lifecycle

        public override void Enter()
        {
            Controller.StopMovement();
        }

        public override void Update()
        {
            // 1. Health check: retreat if critical
            if (Controller.Archetype != null)
            {
                float healthRatio = Controller.CurrentHealth / Controller.MaxHealth;
                if (healthRatio <= Controller.Archetype.RetreatHealthThreshold)
                {
                    Controller.StateMachine.TransitionTo(AIStateType.Retreat);
                    return;
                }
            }

            // 2. Target validation
            if (!Controller.HasTarget)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Search);
                return;
            }

            float combatRange = Controller.Archetype != null
                ? Controller.Archetype.PreferredCombatRange
                : 2.0f;

            float distanceToTarget = Vector3.Distance(Controller.transform.position, Controller.Target.position);

            // 3. Distance check: if target escaped combat range, resume Chase
            if (distanceToTarget > combatRange * 1.35f)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Chase);
                return;
            }

            // 4. Face target
            Controller.RotateTowards(Controller.Target.position, 15f);

            // 5. Strike execution
            Controller.TryPerformAttack();
        }

        public override void Exit()
        {
            Controller.ResumeMovement();
        }

        #endregion
    }
}
