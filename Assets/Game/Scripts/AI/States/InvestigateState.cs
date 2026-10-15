using UnityEngine;

namespace RageHunters.AI.States
{
    /// <summary>
    /// AI investigates a suspicious sound or partially-detected stimulus.
    /// Moves to the last known position and looks around before returning to patrol.
    /// </summary>
    public class InvestigateState : AIState
    {
        private float _investigateTimer;
        private float _investigateDuration = 5f;
        private float _lookAroundTimer;
        private float _lookAroundInterval = 1.5f;
        private bool _hasReachedPoint;

        public InvestigateState(AIController controller) : base(controller) { }

        public override void Enter()
        {
            _investigateTimer = 0f;
            _lookAroundTimer = 0f;
            _hasReachedPoint = false;

            // Move to the last known target position
            Vector3 investigatePos = Controller.LastKnownTargetPosition;
            Controller.SetDestination(investigatePos);

            float speed = Controller.Archetype != null ? Controller.Archetype.MoveSpeed * 0.8f : 3f;
            Controller.SetSpeed(speed);

            Debug.Log($"[AI:{Controller.name}] Investigate → moving to investigate position");
        }

        public override void Update()
        {
            _investigateTimer += Time.deltaTime;

            // Check for full target detection during investigation
            Transform detected = Controller.DetectPotentialTarget();
            if (detected != null)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Chase);
                return;
            }

            // Move toward investigation point
            if (!_hasReachedPoint && Controller.HasReachedDestination(1.5f))
            {
                _hasReachedPoint = true;
                Controller.StopMovement();
            }

            // Look around at the investigation point
            if (_hasReachedPoint)
            {
                _lookAroundTimer += Time.deltaTime;
                if (_lookAroundTimer >= _lookAroundInterval)
                {
                    _lookAroundTimer = 0f;
                    // Rotate to a random direction
                    float randomAngle = Random.Range(-90f, 90f);
                    Vector3 lookDir = Quaternion.Euler(0f, randomAngle, 0f) * Controller.transform.forward;
                    Vector3 lookTarget = Controller.transform.position + lookDir * 5f;
                    Controller.RotateTowards(lookTarget, 5f);
                }
            }

            // Timeout — nothing found
            if (_investigateTimer >= _investigateDuration)
            {
                Controller.ClearTarget();
                Controller.StateMachine.TransitionTo(AIStateType.ReturnToPatrol);
            }
        }

        public override void Exit()
        {
            Controller.StopMovement();
        }
    }
}
