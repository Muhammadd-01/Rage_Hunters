using UnityEngine;
using UnityEngine.AI;

namespace RageHunters.AI.States
{
    /// <summary>
    /// AI State: Enemy disengages and flees away from the hostile target when health drops below critical threshold.
    /// Transitions to ReturnToPatrol once safety distance is achieved or target is lost.
    /// </summary>
    public class RetreatState : AIState
    {
        #region Private Fields

        private float _repathTimer;
        private const float RepathInterval = 0.5f;
        private const float SafeDistance = 25f;

        #endregion

        #region Constructor

        public RetreatState(AIController controller) : base(controller)
        {
        }

        #endregion

        #region State Lifecycle

        public override void Enter()
        {
            float speed = Controller.Archetype != null ? Controller.Archetype.RetreatSpeed : 6.0f;
            Controller.SetSpeed(speed);
            Controller.ResumeMovement();

            _repathTimer = 0f;
            CalculateAndSetRetreatDestination();
        }

        public override void Update()
        {
            // 1. If target is gone, return to patrol
            if (!Controller.HasTarget)
            {
                Controller.StateMachine.TransitionTo(AIStateType.ReturnToPatrol);
                return;
            }

            float distanceToTarget = Vector3.Distance(Controller.transform.position, Controller.Target.position);

            // 2. Safe distance reached
            if (distanceToTarget >= SafeDistance)
            {
                Controller.ClearTarget();
                Controller.StateMachine.TransitionTo(AIStateType.ReturnToPatrol);
                return;
            }

            // 3. Periodic flee path recalculation
            _repathTimer += Time.deltaTime;
            if (_repathTimer >= RepathInterval)
            {
                _repathTimer = 0f;
                CalculateAndSetRetreatDestination();
            }
        }

        public override void Exit()
        {
        }

        #endregion

        #region Helpers

        private void CalculateAndSetRetreatDestination()
        {
            if (!Controller.HasTarget) return;

            Vector3 fleeDirection = (Controller.transform.position - Controller.Target.position).normalized;
            Vector3 rawTarget = Controller.transform.position + fleeDirection * 15f;

            if (NavMesh.SamplePosition(rawTarget, out var hit, 10f, NavMesh.AllAreas))
            {
                Controller.SetDestination(hit.position);
            }
        }

        #endregion
    }
}
