// Author: Pietro Vitagliano

using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace MysticAxe
{
    public class HeadLookAtTarget : AbstractRigWeightHandler
    {
        private MultiAimConstraint rigAimConstraint;
        private PlayerStatusHandler playerStatusHandler;
        private Transform canvasCrosshair;
        private Transform canvasLockOnTarget;

        protected override void Start()
        {
            base.Start();
            
            playerStatusHandler = GetComponent<PlayerStatusHandler>();
            canvasCrosshair = GameObject.FindGameObjectWithTag(Utils.CANVAS_CROSSHAIR_TAG).transform;
            canvasLockOnTarget = GameObject.FindGameObjectWithTag(Utils.CANVAS_LOCK_ON_TARGET_TAG).transform;
            rigAimConstraint = Rig.GetComponent<MultiAimConstraint>();
        }

        protected override void Update()
        {
            if (playerStatusHandler.IsAiming || playerStatusHandler.IsTargetingEnemy)
            {
                RigOn();
            }
            else
            {
                RigOff();
            }

            UpdateRigWeight();
            SwitchLookingAtTarget();
        }

        private void SwitchLookingAtTarget()
        {
            WeightedTransformArray sources = new WeightedTransformArray();
            if (playerStatusHandler.IsAiming)
            {
                sources.Add(new WeightedTransform(canvasCrosshair, 1f));
            }
            else if (playerStatusHandler.IsTargetingEnemy)
            {
                sources.Add(new WeightedTransform(canvasLockOnTarget, 1f));
            }
            else
            {
                sources.Add(new WeightedTransform(canvasCrosshair, 1f));
            }

            rigAimConstraint.data.sourceObjects = sources;
        }
    }
}
