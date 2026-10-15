using UnityEngine;

namespace RageHunters.AI.States
{
    /// <summary>
    /// AI State: Enemy aggressively pursues the target using high movement speed.
    /// Transitions to Attack when in range, Search if target breaks sight, or Retreat if health is critical.
    /// </summary>
    public class ChaseState : AIState
    {
        #region Private Fields

        private float _repathTimer;
        private const float RepathInterval = 0.25f; // Throttle path recalculations for mobile performance
        private float _targetLostTimer;
        private const float TargetLostGracePeriod = 2.0f;

        #endregion

        #region Constructor

        public ChaseState(AIController controller) : base(controller)
        {
        }

        #endregion

        #region State Lifecycle

        public override void Enter()
        {
            float chaseSpeed = Controller.Archetype != null ? Controller.Archetype.ChaseSpeed : 5.5f;
            Controller.SetSpeed(chaseSpeed);
            Controller.ResumeMovement();

            _repathTimer = 0f;
            _targetLostTimer = 0f;

            if (Controller.HasTarget)
            {
                Controller.SetDestination(Controller.Target.position);
            }
        }

        public override void Update()
        {
            // 1. Check critical health retreat condition
            if (Controller.Archetype != null)
            {
                float healthRatio = Controller.CurrentHealth / Controller.MaxHealth;
                if (healthRatio <= Controller.Archetype.RetreatHealthThreshold)
                {
                    Controller.StateMachine.TransitionTo(AIStateType.Retreat);
                    return;
                }
            }

            // 2. Validate target presence
            if (!Controller.HasTarget)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Search);
                return;
            }

            float combatRange = Controller.Archetype != null
                ? Controller.Archetype.PreferredCombatRange
                : 2.0f;

            float distanceToTarget = Vector3.Distance(Controller.transform.position, Controller.Target.position);

            // 3. Check Attack range transition
            if (distanceToTarget <= combatRange)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Attack);
                return;
            }

            // 4. Check Line of Sight
            bool canSee = Controller.CanSeeTarget(Controller.Target);
            if (canSee)
            {
                _targetLostTimer = 0f;
                Controller.LastKnownTargetPosition = Controller.Target.position;
            }
            else
            {
                _targetLostTimer += Time.deltaTime;
                if (_targetLostTimer >= TargetLostGracePeriod)
                {
                    // Target lost: transition to Search at last known position
                    Controller.StateMachine.TransitionTo(AIStateType.Search);
                    return;
                }
            }

            // 5. Throttled path updating towards target
            _repathTimer += Time.deltaTime;
            if (_repathTimer >= RepathInterval)
            {
                _repathTimer = 0f;
                Controller.SetDestination(Controller.Target.position);
            }
        }

        public override void Exit()
        {
        }

        #endregion
    }
}
