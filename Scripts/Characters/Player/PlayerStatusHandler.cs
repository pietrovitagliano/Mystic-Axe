// Author: Pietro Vitagliano

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    [RequireComponent(typeof(PlayerAnimatorHandler))]
    [RequireComponent(typeof(PlayerLocomotionHandler))]
    [RequireComponent(typeof(PlayerDodgeHandler))]
    [RequireComponent(typeof(PlayerStateManager))]
    public class PlayerStatusHandler : CharacterStatusHandler
    {
        private InputHandler inputHandler;
        private PlayerCombatHandler playerCombatHandler;
        private PlayerAnimatorHandler playerAnimatorHandler;
        private PlayerLocomotionHandler playerLocomotionHandler;
        private PlayerDodgeHandler playerDodgeHandler;

        private CharacterController characterController;
        private CapsuleCollider physicsCapsuleCollider;

        private RectTransform canvasLockOnTarget;
        
        private Transform axe;
        private AxeStatusHandler axeStatusHandler;
        private AxeReturnHandler axeReturnHandler;

        [Header("Is On The ground")]
        [SerializeField, Range(0.1f, 2)] private float groundDistance = 0.5f;

        [Header("Look For Obstacles")]
        [SerializeField, Min(0.1f)] private float lookForObstacleDistance = 0.75f;

        [Header("Enemy Nearby Settings")]
        [SerializeField, Range(0.1f, 1000)] float enemyNearbyRadius = 100f;


        #region Status Variables
        private bool isLocomotion1D;
        private bool isOnTheGround;
        private bool isLanding;
        private bool isRotatingWithAnimation;
        private bool isPlayingWalkAnimation;
        private bool hasToWalk;
        private bool canDodge;
        private bool wantsToDodge;
        private bool isDodging;
        private bool isRunningFast;
        private bool fastRunSpeedReached;
        private bool jumpStarted;
        private bool isJumping;
        private int jumpCounter = 0;
        [SerializeField, Min(1)] private int jumpMaxNumber;
        private bool isEquipingOrUnequipingAnimationInProgress;
        private bool isAxeOnBody;
        private bool isAxeInHand;
        private bool isAiming;
        private bool isThrowingAxe;
        private bool isRecallingAxe;
        private bool isTargetingEnemy;
        private bool canDodgeWhileAttacking;
        private bool isBlocking;
        private bool canInteract;
        private bool wantsToUseConsumable;
        private bool isUsingConsumable;
        private bool wantsToChangeConsumable;
        private bool enemyNearby;
        #endregion


        #region Status Properties
        public bool IsRunningFast { get => isRunningFast; }
        public bool JumpStarted { get => jumpStarted; }
        public bool IsLocomotion1D { get => isLocomotion1D; }
        public bool IsOnTheGround { get => isOnTheGround; }
        public bool CanDodge { get => canDodge; }
        public bool WantsToDodge { get => wantsToDodge; }
        public bool IsDodging { get => isDodging; set => isDodging = value; }
        public bool IsAiming { get => isAiming; }
        public bool IsThrowingAxe { get => isThrowingAxe; }
        public bool IsRecallingAxe { get => isRecallingAxe; }
        public bool IsTargetingEnemy { get => isTargetingEnemy; }
        public bool IsBlocking { get => isBlocking; }
        public bool IsAxeOnBody { get => isAxeOnBody; }
        public bool IsAxeInHand { get => isAxeInHand; }
        public bool IsPlayingWalkAnimation { get => isPlayingWalkAnimation; set => isPlayingWalkAnimation = value; }
        public bool HasToWalk { get => hasToWalk; }
        public bool IsLanding { get => isLanding; }
        public bool CanInteract { get => canInteract; }
        public bool IsRotatingWithAnimation { get => isRotatingWithAnimation; }
        public bool WantsToUseConsumable { get => wantsToUseConsumable; }
        public bool IsUsingConsumable { get => isUsingConsumable; set => isUsingConsumable = value; }
        public bool WantsToChangeConsumable { get => wantsToChangeConsumable; }
        public bool EnemyNearby { get => enemyNearby; }
        #endregion

        protected override List<Feature> InitializeFeatures()
        {
            return JsonDatabase.Instance.GetDataFromJson<FeaturesJsonMap>(Utils.PLAYER_FEATURES_JSON_NAME).Features;
        }

        protected override void Start()
        {
            inputHandler = InputHandler.Instance;
            playerCombatHandler = GetComponent<PlayerCombatHandler>();
            playerAnimatorHandler = GetComponent<PlayerAnimatorHandler>();
            playerLocomotionHandler = GetComponent<PlayerLocomotionHandler>();
            playerDodgeHandler = GetComponent<PlayerDodgeHandler>();
            
            canvasLockOnTarget = FindObjectOfType<LockOnHandler>().CanvasLockOnTarget;

            // Since the character controller isn't affected by physics,
            // a CapsuleCollider is used to manage situations managed by physics.
            // In these situatioons, the character controller will be disabled,
            // whilst the capsule collider will be enabled
            characterController = GetComponent<CharacterController>();
            if (!TryGetComponent(out physicsCapsuleCollider))
            {
                physicsCapsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            }

            physicsCapsuleCollider.center = characterController.center;
            physicsCapsuleCollider.radius = characterController.radius;
            physicsCapsuleCollider.height = characterController.height;
            physicsCapsuleCollider.isTrigger = false;
            physicsCapsuleCollider.enabled = false;

            InitializeAxeVariables();

            base.Start();
        }
        
        protected override void UpdateStatus()
        {
            IsDeadCheck();
            HandleColliders();
            IsOnTheGroundCheck();
            IsLandingCheck();
            LocomotionCheck();
            CanInteractCheck();
            MoveCheck();
            HasToWalkCheck();
            RotationCheck();
            DodgeCheck();
            FastRunCheck();
            JumpCheck();
            AxeOnBodyCheck();
            EquipUnequipAxeCheck();
            AimCheck();
            ThrowAxeCheck();
            RecallAxeCheck();
            CatchAxeCheck();
            BlockCheck();
            TargetEnemyCheck();
            HitCheck();
            AttackCheck();
            IsDoingHeavyAttackCheck();
            UseConsumableCheck();
            ChangeConsumableCheck();
            InCombatCheck();
            EnemyNearbyCheck();
        }

        private void InitializeAxeVariables()
        {
            axe = GameObject.FindGameObjectWithTag(Utils.PLAYER_AXE_TAG).transform;

            axeStatusHandler = axe.GetComponent<AxeStatusHandler>();
            axeReturnHandler = axe.GetComponent<AxeReturnHandler>();
        }

        private void HandleColliders()
        {
            characterController.enabled = Rigidbody.isKinematic;
            physicsCapsuleCollider.enabled = !characterController.enabled;
        }

        #region Status Check Methods
        private void CanInteractCheck()
        {
            canInteract = !wantsToDodge && !isDodging && !WantsToAttack && !IsAttacking
                            && !WantsToCastSkill  && !isThrowingAxe && !isRecallingAxe && !isAiming
                            && !isTargetingEnemy && !isBlocking;
        }
        
        private void LocomotionCheck()
        {
            float playerSpeed = playerAnimatorHandler.Animator.GetFloat(playerAnimatorHandler.SpeedHash);
            float targetingBaseRunSpeed = playerLocomotionHandler.TargetingSpeed;
            bool dodgeCondition = !wantsToDodge && !playerDodgeHandler.WantsToInterruptStep && !isDodging;
            
            isLocomotion1D = !IsAiming && !isRecallingAxe && (!IsTargetingEnemy || (IsTargetingEnemy && playerSpeed > targetingBaseRunSpeed && dodgeCondition));
        }

        private void IsOnTheGroundCheck()
        {
            if (characterController.isGrounded)
            {
                isOnTheGround = true;
            }
            else
            {
                LayerMask groundLayerMask = LayerMask.GetMask(Utils.GROUND_LAYER_NAME, Utils.OBSTACLES_LAYER_NAME);
                Vector3 groundDirection = transform.TransformDirection(Vector3.down);

                isOnTheGround = Physics.RaycastAll(transform.position, groundDirection, groundDistance, groundLayerMask, QueryTriggerInteraction.Ignore)
                                        .Any(hit => Vector3.Angle(hit.normal, -groundDirection) <= characterController.slopeLimit);
            }
                            
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsGroundedHash, isOnTheGround);
        }

        private void IsLandingCheck()
        {
            int movementLayer = playerAnimatorHandler.Animator.GetLayerIndex(Utils.MOVEMENT_LAYER_NAME);
            
            bool isLandingLowHeight = playerAnimatorHandler.Animator.GetNextAnimatorStateInfo(movementLayer).IsName(Utils.landingLowAnimationStateName) ||
                                        (playerAnimatorHandler.Animator.GetCurrentAnimatorStateInfo(movementLayer).IsName(Utils.landingLowAnimationStateName) &&
                                        playerAnimatorHandler.Animator.GetCurrentAnimatorStateInfo(movementLayer).normalizedTime < 1);

            bool isLandingMidHeight = playerAnimatorHandler.Animator.GetNextAnimatorStateInfo(movementLayer).IsName(Utils.landingMidAnimationStateName) ||
                                        (playerAnimatorHandler.Animator.GetCurrentAnimatorStateInfo(movementLayer).IsName(Utils.landingMidAnimationStateName) &&
                                        playerAnimatorHandler.Animator.GetCurrentAnimatorStateInfo(movementLayer).normalizedTime < 1);

            isLanding = isLandingLowHeight || isLandingMidHeight;
        }

        protected override void MoveCheck()
        {
            bool moveCondition = !IsDead && !isLanding && !LookForObstacles() && Rigidbody.isKinematic && !IsHitAnimationOnGoing;
            bool moveConditionWhileAttacking = !IsAttacking || (IsAttacking && playerCombatHandler.CanAttackBeInterruptedByMovement);
            bool moveConditionWhileCastingSkill = !IsCastingSkill || (IsCastingSkill && playerCombatHandler.CanAttackBeInterruptedByMovement);

            CanMove = moveCondition && !WantsToAttack && moveConditionWhileAttacking && !WantsToCastSkill && moveConditionWhileCastingSkill;
        }

        private void HasToWalkCheck()
        {
            hasToWalk = isPlayingWalkAnimation || isUsingConsumable || isAiming || isRecallingAxe || WantsToAttack || (isBlocking && isOnTheGround);
        }

        protected override void RotationCheck()
        {
            CinemachineHandler playerCMCameraScript = Camera.main.GetComponent<CinemachineHandler>();

            bool dodgeCondition = !wantsToDodge && !isDodging;
            bool baseAttackCondition = !WantsToAttack && !IsAttacking;
            bool skillCondition = !WantsToCastSkill && !IsCastingSkill;
            bool generalAttackCondition = baseAttackCondition && skillCondition;
            
            CanRotateTransform = !IsDead && !IsHitAnimationOnGoing && !playerCMCameraScript.IsCameraResetting && Speed > 0 && generalAttackCondition;
            isRotatingWithAnimation = !IsDead && !IsHitAnimationOnGoing && !isLocomotion1D && !isUsingConsumable && Speed == 0 && generalAttackCondition && dodgeCondition;

            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsRotatingHash, isRotatingWithAnimation);
        }

        private void DodgeCheck()
        {
            bool moveCondition = !IsDead && isOnTheGround && !isLanding && !LookForObstacles() && Rigidbody.isKinematic;
            bool attackingCondition = !IsAttacking || (IsAttacking && playerCombatHandler.CanAttackBeInterruptedByDodge);
            bool generalAttackCondition = !WantsToAttack && attackingCondition && !WantsToCastSkill && !IsCastingSkill;

            canDodge = CanMove && !isUsingConsumable && !isJumping && !isThrowingAxe && !isRecallingAxe;
            canDodgeWhileAttacking = moveCondition && !isUsingConsumable && !isJumping && !isThrowingAxe && !isRecallingAxe && generalAttackCondition;
            wantsToDodge = !isDodging && inputHandler.DodgePressed && (canDodge || canDodgeWhileAttacking);
        }

        private void FastRunCheck()
        {
            bool stopFastRunCondition = !isOnTheGround || isLanding || inputHandler.MovementInput.magnitude == 0 || hasToWalk ||
                                        (fastRunSpeedReached && Speed < playerLocomotionHandler.FastRunSpeed);
            
            if (!IsDead && stopFastRunCondition)
            {
                isRunningFast = false;
            }
            else
            {
                bool dodgeCondition = !wantsToDodge && !isDodging;
                if (CanMove && dodgeCondition && !isUsingConsumable && inputHandler.FastRunPressed)
                {
                    inputHandler.FastRunPressed = false;
                    isRunningFast = !isRunningFast;
                }
            }

            // If the player is running fast, fast run speed cannot be reached
            if (!isRunningFast)
            {
                fastRunSpeedReached = false;
            }
            // If is running fast and fast run speed has not been reached yet, check if it has been reached
            else if (!fastRunSpeedReached)
            {
                fastRunSpeedReached = Speed >= playerLocomotionHandler.FastRunSpeed;
            }
        }

        private void JumpCheck()
        {
            jumpCounter = isOnTheGround ? 0 : jumpCounter == 0 ? 1 : jumpCounter;

            if (jumpCounter < jumpMaxNumber)
            {
                // A new jump is started
                jumpStarted = !isJumping && !isDodging && Rigidbody.isKinematic && inputHandler.JumpPressed;

                // Perform a jump
                if (jumpStarted)
                {
                    playerAnimatorHandler.Animator.SetTrigger(playerAnimatorHandler.JumpStartedHash);
                    isJumping = true;
                    jumpCounter += 1;
                }
            }
            else
            {
                jumpStarted = false;
            }

            // If player releases the jump button, the jump is considered ended.
            // Anyway, it can't be considered ended if doesn't bring the player in the air.
            if (!IsDead && !isOnTheGround && isJumping && !inputHandler.JumpPressed)
            {
                isJumping = false;
            }
        }

        private void AxeOnBodyCheck()
        {
            isAxeInHand = axeStatusHandler.IsAxeInHand();
            isAxeOnBody = axeStatusHandler.IsAxeOnBody();

            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsAxeInHandHash, isAxeInHand);

            if (isAxeInHand)
            {
                playerAnimatorHandler.Animator.ResetTrigger(playerAnimatorHandler.CatchAxeHash);
            }
        }

        private void EquipUnequipAxeCheck()
        {
            if (!IsDead && isAxeOnBody)
            {
                int equipUnequipAxeLayer = playerAnimatorHandler.Animator.GetLayerIndex(Utils.EQUIP_UNEQUIP_LAYER_NAME);
                isEquipingOrUnequipingAnimationInProgress = !playerAnimatorHandler.Animator.GetCurrentAnimatorStateInfo(equipUnequipAxeLayer).IsName(Utils.emptyAnimationStateName);

                if (!isEquipingOrUnequipingAnimationInProgress)
                {
                    // Start Animation Equip/Unequip Axe
                    bool recallAxeFromFolder = axeStatusHandler.IsAxeIsInFolder() && inputHandler.RecallAxePressed;
                    bool dodgeCondition = !wantsToDodge && !isDodging;
                    bool attackCondition = !WantsToAttack && !IsAttacking && !WantsToCastSkill && !IsCastingSkill;
                    if (dodgeCondition && attackCondition && !isUsingConsumable && (inputHandler.EquipAxePressed || recallAxeFromFolder))
                    {
                        inputHandler.EquipAxePressed = false;
                        inputHandler.RecallAxePressed = false;
                        playerAnimatorHandler.Animator.SetTrigger(playerAnimatorHandler.EquipUnequipAxeTriggerHash);
                    }
                }
                else
                {
                    playerAnimatorHandler.Animator.ResetTrigger(playerAnimatorHandler.EquipUnequipAxeTriggerHash);
                }
            }
        }

        protected override void InCombatCheck()
        {            
            InCombat = !IsDead && (isAiming || isTargetingEnemy || isBlocking || IsAttacking || IsCastingSkill);
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.InCombatHash, InCombat);
        }

        private void AimCheck()
        {
            bool attackingCondition = !IsAttacking || (IsAttacking && playerCombatHandler.CanAttackBeInterruptedByMovement);
            bool generalAttackCondition = !WantsToAttack && attackingCondition && !WantsToCastSkill && !IsCastingSkill;

            isAiming = !IsDead && (isThrowingAxe || (isOnTheGround && !isUsingConsumable && !isPlayingWalkAnimation && generalAttackCondition && inputHandler.AimPressed));
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsAimingHash, isAiming);
        }

        private void ThrowAxeCheck()
        {
            int throwAxeLayer = playerAnimatorHandler.Animator.GetLayerIndex(Utils.THROW_AXE_LAYER_NAME);
            int recallAxeLayer = playerAnimatorHandler.Animator.GetLayerIndex(Utils.RECALL_AXE_LAYER_NAME);
            
            bool attackingCondition = !IsAttacking || (IsAttacking && playerCombatHandler.CanAttackBeInterruptedByMovement);
            bool generalAttackCondition = !WantsToAttack && attackingCondition && !WantsToCastSkill && !IsCastingSkill;
            bool canThrowAxeAfterRecall = playerAnimatorHandler.Animator.GetCurrentAnimatorStateInfo(recallAxeLayer).IsName(Utils.emptyAnimationStateName) ||
                                            playerAnimatorHandler.Animator.GetNextAnimatorStateInfo(recallAxeLayer).IsName(Utils.emptyAnimationStateName);
            
            if (isAiming && isAxeInHand && !isUsingConsumable && !isLanding && !isThrowingAxe && generalAttackCondition && canThrowAxeAfterRecall && inputHandler.ThrowAxePressed)
            {
                inputHandler.ThrowAxePressed = false;
                playerAnimatorHandler.Animator.SetTrigger(playerAnimatorHandler.ThrowAxeHash);
                isThrowingAxe = true;
            }
            else
            {                
                // Check if the throw animation inside throwAxeLayer has ended
                if (playerAnimatorHandler.Animator.GetNextAnimatorStateInfo(throwAxeLayer).IsName(Utils.emptyAnimationStateName) ||
                    (playerAnimatorHandler.Animator.GetCurrentAnimatorStateInfo(throwAxeLayer).IsName(Utils.throwAxeAnimationStateName) &&
                    playerAnimatorHandler.Animator.GetCurrentAnimatorStateInfo(throwAxeLayer).normalizedTime >= 1))
                {
                    isThrowingAxe = false;
                }
            }
        }

        private void RecallAxeCheck()
        {
            if (!IsDead)
            {
                if (!isAxeOnBody && !isUsingConsumable && !isRecallingAxe && !isDodging && inputHandler.RecallAxePressed)
                {
                    inputHandler.RecallAxePressed = false;
                    inputHandler.RecallAxePressed = false;
                    isRecallingAxe = true;
                }
            }
            else
            {
                isRecallingAxe = false;
            }

            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsRecallingAxeHash, isRecallingAxe);
        }

        private void CatchAxeCheck()
        {
            float elapsedReturnTimeNormalized = axeReturnHandler.ElapsedReturnTime / axeReturnHandler.ReturnTime;
            if (isRecallingAxe && !isAxeOnBody && elapsedReturnTimeNormalized >= 1)
            {
                isRecallingAxe = false;
                playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsRecallingAxeHash, false);
                playerAnimatorHandler.Animator.SetTrigger(playerAnimatorHandler.CatchAxeHash);
            }
        }

        private void BlockCheck()
        {
            bool dodgeCondition = !wantsToDodge && !IsDodging;
            bool attackingCondition = !IsAttacking || (IsAttacking && playerCombatHandler.CanAttackBeInterruptedByMovement);
            bool generalAttackCondition = attackingCondition && !IsCastingSkill;
            
            isBlocking = !IsDead && !IsHitAnimationOnGoing && !isThrowingAxe && !isUsingConsumable && dodgeCondition && generalAttackCondition && inputHandler.BlockPressed;
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsBlockingHash, isBlocking);
        }

        private void TargetEnemyCheck()
        {
            isTargetingEnemy = !IsDead && canvasLockOnTarget.parent != null;
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsTargetingHash, isTargetingEnemy);
        }

        protected override void AttackCheck()
        {
            bool dodgeCondition = !wantsToDodge && !isDodging;
            bool canAttack = !IsDead && isOnTheGround && !IsForceEffectOnGoing && isAxeInHand && !isUsingConsumable &&
                                dodgeCondition && !isThrowingAxe && !isRecallingAxe && !isEquipingOrUnequipingAnimationInProgress;

            bool attackInputPressed = inputHandler.LightAttackPressed || inputHandler.HeavyAttackPressed;
            bool skillButtonPressed = inputHandler.FirstSkillPressed || inputHandler.SecondSkillPressed;

            WantsToAttack = attackInputPressed && canAttack && !WantsToCastSkill && !IsCastingSkill && !isAiming && !isBlocking &&
                            (!IsAttacking || playerCombatHandler.CanAttackBeInterruptedByNextAttack);

            WantsToCastSkill = canAttack && isBlocking && !IsCastingSkill && skillButtonPressed;

            if (IsAttacking)
            {
                float playerSpeed = playerAnimatorHandler.Animator.GetFloat(playerAnimatorHandler.SpeedHash);
                IsAttacking = !IsDead && !IsHitAnimationOnGoing && !WantsToCastSkill && !IsCastingSkill && dodgeCondition && !isThrowingAxe && !isBlocking &&
                                !(playerSpeed > 0 && playerCombatHandler.CanAttackBeInterruptedByMovement);
            }

            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsCastingSpecialAttackHash, IsCastingSkill);
        }
        
        protected override void IsDoingHeavyAttackCheck()
        {
            if (IsDoingHeavyAttack)
            {
                IsDoingHeavyAttack = IsAttacking;
            }
        }

        private void UseConsumableCheck()
        {
            bool dodgeCondition = !wantsToDodge && !isDodging;
            bool attackCondition = !WantsToAttack && !IsAttacking && !WantsToCastSkill && !IsCastingSkill;
            bool throwAxeCondition = !isThrowingAxe && !isRecallingAxe;
            
            wantsToUseConsumable = !IsDead && isOnTheGround && !isBlocking && Rigidbody.isKinematic && dodgeCondition &&
                                    attackCondition && throwAxeCondition && !isUsingConsumable && inputHandler.UseConsumablePressed;
            
            playerAnimatorHandler.Animator.SetBool(playerAnimatorHandler.IsUsingConsumableHash, isUsingConsumable);
        }

        private void ChangeConsumableCheck()
        {
            wantsToChangeConsumable = !IsDead && !isUsingConsumable && inputHandler.ChangeConsumablePressed;
        }

        private void EnemyNearbyCheck()
        {
            Vector3 playerChest = Utils.GetCharacterHeightPosition(transform, Utils.UPPER_CHEST_HEIGHT);

            LayerMask enemyLayerMask = LayerMask.GetMask(Utils.ENEMY_LAYER_NAME);

            // Check if there are enemies nearby the player
            enemyNearby = Physics.OverlapSphere(playerChest, enemyNearbyRadius, enemyLayerMask, QueryTriggerInteraction.Ignore)
                                        .Select(collider => collider.gameObject.GetComponent<EnemyStateManager>())
                                        .Where(enemyStateManager => enemyStateManager.CurrentState is EnemyChasingPlayerState || enemyStateManager.CurrentState is EnemyAttackState)
                                        .Count() > 0;
        }


        public bool LookForObstacles(object direction = null)
        {
            bool LookForObstaclesTowardsDirection(Vector3 direction, float maxSlopeAngle)
            {
                LayerMask wallLayerMask = LayerMask.GetMask(Utils.GROUND_LAYER_NAME, Utils.OBSTACLES_LAYER_NAME, Utils.INVISIBLE_WALL_LAYER_NAME);

                Vector3 headPosition = Utils.GetCharacterHeightPosition(transform, Utils.HEAD_HEIGHT);
                Vector3 upperChestPosition = Utils.GetCharacterHeightPosition(transform, Utils.UPPER_CHEST_HEIGHT);
                Vector3 midHeightPosition = Utils.GetCharacterHeightPosition(transform, Utils.MID_HEIGHT);
                Vector3 legsHeightPosition = Utils.GetCharacterHeightPosition(transform, Utils.LEGS_HEIGHT);

                RaycastHit[] headObstacles = Utils.LookForObstacles(headPosition, direction, lookForObstacleDistance, wallLayerMask);
                RaycastHit[] upperChestObstacles = Utils.LookForObstacles(upperChestPosition, direction, lookForObstacleDistance, wallLayerMask);
                RaycastHit[] midHeightObstacles = Utils.LookForObstacles(midHeightPosition, direction, lookForObstacleDistance, wallLayerMask);
                RaycastHit[] legsHeightObstacles = Utils.LookForObstacles(legsHeightPosition, direction, lookForObstacleDistance, wallLayerMask, maxSlopeAngle);

                // DEBUG CODE HERE
                /*Debug.DrawRay(headPosition, movementDirection * lookForObstacleDistance, Color.red, 0.1f);
                Debug.DrawRay(chestPosition, movementDirection * lookForObstacleDistance, Color.red, 0.1f);
                Debug.DrawRay(upperLegsPosition, movementDirection * lookForObstacleDistance, Color.red, 0.1f);
                Debug.Log("LookForObstacles headObstacles = " + headObstacles);
                Debug.Log("LookForObstacles chestObstacles = " + chestObstacles);
                Debug.Log("LookForObstacles upperLegsObstacles = " + upperLegsObstacles);*/

                return headObstacles.Length > 0 || upperChestObstacles.Length > 0 || midHeightObstacles.Length > 0 || legsHeightObstacles.Length > 0;
            }
            
            Vector3 movementDirection;
            if (direction == null || direction is not Vector3 vector)
            {
                Vector2 playerMovementInput = inputHandler.MovementInput;
                movementDirection = Utils.ComputeInputDirectionRelativeToCamera(playerMovementInput);
            }
            else
            {
                movementDirection = Vector3.ProjectOnPlane(vector, Vector3.up).normalized;
            }

            CharacterController characterController = transform.GetComponent<CharacterController>();
            float maxSlopeAngle = characterController.slopeLimit;

            return LookForObstaclesTowardsDirection(movementDirection, maxSlopeAngle);
        }
        #endregion
        
        public override IEnumerator AddInstantForceCoroutine(Vector3 force, float forceDuration)
        {
            // Store the character controller center local value, in order to restore it later
            Vector3 characterControllerCenter = characterController.center;

            Rigidbody.isKinematic = false;
            Rigidbody.AddForce(force, ForceMode.Impulse);

            yield return new WaitForSeconds(forceDuration);

            Rigidbody.isKinematic = true;

            // Reset the character controller center to its initial local value
            // (otherwise the character will return where the force has been originally applied)
            characterController.center = characterControllerCenter;
        }

        public override void SaveStatusForNextScene() 
        {
            PlayerLevelSystemHandler palyerLevelSystemHandler = gameObject.GetComponent<PlayerLevelSystemHandler>();
            HealthHandler healthHandler = gameObject.GetComponent<HealthHandler>();
            ManaHandler manaHandler = gameObject.GetComponent<ManaHandler>();
            CurrenciesHandler currenciesHandler = gameObject.GetComponent<CurrenciesHandler>();
            PlayerConsumableHandler playerConsumableHandler = gameObject.GetComponent<PlayerConsumableHandler>();

            int healthToSave = IsDead ? healthHandler.MaxHealth : healthHandler.CurrentHealth;
            int manaToSave = IsDead ? manaHandler.MaxMana : manaHandler.CurrentMana;
            
            DataBetweenScenes.Instance.StoreData(DataBetweenScenes.PLAYER_CURRENT_LEVEL_KEY, palyerLevelSystemHandler.CurrentLevel);
            DataBetweenScenes.Instance.StoreData(DataBetweenScenes.PLAYER_CURRENT_EXP_KEY, palyerLevelSystemHandler.CurrentExp);
            DataBetweenScenes.Instance.StoreData(DataBetweenScenes.PLAYER_CURRENT_HEALTH_KEY, healthToSave);
            DataBetweenScenes.Instance.StoreData(DataBetweenScenes.PLAYER_CURRENT_MANA_KEY, manaToSave);
            DataBetweenScenes.Instance.StoreData(DataBetweenScenes.PLAYER_GOLD_KEY, currenciesHandler.GoldAmount);
            DataBetweenScenes.Instance.StoreData(DataBetweenScenes.PLAYER_CONSUMABLE_LIST_KEY, playerConsumableHandler.ConsumableList);
            DataBetweenScenes.Instance.StoreData(DataBetweenScenes.PLAYER_CONSUMABLE_INDEX_KEY, playerConsumableHandler.ConsumableSelectedIndex);
        }
    }
}

