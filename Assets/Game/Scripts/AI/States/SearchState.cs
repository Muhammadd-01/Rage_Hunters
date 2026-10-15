using UnityEngine;

namespace RageHunters.AI.States
{
    /// <summary>
    /// AI State: Enemy travels to the target's last known position and conducts a local search
    /// (looking around). If no target is re-acquired before timeout, transitions to ReturnToPatrol.
    /// </summary>
    public class SearchState : AIState
    {
        #region Private Fields

        private float _searchTimer;
        private float _searchDuration;
        private bool _reachedLastKnownPosition;
        private float _lookAroundTimer;
        private float _lookDirectionSign = 1f;

        #endregion

        #region Constructor

        public SearchState(AIController controller) : base(controller)
        {
        }

        #endregion

        #region State Lifecycle

        public override void Enter()
        {
            float speed = Controller.Archetype != null ? Controller.Archetype.MoveSpeed : 3.5f;
            Controller.SetSpeed(speed);
            Controller.ResumeMovement();

            _searchDuration = Controller.Archetype != null ? Controller.Archetype.SearchDuration : 5.0f;
            _searchTimer = 0f;
            _lookAroundTimer = 0f;
            _reachedLastKnownPosition = false;

            // Travel to target's last observed position
            Controller.SetDestination(Controller.LastKnownTargetPosition);
        }

        public override void Update()
        {
            // 1. Perception check: Target re-acquired
            Transform spottedTarget = Controller.DetectPotentialTarget();
            if (spottedTarget != null)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Detect);
                return;
            }

            // 2. Check if arrived at last known spot
            if (!_reachedLastKnownPosition)
            {
                if (Controller.HasReachedDestination(1.0f))
                {
                    _reachedLastKnownPosition = true;
                    Controller.StopMovement();
                }
            }
            else
            {
                // 3. Look around behavior while stopped
                _lookAroundTimer += Time.deltaTime;
                if (_lookAroundTimer >= 1.5f)
                {
                    _lookAroundTimer = 0f;
                    _lookDirectionSign *= -1f;
                }

                // Rotate left and right smoothly
                Vector3 lookTarget = Controller.transform.position +
                    (Controller.transform.forward + Controller.transform.right * _lookDirectionSign * 0.75f).normalized * 5f;
                Controller.RotateTowards(lookTarget, 3f);
            }

            // 4. Timeout check
            _searchTimer += Time.deltaTime;
            if (_searchTimer >= _searchDuration)
            {
                Controller.ClearTarget();
                Controller.StateMachine.TransitionTo(AIStateType.ReturnToPatrol);
            }
        }

        public override void Exit()
        {
            Controller.ResumeMovement();
        }

        #endregion
    }
}
