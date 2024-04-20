// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    [RequireComponent(typeof(PlayerAnimatorHandler))]
    [RequireComponent(typeof(PlayerStatusHandler))]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerLocomotionHandler : CharacterLocomotionHandler
    {
        private CharacterController characterController;
        private PlayerStatusHandler playerStatusHandler;
        private PlayerAnimatorHandler playerAnimatorHandler;

        public enum MovementType { Idle, Walk, BaseRun, FastRun }

        private const float GROUNDED_GRAVITY = -0.1f;
        private float gravity;
        
        [Header("Speed Settings")]
        [SerializeField, Range(1, 10)] private float fastRunSpeed = 7f;

        [Header("Acceleration Time Settings")]
        [SerializeField, Range(0.1f, 3)] private float runFastTime = 0.5f;

        [Header("Rotation Settings")]
        [SerializeField, Min(0.1f)] private float animationBodyRotationSpeedTime = 0.3f;
        [SerializeField, Min(0.1f)] private float animationBodyRotationAimingDeadZone = 0.45f;
        [SerializeField, Min(0.1f)] private float animationBodyRotationTargetingDeadZone = 3.5f;
        
        [Header("Movement Settings")]
        [SerializeField, Range(0, 1)] float startToRunThreshold = 0.7f;
        
        [Header("Jump Settings")]
        [SerializeField] private float jumpHeight = 2.8f;
        [SerializeField] private float jumpTime = 1f;
        private float initialJumpSpeed;

        private Vector3 movementSpeed;
        private Vector3 appliedMovementSpeed;
        private float startFallingHeight;


        public float FastRunSpeed { get => fastRunSpeed; }
        public float BodyAngleRotationSpeed { get => RotationSpeed; }

        
        private void Start()
        {            
            characterController = GetComponent<CharacterController>();
            playerStatusHandler = GetComponent<PlayerStatusHandler>();
            playerAnimatorHandler = GetComponent<PlayerAnimatorHandler>();

            InitializeGravity();
            InitializeInitialJumpSpeed();
        }

        protected override void Update()
        {
            base.Update();
            
            HandleFallingHeight();
        }

        protected override void HandleSpeed()
        {
            HandleGravity();

            bool canHandleSpeedAlongXZAxis = !playerStatusHandler.WantsToDodge && !playerStatusHandler.IsDodging && !playerStatusHandler.IsPlayingWalkAnimation;
            if (canHandleSpeedAlongXZAxis)
            {
                // Update movement parameters in the animator
                UpdateAnimatorMovementParams();
                
                // Get the current movement type
                MovementType movementType = GetMovementType();

                // Array containing the accelerationFactor and the targetSpeed for the current movementType
                float[] accelerationData = ManageMovementType(movementType);

                // Handle player speed
                AcceleratePlayerUntilTargetSpeed(accelerationData[0], accelerationData[1]);
            }
        }
        
        public MovementType GetMovementType()
        {
            MovementType movementType = MovementType.Idle;
            float inputMagnitude = InputHandler.Instance.MovementInput.magnitude;

            // Idle
            if (!playerStatusHandler.CanMove || inputMagnitude == 0)
            {
                movementType = MovementType.Idle;

                // In case the player is facing a wall but he wants to turn, he will be able to do it.
                // Thus, in this particular case (Idle) there is the chance to have a non-zero speed (Walk).
                // The dot product is used to check if there is an angle, big enough,
                // between the player forward direction and the movementDirection.
                // The bigger the bigger the dot product value, the lower is the angle between the 2 direction,
                // that's why only if the dot value is < 0.95, the player will be able to move.
                // Besides, when the locomotion is 2D, the transform rotation is handled differently thus, in this case,
                // the non-zero speed is not allowed.
                Vector3 movementDirection = Utils.ComputeInputDirectionRelativeToCamera(InputHandler.Instance.MovementInput);
                if (playerStatusHandler.LookForObstacles() && playerStatusHandler.IsLocomotion1D && Vector3.Dot(movementDirection, transform.forward) < 0.95f && inputMagnitude > 0)
                {
                    movementType = MovementType.Walk;
                }
            }
            // Walking or Base Run
            else if ((inputMagnitude > 0 && !playerStatusHandler.IsRunningFast) || playerStatusHandler.HasToWalk)
            {
                // Walking
                if ((inputMagnitude > 0 && inputMagnitude < startToRunThreshold) || playerStatusHandler.HasToWalk)
                {
                    movementType = MovementType.Walk;
                }
                // Base Run
                else
                {
                    if (inputMagnitude >= startToRunThreshold)
                    {
                        movementType = MovementType.BaseRun;
                    }
                }
            }
            // Fast Run
            else
            {
                if (inputMagnitude >= startToRunThreshold && playerStatusHandler.IsRunningFast)
                {
                    movementType = MovementType.FastRun;
                }
            }

            return movementType;
        }

        private float[] ManageMovementType(MovementType movementType)
        {
            float targetSpeed = 0;
            float timeToAccelerate;
            float accelerationFactor = 0;

            switch (movementType)
            {
                case MovementType.Idle:
                    targetSpeed = 0;
                    timeToAccelerate = StopMovingTime;
                    accelerationFactor = fastRunSpeed / timeToAccelerate;

                    break;

                case MovementType.Walk:
                    targetSpeed = WalkSpeed;
                    accelerationFactor = playerStatusHandler.Speed <= WalkSpeed ? WalkSpeed / StartMovingTime : fastRunSpeed / StopMovingTime;

                    break;

                case MovementType.BaseRun:
                    targetSpeed = playerStatusHandler.IsTargetingEnemy ? TargetingSpeed : BaseRunSpeed;
                    timeToAccelerate = playerStatusHandler.Speed <= BaseRunSpeed ? StartMovingTime : 2 * StartMovingTime;
                    accelerationFactor = BaseRunSpeed / timeToAccelerate;

                    break;

                case MovementType.FastRun:
                    targetSpeed = fastRunSpeed;
                    timeToAccelerate = playerStatusHandler.Speed <= BaseRunSpeed ? StartMovingTime : runFastTime;
                    accelerationFactor = fastRunSpeed / timeToAccelerate;

                    break;
            }

            return new float[] { accelerationFactor, targetSpeed };
        }        
        private void AcceleratePlayerUntilTargetSpeed(float accelerationFactor, float targetSpeed)
        {
            // Get the movement from the animator
            Vector2 movement = new Vector2(playerAnimatorHandler.Animator.GetFloat(playerAnimatorHandler.MovementXHash),
                                            playerAnimatorHandler.Animator.GetFloat(playerAnimatorHandler.MovementYHash));

            // Compute movement direction
            Vector3 movementDirection = Utils.ComputeMovementDirection(transform, movement, playerStatusHandler.IsLocomotion1D);

            // Accelerate or decelerate the player until it reaches the targetSpeed
            playerStatusHandler.Speed = Utils.FloatInterpolation(playerStatusHandler.Speed, targetSpeed, accelerationFactor * Time.deltaTime);

            // Update player speed in animator
            playerAnimatorHandler.Animator.SetFloat(playerAnimatorHandler.SpeedHash, playerStatusHandler.Speed);

            // Compute movement vector
            movementSpeed.x = playerStatusHandler.Speed * movementDirection.x;
            movementSpeed.z = playerStatusHandler.Speed * movementDirection.z;
            appliedMovementSpeed.x = playerStatusHandler.Speed * movementDirection.x;
            appliedMovementSpeed.z = playerStatusHandler.Speed * movementDirection.z;

            float currentSpeedFactor = playerStatusHandler.GetFeature(Utils.SPEED_FEATURE_NAME).CurrentValue;

            // Move the player
            characterController.Move(currentSpeedFactor * Time.deltaTime * appliedMovementSpeed);
        }

        protected override void UpdateAnimatorMovementParams()
        {
            Vector2 playerInput = InputHandler.Instance.MovementInput;

            float currentMovementX = playerAnimatorHandler.Animator.GetFloat(playerAnimatorHandler.MovementXHash);
            float currentMovementY = playerAnimatorHandler.Animator.GetFloat(playerAnimatorHandler.MovementYHash);

            float newMovementX = 0, newMovementY = 0;
            if (playerInput.magnitude != 0 || playerStatusHandler.Speed > 0)
            {
                if (playerStatusHandler.IsLocomotion1D)
                {
                    newMovementX = 0;
                    newMovementY = 1;
                }
                else
                {
                    newMovementX = playerInput.x;
                    newMovementY = playerInput.y;
                }
            }

            currentMovementX = Utils.FloatInterpolation(currentMovementX, newMovementX, Time.deltaTime / MovementLerpTime);
            currentMovementY = Utils.FloatInterpolation(currentMovementY, newMovementY, Time.deltaTime / MovementLerpTime);

            playerAnimatorHandler.Animator.SetFloat(playerAnimatorHandler.MovementXHash, currentMovementX);
            playerAnimatorHandler.Animator.SetFloat(playerAnimatorHandler.MovementYHash, currentMovementY);
        }

        private void HandleFallingHeight()
        {
            float fallingHeight = playerAnimatorHandler.Animator.GetFloat(playerAnimatorHandler.FallingHeightHash);

            if (!playerStatusHandler.IsOnTheGround)
            {
                // Player is falling
                if (movementSpeed.y < 0)
                {
                    // Start Falling
                    if (startFallingHeight == 0)
                    {
                        startFallingHeight = transform.position.y;
                        fallingHeight = 0;
                    }
                    // Continue Falling
                    else
                    {
                        fallingHeight = Mathf.Abs(transform.position.y - startFallingHeight);
                    }
                }
            }
            // Player is not falling or fall is ended
            else
            {
                // End Falling
                if (playerStatusHandler.IsLanding)
                {
                    startFallingHeight = 0;
                    fallingHeight = 0;
                }
            }

            playerAnimatorHandler.Animator.SetFloat(playerAnimatorHandler.FallingHeightHash, fallingHeight);
        }

        protected override void HandleRotation()
        {
            void ResetXRotationAngle()
            {
                float currentXRotationValue = playerAnimatorHandler.Animator.GetFloat(playerAnimatorHandler.XRotationAngleHash);
                currentXRotationValue = Utils.FloatInterpolation(currentXRotationValue, 0, Time.deltaTime / 0.25f);
                playerAnimatorHandler.Animator.SetFloat(playerAnimatorHandler.XRotationAngleHash, currentXRotationValue);
            }

            // Player is moving
            if (playerStatusHandler.CanRotateTransform && !playerStatusHandler.IsRotatingWithAnimation &&
                !playerStatusHandler.WantsToDodge && !playerStatusHandler.IsDodging)
            {
                HandleTransformRotation(bodyAngleRotationSpeed: RotationSpeed);

                // Player is not rotating with animation, thus this value is reset to 0
                ResetXRotationAngle();
            }
            // Player is rotating but not moving 
            else if (playerStatusHandler.IsRotatingWithAnimation)
            {
                HandleAnimationRotation();

                if (playerStatusHandler.IsAiming)
                {
                    HandleTransformRotation(bodyAngleRotationSpeed: RotationSpeed);
                }
                else if (playerStatusHandler.IsTargetingEnemy)
                {
                    HandleTransformRotation(bodyAngleRotationSpeed: 0.45f * RotationSpeed);
                }
            }
            // Player is neither rotating nor moving 
            else
            {
                ResetXRotationAngle();
            }
        }

        private void HandleTransformRotation(float bodyAngleRotationSpeed)
        {
            Vector3 playerDirection;

            // Player looks at target
            if (playerStatusHandler.IsTargetingEnemy && playerStatusHandler.Speed <= TargetingSpeed && !playerStatusHandler.IsAiming)
            {
                RectTransform canvasTarget = GameObject.FindWithTag(Utils.CANVAS_LOCK_ON_TARGET_TAG).GetComponent<RectTransform>();
                Transform target = canvasTarget.transform.parent;
                playerDirection = target.position - transform.position;
            }
            // Player looks at camera forward direction
            else
            {
                playerDirection = GetPlayerDirectionRelativeToCamera();
            }

            Utils.RotateTransformTowardsDirection(transform, playerDirection, bodyAngleRotationSpeed);
        }

        private void HandleAnimationRotation()
        {
            float currentXRotationValue = playerAnimatorHandler.Animator.GetFloat(playerAnimatorHandler.XRotationAngleHash);

            float newXRotationValue = 0;
            if (playerStatusHandler.IsAiming)
            {
                newXRotationValue = InputHandler.Instance.CameraRotationInput.x;

                // Deadzone on newXRotationValue
                newXRotationValue = Mathf.Abs(newXRotationValue) > 0.45f ? newXRotationValue : 0;
            }
            else if (playerStatusHandler.IsTargetingEnemy)
            {
                RectTransform canvasTarget = GameObject.FindWithTag(Utils.CANVAS_LOCK_ON_TARGET_TAG).GetComponent<RectTransform>();
                Transform target = canvasTarget.transform.parent;
                Vector3 rotationDirection = target.position - transform.position;

                Vector3 projectedPlayerForwardDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                Vector3 projectedRotationDirection = Vector3.ProjectOnPlane(rotationDirection, Vector3.up);
                newXRotationValue = Vector3.SignedAngle(projectedPlayerForwardDirection, projectedRotationDirection, Vector3.up);

                // Deadzone and Sign on the newXRotationValue
                newXRotationValue = Mathf.Abs(newXRotationValue) > 5f ? Mathf.Sign(newXRotationValue) : 0;
            }

            newXRotationValue = newXRotationValue != 0 ? Mathf.Sign(newXRotationValue) : 0;
            currentXRotationValue = Utils.FloatInterpolation(currentXRotationValue, newXRotationValue, Time.deltaTime / animationBodyRotationSpeedTime);

            playerAnimatorHandler.Animator.SetFloat(playerAnimatorHandler.XRotationAngleHash, currentXRotationValue);
        }

        private Vector3 GetPlayerDirectionRelativeToCamera()
        {
            if (playerStatusHandler.IsLocomotion1D)
            {
                Vector3 inputDirection = new Vector3(InputHandler.Instance.MovementInput.x, 0, InputHandler.Instance.MovementInput.y);
                
                return Camera.main.transform.TransformDirection(inputDirection);
            }
            else
            {
                return Camera.main.transform.forward;
            }
        }

        private void HandleGravity()
        {
            // If player is not on the ground or vertical speed is positive, gravity will reduce it
            if (!playerStatusHandler.IsOnTheGround || movementSpeed.y > 0)
            {
                // Use Verlet Integration to make the jump trajectory frame rate independent
                float previuosSpeed = movementSpeed.y;
                movementSpeed.y += gravity * Time.deltaTime;
                appliedMovementSpeed.y = (previuosSpeed + movementSpeed.y) / 2;
            }
            // Otherwise it's mean player is on the floor
            else if (!playerStatusHandler.JumpStarted)
            {
                movementSpeed.y = GROUNDED_GRAVITY;
                appliedMovementSpeed.y = GROUNDED_GRAVITY;
            }
        }

        protected override void OnPlayWalkSound()
        {
            bool dodgeCondition = !playerStatusHandler.WantsToDodge && !playerStatusHandler.IsDodging;
            bool attackCondition = !playerStatusHandler.WantsToAttack && !playerStatusHandler.IsAttacking && !playerStatusHandler.WantsToCastSkill && !playerStatusHandler.IsCastingSkill;

            bool walkSoundCondition = playerStatusHandler.IsOnTheGround && dodgeCondition && attackCondition;
            if (walkSoundCondition)
            {
                AudioManager.Instance.PlaySound(Utils.playerWalkAudioName, gameObject);
            }
        }

        protected override void OnPlayRunSound()
        {
            bool dodgeCondition = !playerStatusHandler.WantsToDodge && !playerStatusHandler.IsDodging;
            bool attackCondition = !playerStatusHandler.WantsToAttack && !playerStatusHandler.IsAttacking && !playerStatusHandler.WantsToCastSkill && !playerStatusHandler.IsCastingSkill;

            bool runSoundCondition = playerStatusHandler.IsOnTheGround && dodgeCondition && attackCondition;
            if (runSoundCondition)
            {
                AudioManager.Instance.PlaySound(Utils.playerRunAudioName, gameObject);
            }
        }

        private void OnJumping()
        {
            movementSpeed.y = initialJumpSpeed;
            appliedMovementSpeed.y = initialJumpSpeed;

            // Play jump sound
            bool jumpSoundCondition = playerStatusHandler.IsOnTheGround;
            if (jumpSoundCondition)
            {
                //AudioManager.Instance.PlaySound(Utils., gameObject);
            }
        }

        protected override void OnPlayLightLandingSound()
        {
            bool landingSoundCondition = playerStatusHandler.IsOnTheGround;
            if (landingSoundCondition)
            {
                AudioManager.Instance.PlaySound(Utils.playerLightLandingAudioName, gameObject);
            }
        }

        protected override void OnPlayHeavyLandingSound()
        {
            bool landingSoundCondition = playerStatusHandler.IsOnTheGround;
            if (landingSoundCondition)
            {
                AudioManager.Instance.PlaySound(Utils.playerHeavyLandingAudioName, gameObject);
            }
        }

        private void InitializeGravity()
        {
            float timeToReachMaxHeight = jumpTime / 2;
            gravity = -2 * jumpHeight / Mathf.Pow(timeToReachMaxHeight, 2);
        }

        public void InitializeInitialJumpSpeed()
        {
            float timeToReachMaxHeight = Mathf.Sqrt(-2 * jumpHeight / gravity);
            initialJumpSpeed = 2 * jumpHeight / timeToReachMaxHeight;
        }
    }
}
