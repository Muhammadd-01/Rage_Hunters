using UnityEngine;

namespace RageHunters.AI.States
{
    /// <summary>
    /// AI State: Enemy reacts to a detected target, rotates to face them,
    /// pauses for a reaction/recognition delay, and initiates Chase.
    /// </summary>
    public class DetectState : AIState
    {
        #region Private Fields

        private float _detectTimer;
        private float _delayDuration;

        #endregion

        #region Constructor

        public DetectState(AIController controller) : base(controller)
        {
        }

        #endregion

        #region State Lifecycle

        public override void Enter()
        {
            Controller.StopMovement();

            _delayDuration = Controller.Archetype != null
                ? Controller.Archetype.DetectionDelay
                : 0.5f;

            _detectTimer = 0f;
        }

        public override void Update()
        {
            if (!Controller.HasTarget)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Search);
                return;
            }

            // Continuously face target during detection buildup
            Controller.RotateTowards(Controller.Target.position, 12f);

            // Verify target is still visible
            bool canSee = Controller.CanSeeTarget(Controller.Target);
            if (!canSee)
            {
                // Lost line of sight before fully detecting
                Controller.StateMachine.TransitionTo(AIStateType.Search);
                return;
            }

            _detectTimer += Time.deltaTime;
            if (_detectTimer >= _delayDuration)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Chase);
            }
        }

        public override void Exit()
        {
            Controller.ResumeMovement();
        }

        #endregion
    }
}
