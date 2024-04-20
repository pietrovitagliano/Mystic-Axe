namespace MysticAxe
{
    public class PlayerStateManager : CharacterStateManager
    {
        private CharacterAliveState aliveState = new PlayerAliveState();
        
        protected override void EnterFirstState()
        {
            ChangeState(aliveState);
        }

        protected override void InitializeStateManager()
        {
            base.InitializeStateManager();

            aliveState.InitializeState(this);
        }
    }
}