// Author: Pietro Vitagliano

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MovementType = MysticAxe.PlayerLocomotionHandler.MovementType;

namespace MysticAxe
{
    [RequireComponent(typeof(PlayerAnimatorHandler))]
    [RequireComponent(typeof(PlayerStatusHandler))]
    [RequireComponent(typeof(PlayerLocomotionHandler))]
    [RequireComponent(typeof(CharacterController))] 
    public class PlayerDodgeHandler : MonoBehaviour
    {
        private PlayerStatusHandler playerStatusHandler;
        private PlayerLocomotionHandler playerLocomotionHandler;
        private InputHandler inputHandler;
        private CharacterController characterController;
        private PlayerAnimatorHandler playerAnimatorHandler;

        [Header("Dodge VariSettings")]
        [SerializeField, Range(0.1f, 1.5f)] private float nextStepTimeAfterStep = 1f;
        [SerializeField, Range(0.1f, 1.5f)] private float nextStepTimeAfterRoll = 0.7f;
        [SerializeField] private AnimationCurve[] dodgeSpeedCurves = { new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.25f, 9f), new Keyframe(0.65f, 0f)),
                                                                    new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.25f, 15f), new Keyframe(0.475f, 5.7f), new Keyframe(0.73f, 0f)) };

        [Header("Dodge Final Speed Settings")]
        [SerializeField, Range(0.1f, 2)] private float walkFinalSpeedScaler = 0.8f;
        [SerializeField, Range(0.1f, 2)] private float baseRunFinalSpeedScaler = 0.4f;
        [SerializeField, Range(0.1f, 2)] private float fastRunFinalSpeedScaler = 0.75f;
        [SerializeField, Range(0.1f, 2)] private float rollFinalSpeedScaler = 1.3f;

        private float timeElapsedAfterStep, timeElapsedAfterRoll;
        private const int MAX_DODGE_PERFORMABLE = 2;
        private int dodgePerformedCounter;
        private Dictionary<MovementType, float> dodgeFinalSpeedDict;
        private bool canStepBeInterrupted;
        private bool wantsToInterruptStep;

        public bool WantsToInterruptStep { get => wantsToInterruptStep; }

        private void Start()
        {
            playerStatusHandler = GetComponent<PlayerStatusHandler>();
            playerLocomotionHandler = GetComponent<PlayerLocomotionHandler>();
            inputHandler = InputHandler.Instance;
            characterController = GetComponent<CharacterController>();
            playerAnimatorHandler = GetComponent<PlayerAnimatorHandler>();

            InitializeDodgeFinalSpeedDict();
        }

        private void Update()
        {
            HandleDodgePerform();
            InterruptStepCheck();
            HandleWaitToDodgeAgain();
        }

        private void InterruptStepCheck()
        {
            canStepBeInterrupted = dodgePerformedCounter != 1 ? false : canStepBeInterrupted;
            wantsToInterruptStep = playerStatusHandler.CanDodge && playerStatusHandler.IsDodging && dodgePerformedCounter == 1 && canStepBeInterrupted && inputHandler.DodgePressed;
        }

        private void HandleWaitToDodgeAgain()
        {
            if (dodgePerformedCounter > 0)
            {
                if (dodgePerformedCounter == 1)
                {
                    timeElapsedAfterStep += Time.deltaTime;
                }
                else
                {
                    timeElapsedAfterStep = 0;

                    timeElapsedAfterRoll += Time.deltaTime;
                }

                bool timeElapsedCondition = timeElapsedAfterStep > nextStepTimeAfterStep || timeElapsedAfterRoll > nextStepTimeAfterRoll;
                dodgePerformedCounter = timeElapsedCondition ? 0 : dodgePerformedCounter;

                playerAnimatorHandler.Animator.SetInteger(playerAnimatorHandler.DodgeCounterHash, dodgePerformedCounter);
            }
            else
            {
                timeElapsedAfterStep = 0;
                timeElapsedAfterRoll = 0;
            }
        }

        private Vector3 ComputeAndStoreDodgeDirection()
        {
            Vector2 playerInput = inputHandler.MovementInput;
            Vector2 newMovement = playerInput;

            // Default Dodge is a Backward Dodge
            Vector3 dodgeDirection;
            if (playerInput.magnitude == 0)
            {
                newMovement = Vector2.down;
                dodgeDirection = newMovement.y * transform.forward;
            }
            else
            {
                if (playerStatusHandler.IsLocomotion1D)
                {
                    newMovement = Vector2.up;
                }

                dodgeDirection = Utils.ComputeMovementDirection(transform, newMovement, playerStatusHandler.IsLocomotion1D);
            }

            dodgeDirection = Vector3.ProjectOnPlane(dodgeDirection, Vector3.up);
            dodgeDirection = dodgeDirection.normalized;

            newMovement.x = Mathf.Abs(newMovement.x) > 0 ? Mathf.Sign(newMovement.x) : 0;
            newMovement.y = Mathf.Abs(newMovement.y) > 0 ? Mathf.Sign(newMovement.y) : 0;

            playerAnimatorHandler.Animator.SetFloat(playerAnimatorHandler.MovementXHash, newMovement.x);
            playerAnimatorHandler.Animator.SetFloat(playerAnimatorHandler.MovementYHash, newMovement.y);

            return dodgeDirection;
        }

        private void HandleDodgePerform()
        {
            if (dodgePerformedCounter < MAX_DODGE_PERFORMABLE)
            {
                if (playerStatusHandler.WantsToDodge || wantsToInterruptStep)
                {
                    Vector3 dodgeDirection = ComputeAndStoreDodgeDirection();

                    bool obstaclesPresent = playerStatusHandler.LookForObstacles(dodgeDirection);
                    if (!obstaclesPresent)
                    {
                        StartCoroutine(PerformDodgeCoroutine(dodgeDirection));

                        dodgePerformedCounter++;
                    }

                    inputHandler.DodgePressed = false;
                }
            }
        }

        private IEnumerator PerformDodgeCoroutine(Vector3 dodgeDirection)
        {
            AnimationCurve dodgeCurve = dodgeSpeedCurves[dodgePerformedCounter];
            float dodgeTimer = dodgeCurve[dodgeCurve.length - 1].time;
            int dodgeNumberToPerform = dodgePerformedCounter + 1;

            playerStatusHandler.IsDodging = true;
            playerAnimatorHandler.Animator.SetInteger(playerAnimatorHandler.DodgeCounterHash, dodgeNumberToPerform);
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsDodgingHash, true);

            MovementType movementType = playerLocomotionHandler.GetMovementType();
            float targetSpeed = dodgeFinalSpeedDict[movementType];

            if (dodgeNumberToPerform == 2)
            {
                dodgeCurve = ChangeAnimationCurveFinalValue(dodgeCurve, rollFinalSpeedScaler * targetSpeed);
                yield return PerformRoll(dodgeCurve, dodgeDirection, dodgeTimer);
            }
            else
            {
                dodgeCurve = ChangeAnimationCurveFinalValue(dodgeCurve, targetSpeed);
                yield return PerformStep(dodgeCurve, dodgeDirection, dodgeTimer);

                if (wantsToInterruptStep)
                {
                    playerStatusHandler.IsDodging = false;
                    playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsDodgingHash, false);

                    yield break;
                }
            }

            playerStatusHandler.IsDodging = false;
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsDodgingHash, false);
        }

        private IEnumerator PerformStep(AnimationCurve dodgeCurve, Vector3 dodgeDirection, float dodgeTimer)
        {
            RectTransform canvasLockOnTarget = GameObject.FindGameObjectWithTag(Utils.CANVAS_LOCK_ON_TARGET_TAG).GetComponent<RectTransform>();

            float timeElapsed = 0;
            while (timeElapsed < dodgeTimer)
            {
                // Ends the coroutine instantly
                if (wantsToInterruptStep)
                {
                    yield break;
                }

                // If player is targeting an enemy, he looks at target during the step
                if (playerStatusHandler.CanRotateTransform && playerStatusHandler.IsTargetingEnemy && canvasLockOnTarget.parent != null)
                {
                    Vector3 playerDirection = canvasLockOnTarget.parent.position - transform.position;

                    Utils.RotateTransformTowardsDirection(transform, playerDirection, playerLocomotionHandler.BodyAngleRotationSpeed);
                }

                float currentSpeedFactor = playerStatusHandler.GetFeature(Utils.SPEED_FEATURE_NAME).CurrentValue;

                playerStatusHandler.Speed = dodgeCurve.Evaluate(timeElapsed);
                playerAnimatorHandler.Animator.SetFloat(playerAnimatorHandler.SpeedHash, playerStatusHandler.Speed);
                characterController.Move(currentSpeedFactor * playerStatusHandler.Speed * Time.deltaTime * dodgeDirection);
                
                timeElapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator PerformRoll(AnimationCurve dodgeCurve, Vector3 dodgeDirection, float dodgeTimer)
        {
            float timeElapsed = 0;
            while (timeElapsed < dodgeTimer)
            {
                float currentSpeedFactor = playerStatusHandler.GetFeature(Utils.SPEED_FEATURE_NAME).CurrentValue;

                playerStatusHandler.Speed = dodgeCurve.Evaluate(timeElapsed);
                playerAnimatorHandler.Animator.SetFloat(playerAnimatorHandler.SpeedHash, playerStatusHandler.Speed);
                characterController.Move(currentSpeedFactor * playerStatusHandler.Speed * Time.deltaTime * dodgeDirection);
                
                timeElapsed += Time.deltaTime;
                yield return null;
            }            
        }

        private AnimationCurve ChangeAnimationCurveFinalValue(AnimationCurve curve, float value)
        {
            // Get last keyframe
            int lastKeyIndex = curve.keys.Length - 1;
            Keyframe lastKeyframe = curve.keys[lastKeyIndex];

            // Change last keyframe value
            lastKeyframe.value = value;

            // Set last keyframe
            curve.MoveKey(lastKeyIndex, lastKeyframe);

            return curve;
        }

        private void InitializeDodgeFinalSpeedDict()
        {
            dodgeFinalSpeedDict = new Dictionary<MovementType, float>
            {
                { MovementType.Idle, 0 },
                { MovementType.Walk, walkFinalSpeedScaler * playerLocomotionHandler.WalkSpeed },
                { MovementType.BaseRun, baseRunFinalSpeedScaler * playerLocomotionHandler.TargetingSpeed },
                { MovementType.FastRun, fastRunFinalSpeedScaler * playerLocomotionHandler.TargetingSpeed }
            };
        }
        
        public void MakeStepInterruptable()
        {
            canStepBeInterrupted = true;
        }

        private void OnPlayStepSound()
        {
            bool stepSoundCondition = playerStatusHandler.IsOnTheGround && (playerStatusHandler.WantsToDodge || playerStatusHandler.IsDodging);
            if (stepSoundCondition && dodgePerformedCounter == 1)
            {
                AudioManager.Instance.PlaySound(Utils.playerLightLandingAudioName, gameObject);
            }
        }

        private void OnPlayRollSound()
        {
            bool rollSoundCondition = playerStatusHandler.IsOnTheGround && (playerStatusHandler.WantsToDodge || playerStatusHandler.IsDodging);
            if (rollSoundCondition && dodgePerformedCounter == 2)
            {
                AudioManager.Instance.PlaySound(Utils.playerRollAudioName, gameObject);
            }
        }
    }
}
