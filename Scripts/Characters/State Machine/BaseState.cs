// Author: Pietro Vitagliano

namespace MysticAxe
{
    public abstract class BaseState
    {
        public abstract void InitializeState(CharacterStateManager characterStateManager);
        public abstract void EnterState();
        public abstract void UpdateState();
    }
}