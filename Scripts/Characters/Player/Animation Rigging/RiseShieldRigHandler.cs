// Author: Pietro Vitagliano

namespace MysticAxe
{
    public class RiseShieldRigHandler : AbstractRigWeightHandler
    {
        private PlayerStatusHandler playerStatusHandler;

        protected override void Start()
        {
            base.Start();
            playerStatusHandler = GetComponent<PlayerStatusHandler>();
        }

        protected override void Update()
        {
            if (playerStatusHandler.IsBlocking)
            {
                RigOn();
            }
            else
            {
                RigOff();
            }

            UpdateRigWeight();
        }
    }
}
