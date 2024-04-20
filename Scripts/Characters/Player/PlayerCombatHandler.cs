// Author: Pietro Vitagliano

using DG.Tweening;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    [RequireComponent(typeof(PlayerAnimatorHandler))]
    [RequireComponent(typeof(PlayerStatusHandler))]
    public class PlayerCombatHandler : MonoBehaviour
    {
        private Transform axe;
        private AxeStatusHandler axeStatusHandler;
        private AxeThrowHandler axeThrowHandler;
        private AxeReturnHandler axeReturnHandler;
        private AxeAnimatorHandler axeAnimatorHandler;
        private SkillSetHandler skillSetHandler;

        private InputHandler inputHandler;
        private PlayerStatusHandler playerStatusHandler;
        private PlayerAnimatorHandler playerAnimatorHandler;
        
        private RectTransform canvasLockOnTarget;

        [Header("Attack Settings")]
        [SerializeField, Range(1, 10)] private int lightComboMaxLength = 6;
        [SerializeField, Range(1, 10)] private int heavyComboMaxLength = 3;
        [SerializeField, Range(0.1f, 3f)] private float lightAttackDamageMultiplier = 1;
        [SerializeField, Range(0.1f, 3f)] private float heavyAttackDamageMultiplier = 1.35f;
        [SerializeField, Min(0.1f)] private float resetComboAfterDodgeTime = 1f;
        [SerializeField, Min(0.1f)] private float findTargetMaxDistance = 2.5f;
        [SerializeField, Range(0.1f, 1)] private float normalAttackRotationDuration = 0.2f;

        [Header("Skills Settings")]
        [SerializeField, Min(0.1f)] private float specialAttacksRotationDuration = 0.1f;

        private int attackLayerIndex;

        private int lightComboCounter = 0;
        private int heavyComboCounter = 0;
        private bool canAttackBeInterruptedByNextAttack = false;
        private bool canAttackBeInterruptedByDodge = true;
        private bool canAttackBeInterruptedByMovement = false;
        private bool baseAttackCanDamage = false;
        private Coroutine resetCountersAfterDodgeCoroutine = null;
        
        public bool CanAttackBeInterruptedByNextAttack { get => canAttackBeInterruptedByNextAttack; }
        public bool CanAttackBeInterruptedByDodge { get => canAttackBeInterruptedByDodge; }
        public bool CanAttackBeInterruptedByMovement { get => canAttackBeInterruptedByMovement; }
        public bool BaseAttackCanDamage { get => baseAttackCanDamage; }

        private void Start()
        {
            canvasLockOnTarget = FindObjectOfType<LockOnHandler>().CanvasLockOnTarget;
            axe = GameObject.FindGameObjectWithTag(Utils.PLAYER_AXE_TAG).transform;
            axeStatusHandler = axe.GetComponent<AxeStatusHandler>();
            axeThrowHandler = axe.GetComponent<AxeThrowHandler>();
            axeReturnHandler = axe.GetComponent<AxeReturnHandler>();
            skillSetHandler = axe.GetComponent<AxeSkillSetHandler>();
            axeAnimatorHandler = axe.GetComponent<AxeAnimatorHandler>();
            
            inputHandler = InputHandler.Instance;
            playerStatusHandler = GetComponent<PlayerStatusHandler>();
            playerAnimatorHandler = GetComponent<PlayerAnimatorHandler>();
            
            attackLayerIndex = playerAnimatorHandler.Animator.GetLayerIndex(Utils.ATTACK_LAYER_NAME);
        }

        private void Update()
        {
            HandleAttack();
            HandleComboCounterReset();
            HandleLightAttackWhooshesWhenDodging();
            HandleSkills();
        }

        private void HandleComboCounterReset()
        {
            bool stopAttackingCondition = playerAnimatorHandler.Animator.GetNextAnimatorStateInfo(attackLayerIndex).IsName(Utils.emptyAnimationStateName);

            if (lightComboCounter > 0 || heavyComboCounter > 0)
            {
                if (!playerStatusHandler.IsDodging)
                {
                    // if player stops to attack, reset both counters,
                    // because both combos will have to restart from the beginning
                    if (stopAttackingCondition)
                    {
                        lightComboCounter = 0;
                        heavyComboCounter = 0;
                    }
                }
                // if player dodges and coroutine is not running,
                // it will be started
                else if (stopAttackingCondition && resetCountersAfterDodgeCoroutine == null)
                {
                    resetCountersAfterDodgeCoroutine = StartCoroutine(ResetCountersAfterDodgeCoroutine());
                }
            }

            canAttackBeInterruptedByNextAttack = stopAttackingCondition ? false : canAttackBeInterruptedByNextAttack;
            canAttackBeInterruptedByDodge = stopAttackingCondition ? true : canAttackBeInterruptedByDodge;
            canAttackBeInterruptedByMovement = stopAttackingCondition ? false : canAttackBeInterruptedByMovement;
            baseAttackCanDamage = stopAttackingCondition ? false : baseAttackCanDamage;
            playerStatusHandler.IsAttacking = stopAttackingCondition ? false : playerStatusHandler.IsAttacking;
            playerStatusHandler.IsCastingSkill = stopAttackingCondition ? false : playerStatusHandler.IsCastingSkill;

            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.CanAttackBeInterruptedByMovementHash, canAttackBeInterruptedByMovement);
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsAttackingHash, playerStatusHandler.IsAttacking);
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsCastingSpecialAttackHash, playerStatusHandler.IsCastingSkill);
        }

        private IEnumerator ResetCountersAfterDodgeCoroutine()
        {            
            yield return new WaitForSeconds(resetComboAfterDodgeTime);
            
            bool stopAttackingCondition = playerAnimatorHandler.Animator.GetCurrentAnimatorStateInfo(attackLayerIndex).IsName(Utils.emptyAnimationStateName);

            if (stopAttackingCondition)
            {
                lightComboCounter = lightComboCounter > 0 ? 0 : lightComboCounter;
                heavyComboCounter = heavyComboCounter > 0 ? 0 : heavyComboCounter;
            }
        }

        private void HandleAttack()
        {
            if (playerStatusHandler.WantsToAttack)
            {
                Targetable target = !playerStatusHandler.IsTargetingEnemy ? FindNearestEnemy() : canvasLockOnTarget.parent.GetComponent<Targetable>();

                Attack(target);

                inputHandler.LightAttackPressed = false;
                inputHandler.HeavyAttackPressed = false;
                inputHandler.SkillIndexInput = 0;
            }

            playerAnimatorHandler.Animator.SetInteger(playerAnimatorHandler.LightComboCounterHash, lightComboCounter);
            playerAnimatorHandler.Animator.SetInteger(playerAnimatorHandler.HeavyComboCounterHash, heavyComboCounter);
        }

        private void Attack(Targetable target)
        {
            if (inputHandler.LightAttackPressed || inputHandler.HeavyAttackPressed)
            {
                RotateTowardsTarget(target, normalAttackRotationDuration);

                if (inputHandler.LightAttackPressed && lightComboCounter < lightComboMaxLength)
                {
                    playerStatusHandler.IsDoingHeavyAttack = false;
                    playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.LightAttackHash, true);
                    lightComboCounter++;
                }
                else if (inputHandler.HeavyAttackPressed && heavyComboCounter < heavyComboMaxLength)
                {
                    playerStatusHandler.IsDoingHeavyAttack = true;
                    playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.HeavyAttackHash, true);
                    heavyComboCounter++;
                }

                canAttackBeInterruptedByNextAttack = false;
                canAttackBeInterruptedByDodge = true;
                canAttackBeInterruptedByMovement = false;
                baseAttackCanDamage = false;

                playerStatusHandler.IsAttacking = true;

                // If the corutine is already running
                // and player attacks, it will be stopped
                if (resetCountersAfterDodgeCoroutine != null)
                {
                    StopCoroutine(resetCountersAfterDodgeCoroutine);
                    resetCountersAfterDodgeCoroutine = null;
                }
            }
        }

        private void RotateTowardsTarget(Targetable target, float rotationDuration)
        {            
            Vector3 attackDirection;
            if (target == null)
            {
                Vector2 input = inputHandler.MovementInput;
                Camera camera = Camera.main;
                attackDirection = input.magnitude > 0 ? camera.transform.forward * input.y + camera.transform.right * input.x : camera.transform.forward;
            }
            else
            {
                attackDirection = target.transform.position - transform.position;
            }

            Vector3 projectedAttackDirection = Vector3.ProjectOnPlane(attackDirection, Vector3.up).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(projectedAttackDirection);

            transform.DORotateQuaternion(targetRotation, rotationDuration)
                        .OnComplete(() =>
                        {
                            if (target != null)
                            {
                                transform.LookAt(target.transform);
                            }
                        });
        }
        
        private Targetable FindNearestEnemy()
        {
            Vector2 playerInput = inputHandler.MovementInput;

            Vector3 lookAtDirection;
            if (playerInput.magnitude > 0)
            {
                Camera camera = Camera.main;
                lookAtDirection = playerInput.y * Vector3.forward + playerInput.x * Vector3.right;
                lookAtDirection = camera.transform.TransformDirection(lookAtDirection);
            }
            else
            {
                lookAtDirection = transform.forward;
            }

            lookAtDirection = Vector3.ProjectOnPlane(lookAtDirection, Vector3.up);
            lookAtDirection = lookAtDirection.normalized;

            int enemyLayer = LayerMask.GetMask(Utils.ENEMY_LAYER_NAME);
            float sphereRadius = 2f;
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, sphereRadius, lookAtDirection, findTargetMaxDistance, enemyLayer, QueryTriggerInteraction.Ignore);
            
            return hits.Where(hit => hit.collider.gameObject.GetComponent<Targetable>() != null)
                        .Select(hit => hit.collider.gameObject.GetComponent<Targetable>())
                        .OrderBy(target => Vector3.Distance(transform.position, target.transform.position))
                        .FirstOrDefault();
        }

        private void HandleSkills()
        {
            int skillLayerIndex = playerAnimatorHandler.Animator.GetLayerIndex(Utils.SKILLS_LAYER_NAME);
            bool isSkillAnimationEnding = playerAnimatorHandler.Animator.GetNextAnimatorStateInfo(skillLayerIndex).IsName(Utils.emptyAnimationStateName);
            
            if (playerStatusHandler.WantsToCastSkill && !isSkillAnimationEnding)
            {
                int skillIndex = inputHandler.SkillIndexInput;

                if (skillSetHandler.IsIndexValid(skillIndex) && skillSetHandler.IsManaEnoughForSkill(skillIndex))
                {
                    ResetVariablesForSkillExecution();

                    RotateTowardsTarget(null, specialAttacksRotationDuration);

                    skillSetHandler.ConsumeManaForSkill(skillIndex);

                    playerAnimatorHandler.Animator.SetInteger(playerAnimatorHandler.SkillIndexHash, skillIndex);
                    playerStatusHandler.IsCastingSkill = true;
                }

                inputHandler.FirstSkillPressed = false;
                inputHandler.SecondSkillPressed = false;
                inputHandler.SkillIndexInput = 0;
            }
        }

        private void ResetVariablesForSkillExecution()
        {
            canAttackBeInterruptedByNextAttack = false;
            canAttackBeInterruptedByDodge = false;
            canAttackBeInterruptedByMovement = false;
            baseAttackCanDamage = false;

            lightComboCounter = 0;
            heavyComboCounter = 0;
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.LightAttackHash, false);
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.HeavyAttackHash, false);
        }

        private void ResetCountersWhenComboEnded()
        {
            // If one of the combos ends,
            // reset both the combo counters, because any combos
            // will have to restart from the beginning
            if (lightComboCounter >= lightComboMaxLength || heavyComboCounter >= heavyComboMaxLength)
            {
                lightComboCounter = 0;
                heavyComboCounter = 0;
            }

            playerAnimatorHandler.Animator.SetInteger(playerAnimatorHandler.LightComboCounterHash, lightComboCounter);
            playerAnimatorHandler.Animator.SetInteger(playerAnimatorHandler.HeavyComboCounterHash, heavyComboCounter);
        }

        private void HandleLightAttackWhooshesWhenDodging()
        {
            if (playerStatusHandler.WantsToDodge || playerStatusHandler.IsDodging)
            {
                axeStatusHandler.StopMeleeLightWhoosh();
                axeStatusHandler.StopMeleeHeavyWhoosh();
            }
        }

        public void StartSpecialAttackEndAnimation()
        {
            playerAnimatorHandler.Animator.SetTrigger(playerAnimatorHandler.IsSpecialAttackEndingHash);
        }

        #region Animation Events
        private void OnCastSkill()
        {
            int skillIndex = playerAnimatorHandler.Animator.GetInteger(playerAnimatorHandler.SkillIndexHash);

            skillSetHandler.CastSkill(skillIndex);
            playerAnimatorHandler.Animator.SetInteger(playerAnimatorHandler.SkillIndexHash, 0);
        }

        private void OnSpecialAttackEnded()
        {
            playerStatusHandler.IsCastingSkill = false;
        }

        private void OnBaseAttackCanDamage()
        {
            baseAttackCanDamage = true;
        }
        
        private void OnBaseAttackCannotDamage()
        {
            baseAttackCanDamage = false;
        }

        private void OnAttackCanBeInterruptedByNextAttack()
        {
            canAttackBeInterruptedByNextAttack = true;
        }

        private void OnAttackCanBeInterruptedByMovement()
        {
            // The flag can be modified only if no transition are going on between animator states
            if (!playerAnimatorHandler.Animator.IsInTransition(attackLayerIndex))
            {
                canAttackBeInterruptedByMovement = true;
            }
        }

        private void OnAttackCanBeInterruptedByDodge()
        {
            canAttackBeInterruptedByDodge = true;
        }

        private void OnAttackCannotBeInterruptedByDodge()
        {
            canAttackBeInterruptedByDodge = false;
        }

        private void OnLightAttackPerformed()
        {
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.LightAttackHash, false);
            ResetCountersWhenComboEnded();
        }

        private void OnHeavyAttackPerformed()
        {
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.HeavyAttackHash, false);
            ResetCountersWhenComboEnded();
        }

        private void OnThrowAxe()
        {
            axeThrowHandler.ThrowAxe();
        }

        private void OnAnimateAxeDuringSpecialAttack(string triggerParam)
        {
            axeAnimatorHandler.Animator.enabled = true;
            
            int triggerParamHash = Animator.StringToHash(triggerParam);
            axeAnimatorHandler.Animator.SetTrigger(triggerParamHash);
        }

        private void OnPlayThrowSoundWithFadeIn(float fadeTime)
        {
            axeStatusHandler.OnPlayThrowSoundWithFadeIn(fadeTime);
        }

        private void OnRecallAxe()
        {
            axeReturnHandler.RecallAxe();
        }

        private void OnCatchAxe()
        {
            axeReturnHandler.CatchAxe();
        }

        private void OnPlayAxeMeleeLightWhoosh()
        {
            if (playerStatusHandler.IsAttacking)
            {
                axeStatusHandler.PlayMeleeLightWhoosh();
            }
        }
        
        private void OnPlayAxeMeleeHeavyWhoosh()
        {
            if (playerStatusHandler.IsAttacking)
            {
                axeStatusHandler.PlayMeleeHeavyWhoosh();
            }
        }

        private void OnPlayAxeThunderstormGroundHit()
        {
            axeStatusHandler.PlayThunderstormGroundHit();
        }
        #endregion
    }
}
