// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    public class EnemyAnimatorHandler : CharacterAnimatorHandler
    {
        private int comboChooserHash;
        private int continueToAttackHash;
        
        public int ComboChooserHash { get => comboChooserHash; }
        public int ContinueToAttackHash { get => continueToAttackHash; }

        protected override void InizializeAnimatorVariables()
        {
            comboChooserHash = Animator.StringToHash(Utils.comboChooserParam);
            continueToAttackHash = Animator.StringToHash(Utils.continueToAttackParam);
        }
    }
}