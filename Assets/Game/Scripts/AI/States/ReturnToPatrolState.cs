using UnityEngine;
using UnityEngine.AI;

namespace RageHunters.AI.States
{
    /// <summary>
    /// AI returns to its nearest patrol waypoint after losing track of a target.
    /// Transitions to PatrolState once the waypoint is reached.
    /// </summary>
    public class ReturnToPatrolState : AIState
    {
        private bool _hasTarget;
        private float _arrivalThreshold = 1.5f;

        public ReturnToPatrolState(AIController controller) : base(controller) { }

        public override void Enter()
        {
            _hasTarget = false;

            NavMeshAgent agent = Controller.Agent;
            if (agent == null || !agent.isOnNavMesh) return;

            // Find the nearest patrol waypoint using the controller's helper
            Vector3 targetWaypoint = Controller.GetNearestWaypointPosition();
            _hasTarget = true;

            agent.isStopped = false;
            float speed = Controller.Archetype != null ? Controller.Archetype.MoveSpeed : 3.5f;
            agent.speed = speed;
            agent.SetDestination(targetWaypoint);

            Debug.Log($"[AI:{Controller.name}] ReturnToPatrol → heading to nearest waypoint");
        }

        public override void Update()
        {
            if (!_hasTarget)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Idle);
                return;
            }

            NavMeshAgent agent = Controller.Agent;
            if (agent == null || !agent.isOnNavMesh)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Idle);
                return;
            }

            // Check if we've arrived
            if (!agent.pathPending && agent.remainingDistance <= _arrivalThreshold)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Patrol);
                return;
            }

            // While returning, still check for new targets
            Transform detected = Controller.DetectPotentialTarget();
            if (detected != null)
            {
                Controller.StateMachine.TransitionTo(AIStateType.Detect);
            }
        }

        public override void Exit()
        {
            NavMeshAgent agent = Controller.Agent;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
        }
    }
}
