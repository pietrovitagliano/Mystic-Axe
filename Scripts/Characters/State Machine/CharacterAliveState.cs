// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    public abstract class CharacterAliveState : BaseState
    {
        private Transform character;
        private CharacterStatusHandler characterStatusHandler;

        public Transform Character { get => character; }

        public override void InitializeState(CharacterStateManager characterStateManager)
        {
            character = characterStateManager.transform;
            characterStatusHandler = character.GetComponent<CharacterStatusHandler>();
        }

        public override void EnterState()
        {
            if (characterStatusHandler.IsDead)
            {
                GoToDeathState();
            }
            else
            {
                AliveEnterState();
            }
        }

        public override void UpdateState()
        {
            if (characterStatusHandler.IsDead)
            {
                GoToDeathState();
            }
            else
            {
                AliveUpdateState();
            }
        }

        protected abstract void AliveEnterState();

        protected abstract void AliveUpdateState();

        private void GoToDeathState()
        {
            CharacterStateManager characterStateManager = character.GetComponent<CharacterStateManager>();
            characterStateManager.ChangeState(characterStateManager.DeathState);
        }
    }
}
