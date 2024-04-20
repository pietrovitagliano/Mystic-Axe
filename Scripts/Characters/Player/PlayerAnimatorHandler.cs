// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    public class PlayerAnimatorHandler : CharacterAnimatorHandler
    {
        #region Animator Parameters
        private int isGroundedHash;
        private int jumpStartedHash;
        private int equipUnequipAxeTriggerHash;
        private int isAxeInHandHash;
        private int isAimingHash;
        private int throwAxeHash;
        private int isRecallingAxeHash;
        private int catchAxeHash;
        private int isBlockingHash;
        private int isTargetingHash;
        private int fallingHeightHash;
        private int dodgeCounterHash;
        private int isDodgingHash;
        private int isRotatingHash;
        private int xRotationAngle;
        private int lightAttackHash;
        private int heavyAttackHash;
        private int isCastingSpecialAttackHash;
        private int isSpecialAttackEndingHash;
        private int skillIndexHash;
        private int lightComboCounterHash;
        private int heavyComboCounterHash;
        private int canAttackBeInterruptedByMovementHash;
        private int drinkPotionHash;
        private int isUsingConsumableHash;
        #endregion

        #region Public Modifiers Parameters
        public int IsGroundedHash { get => isGroundedHash; }
        public int JumpStartedHash { get => jumpStartedHash; }
        public int EquipUnequipAxeTriggerHash { get => equipUnequipAxeTriggerHash; }
        public int IsAxeInHandHash { get => isAxeInHandHash; }
        public int IsAimingHash { get => isAimingHash; }
        public int ThrowAxeHash { get => throwAxeHash; }
        public int IsRecallingAxeHash { get => isRecallingAxeHash; }
        public int CatchAxeHash { get => catchAxeHash; }
        public int IsBlockingHash { get => isBlockingHash; }
        public int IsTargetingHash { get => isTargetingHash; }
        public int FallingHeightHash { get => fallingHeightHash; }
        public int DodgeCounterHash { get => dodgeCounterHash; }
        public int IsDodgingHash { get => isDodgingHash; }
        public int IsRotatingHash { get => isRotatingHash; }
        public int XRotationAngleHash { get => xRotationAngle; }
        public int LightAttackHash { get => lightAttackHash; }
        public int HeavyAttackHash { get => heavyAttackHash; }
        public int SkillIndexHash { get => skillIndexHash; }
        public int IsCastingSpecialAttackHash { get => isCastingSpecialAttackHash; }
        public int LightComboCounterHash { get => lightComboCounterHash; }
        public int HeavyComboCounterHash { get => heavyComboCounterHash; }
        public int CanAttackBeInterruptedByMovementHash { get => canAttackBeInterruptedByMovementHash; }
        public int IsSpecialAttackEndingHash { get => isSpecialAttackEndingHash; }
        public int DrinkPotionHash { get => drinkPotionHash; }
        public int IsUsingConsumableHash { get => isUsingConsumableHash; }
        #endregion


        protected override void InizializeAnimatorVariables()
        {
            isGroundedHash = Animator.StringToHash(Utils.isGroundedParam);
            jumpStartedHash = Animator.StringToHash(Utils.JUMP_STARTED_PARAM);
            equipUnequipAxeTriggerHash = Animator.StringToHash(Utils.equipUnequipParam);
            isAxeInHandHash = Animator.StringToHash(Utils.isAxeInHandParam);
            isAimingHash = Animator.StringToHash(Utils.isAimingParam);
            throwAxeHash = Animator.StringToHash(Utils.isThrowingAxeParam);
            isRecallingAxeHash = Animator.StringToHash(Utils.isRecallingAxeParam);
            catchAxeHash = Animator.StringToHash(Utils.catchAxeParam);
            isBlockingHash = Animator.StringToHash(Utils.isBlockingParam);
            isTargetingHash = Animator.StringToHash(Utils.IS_TARGETING_PARAM);
            fallingHeightHash = Animator.StringToHash(Utils.FALLING_HEIGHT_PARAM);
            dodgeCounterHash = Animator.StringToHash(Utils.dodgeCounterParam);
            isDodgingHash = Animator.StringToHash(Utils.isDodgingParam);
            lightAttackHash = Animator.StringToHash(Utils.lightAttackParam);
            heavyAttackHash = Animator.StringToHash(Utils.heavyAttackParam);
            isCastingSpecialAttackHash = Animator.StringToHash(Utils.isCastingSpecialAttackParam);
            skillIndexHash = Animator.StringToHash(Utils.skillIndexParam);
            isSpecialAttackEndingHash = Animator.StringToHash(Utils.specialAttackEndParam);
            lightComboCounterHash = Animator.StringToHash(Utils.lightComboCounterParam);
            heavyComboCounterHash = Animator.StringToHash(Utils.heavyComboCounterParam);
            canAttackBeInterruptedByMovementHash = Animator.StringToHash(Utils.canAttackBeInterruptedByMovementParam);
            xRotationAngle = Animator.StringToHash(Utils.xRotationAngleParam);
            isRotatingHash = Animator.StringToHash(Utils.isRotatingParam);
            drinkPotionHash = Animator.StringToHash(Utils.drinkPotionParam);
            isUsingConsumableHash = Animator.StringToHash(Utils.isUsingConsumableParam);
        }
    }
}
