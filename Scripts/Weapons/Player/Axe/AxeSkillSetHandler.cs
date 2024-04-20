// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    [RequireComponent(typeof(AxeAnimatorHandler))]
    public class AxeSkillSetHandler : SkillSetHandler
    {
        private AxeAnimatorHandler axeAnimatorHandler;


        protected override void Start()
        {
            axeAnimatorHandler = GetComponent<AxeAnimatorHandler>();

            base.Start();
        }
        
        public override void CastSkill(int index)
        {
            SkillHandler skillHandler = SkillHandlerDict[index];

            if (skillHandler != null)
            {
                GameObject instantiatedGameObject = Instantiate(skillHandler.gameObject);
                skillHandler = instantiatedGameObject.GetComponent<SkillHandler>();

                // Set the weapon which has casted the skill.
                skillHandler.Weapon = gameObject;
            }
        }

        private void OnAxeAnimationEnded()
        {
            axeAnimatorHandler.Animator.enabled = false;
        }
    }
}
