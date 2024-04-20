// Author: Pietro Vitagliano

using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace MysticAxe
{
    public abstract class AbstractRigWeightHandler : MonoBehaviour
    {
        [Header("Animation Rigging Variables")]
        [SerializeField] private Rig rig;
        [SerializeField, Min(0.1f)] private float weightRigChangingTime = 0.25f;
        private float rigWeightTarget = 0f;

        public Rig Rig { get => rig; }

        protected virtual void Start()
        {
            rig.weight = 0;
        }

        protected abstract void Update();

        protected void UpdateRigWeight()
        {            
            rig.weight = Utils.FloatInterpolation(rig.weight, rigWeightTarget, Time.deltaTime / weightRigChangingTime);
        }

        protected void RigOn()
        {
            rigWeightTarget = 1;
        }

        protected void RigOff()
        {
            rigWeightTarget = 0;
        }
    }
}