using UnityEngine;

namespace RageHunters.AI.States
{
    /// <summary>
    /// AI State: Enemy remains stationary, observing surroundings for a configured duration
    /// before resuming patrol. Transitions to Detect if a player is spotted or heard.
    /// </summary>
    public class IdleState : AIState
    {
        #region Private Fields

        private float _idleTimer;
        private float _targetIdleDuration;

        #endregion

        #region Constructor

        public IdleState(AIController controller) : base(controller)
        {
        }

        #endregion

        #region State Lifecycle

        public override void Enter()
        {
            Controller.StopMovement();

            _targetIdleDuration = Controller.Archetype != null
                ? Controller.Archetype.PatrolWaitDuration
                : 2.0f;

            _idleTimer = 0f;
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

            // 2. Idle timer progress
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _targetIdleDuration)
            {
                if (Controller.PatrolWaypoints != null && Controller.PatrolWaypoints.Count > 0)
                {
                    Controller.StateMachine.TransitionTo(AIStateType.Patrol);
                }
                else
                {
                    // If no waypoints, reset timer to keep idling
                    _idleTimer = 0f;
                }
            }
        }

        public override void Exit()
        {
            Controller.ResumeMovement();
        }

        #endregion
    }
}
