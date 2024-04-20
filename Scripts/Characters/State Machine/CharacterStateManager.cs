// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    public abstract class CharacterStateManager : MonoBehaviour
    {
        private BaseState currentState;
        private CharacterDeathState deathState = new CharacterDeathState();

        public BaseState CurrentState { get => currentState; }
        public CharacterDeathState DeathState { get => deathState; protected set => deathState = value; }

        private void Start()
        {
            // Initialize States
            InitializeStateManager();

            // Switch to the first state,
            // which depends on the concrete class
            // that extends this abstract class
            EnterFirstState();
        }

        private void Update()
        {
            currentState.UpdateState();
        }

        /// <summary>
        /// This method has been thought to be overriden,
        /// in order to initialize the states and other variables.
        /// It is called in the Start() method of this class.
        /// </summary>
        protected virtual void InitializeStateManager()
        {
            deathState.InitializeState(this);
        }

        protected abstract void EnterFirstState();

        public void ChangeState(BaseState state)
        {
            currentState = state;
            currentState.EnterState();
        }
    }
}