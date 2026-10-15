using UnityEngine;

namespace RageHunters.AI
{
    /// <summary>
    /// Abstract base class for all AI behaviors executed within an AIStateMachine.
    /// Provides lifecycle hooks and a direct reference to the owning AIController.
    /// </summary>
    public abstract class AIState
    {
        #region Protected Fields

        /// <summary>
        /// Reference to the owning AI controller instance.
        /// </summary>
        protected readonly AIController Controller;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new AI state bound to the specified controller.
        /// </summary>
        /// <param name="controller">The owning AI controller.</param>
        protected AIState(AIController controller)
        {
            Controller = controller;
        }

        #endregion

        #region State Lifecycle Methods

        /// <summary>
        /// Called once when entering this state.
        /// </summary>
        public virtual void Enter()
        {
        }

        /// <summary>
        /// Called every frame while this state is active.
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// Called every physics step while this state is active.
        /// </summary>
        public virtual void FixedUpdate()
        {
        }

        /// <summary>
        /// Called once when transitioning out of this state.
        /// </summary>
        public virtual void Exit()
        {
        }

        #endregion
    }
}
