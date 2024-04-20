// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CharacterStatusHandler))]
    public abstract class CharacterAnimatorHandler : MonoBehaviour
    {
        private Animator animator;
        private int speedHash;
        private int inCombatHash;
        private int movementXHash;
        private int movementYHash;
        private int isAttackingHash;
        private int hitLightlyHash;
        private int hitHeavyHash;
        private int hitDirectionXHash;
        private int hitDirectionYHash;
        private int spawnHash;
        private int isDeadHash;

        public Animator Animator { get => animator; }
        public int SpeedHash { get => speedHash; }
        public int InCombatHash { get => inCombatHash; }
        public int MovementXHash { get => movementXHash; }
        public int MovementYHash { get => movementYHash; }
        public int IsAttackingHash { get => isAttackingHash; }
        public int HitLightlyHash { get => hitLightlyHash; }
        public int HitHeavyHash { get => hitHeavyHash; }
        public int HitDirectionXHash { get => hitDirectionXHash; }
        public int HitDirectionYHash { get => hitDirectionYHash; }
        public int SpawnHash { get => spawnHash; }
        public int IsDeadHash { get => isDeadHash; }

        private void Awake()
        {
            // Get the animator and disable it,
            // since it has to be enabled only after that
            // the character status has been initialized
            animator = GetComponent<Animator>();
            animator.enabled = false;

            CharacterStatusHandler characterStatusHandler = GetComponent<CharacterStatusHandler>();
            characterStatusHandler.OnStatusInitialized.AddListener(() => animator.enabled = true);

            speedHash = Animator.StringToHash(Utils.speedParam);
            inCombatHash = Animator.StringToHash(Utils.inCombatParam);
            movementXHash = Animator.StringToHash(Utils.movementXParam);
            movementYHash = Animator.StringToHash(Utils.movementYParam);
            isAttackingHash = Animator.StringToHash(Utils.isAttackingParam);
            hitLightlyHash = Animator.StringToHash(Utils.hitLightlyParam);
            hitHeavyHash = Animator.StringToHash(Utils.hitHeavyParam);
            hitDirectionXHash = Animator.StringToHash(Utils.hitDirectionXParam);
            hitDirectionYHash = Animator.StringToHash(Utils.hitDirectionYParam);
            spawnHash = Animator.StringToHash(Utils.spawnParam);
            isDeadHash = Animator.StringToHash(Utils.isDeadParam);
            
            InizializeAnimatorVariables();
        }

        protected abstract void InizializeAnimatorVariables();
    }
}