using UnityEngine;

namespace RageHunters.AI.States
{
    /// <summary>
    /// AI State: Enemy navigates along assigned patrol waypoints.
    /// Transitions to Detect state upon spotting or hearing a target.
    /// </summary>
    public class PatrolState : AIState
    {
        #region Constructor

        public PatrolState(AIController controller) : base(controller)
        {
        }

        #endregion

        #region State Lifecycle

        public override void Enter()
        {
            float speed = Controller.Archetype != null ? Controller.Archetype.MoveSpeed : 3.5f;
            Controller.SetSpeed(speed);
            Controller.ResumeMovement();

            MoveToCurrentWaypoint();
        }

        public override void Update()
        {
            // 1. Perception check
            Transform spottedTarget = Controller.DetectPotentialTarget();
            if (spottedTarget != null)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Detect);
                return;
            }

            // 2. Waypoint arrival check
            if (Controller.HasReachedDestination(0.6f))
            {
                Controller.AdvanceToNextWaypoint();
                // Pause briefly at the waypoint by entering Idle
                Controller.StateMachine.TransitionTo(AIStateType.Idle);
            }
        }

        public override void Exit()
        {
        }

        #endregion

        #region Helpers

        private void MoveToCurrentWaypoint()
        {
            Vector3 waypointPos = Controller.GetCurrentWaypointPosition();
            Controller.SetDestination(waypointPos);
        }

        #endregion
    }
}
