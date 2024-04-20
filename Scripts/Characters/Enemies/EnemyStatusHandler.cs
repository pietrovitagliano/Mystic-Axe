// Author: Pietro Vitagliano

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    [RequireComponent(typeof(EnemyAnimatorHandler))]
    [RequireComponent(typeof(EnemyStateManager))]
    [RequireComponent(typeof(EnemyLocomotionHandler))]
    public class EnemyStatusHandler : CharacterStatusHandler
    {
        private EnemyStateManager enemyStateManager;
        private EnemyLocomotionHandler enemyLocomotionHandler;
        private EnemyAnimatorHandler enemyAnimatorHandler;
        private WeaponStatusHandler weaponStatusHandler;

        [Header("Find Player Settings")]
        [SerializeField, Range(0.1f, 5)] private float playerPositionAlwaysKnownDuration = 3.5f;
        private GameObject player;
        private PlayerStatusHandler playerStatusHandler;
        private bool isPlayerTargeted;
        private bool playerAlwaysTargeted;
        private Coroutine playerAlwaysTargetedCoroutine;

        [Header("Give Exp Settings")]
        [SerializeField] private int baseGivenExp = 150;
        [SerializeField, Range(0.1f, 3f)] private float giveExpDelay = 1f;

        [Header("Reward Settings")]
        [SerializeField] private GameObject droppedRewardPrefab;
        [SerializeField] private Reward reward;
        private bool rewardReleased = false;

        // playerLastPositionKnown is a Vector3, but when the player has been completely lost
        // it's necessary to put it to null.
        // Since a Vector3 can't be null, an object is used instead.
        private object playerLastPosition = null;

        public GameObject Player { get => player; }
        public PlayerStatusHandler PlayerStatusHandler { get => playerStatusHandler; }
        public bool IsPlayerTargeted { get => isPlayerTargeted; }
        public object PlayerLastPosition { get => playerLastPosition; set => playerLastPosition = value; }
        public Reward Reward { get => reward; }

        protected override List<Feature> InitializeFeatures()
        {
            return JsonDatabase.Instance.GetDataFromJson<FeaturesJsonMap>(Utils.ENEMY_FEATURES_JSON_NAME).Features;
        }

        protected override void Start()
        {
            enemyStateManager = GetComponent<EnemyStateManager>();
            enemyLocomotionHandler = GetComponent<EnemyLocomotionHandler>();
            enemyAnimatorHandler = GetComponent<EnemyAnimatorHandler>();
            weaponStatusHandler = Utils.FindGameObjectInTransformWithTag(transform, Utils.WEAPON_HOLDER_TAG)
                                    .GetComponentInChildren<WeaponStatusHandler>();

            UpdatePlayerReference();
            
            OnHitByCharacterEvent.AddListener(OnHitByCharacter);

            reward = reward.InitializeReward();

            base.Start();
        }

        protected override void UpdateStatus()
        {
            IsDeadCheck();
            UpdatePlayerReference();
            MoveCheck();
            RotationCheck();
            TargetPlayerCheck();
            ActivateAzazelStrenghtCheck();
            PlayerLastPositionCheck();
            AttackCheck();
            IsDoingHeavyAttackCheck();
            HitCheck();
            InCombatCheck();
            HandleWeaponWhooshes();
        }

        private void UpdatePlayerReference()
        {
            if (!IsDead && player == null)
            {
                player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);

                if (player != null)
                {
                    playerStatusHandler = player.GetComponent<PlayerStatusHandler>();
                }
            }
        }

        private void PlayerLastPositionCheck()
        {
            if (playerStatusHandler.IsDead)
            {
                playerLastPosition = null;
            }
            else if (isPlayerTargeted)
            {
                playerLastPosition = player.transform.position;
            }
        }

        protected override void IsDeadCheck()
        {
            async void AddExpToPlayer()
            {
                await Utils.AsyncWaitTimeScaled(giveExpDelay);

                player.GetComponent<PlayerLevelSystemHandler>().AddExp(LevelSystemHandler.CurrentLevel * baseGivenExp);
            }

            void ReleaseReward()
            {
                // Instantiate the reward gameObject
                Vector3 droppedRewardOffset = droppedRewardPrefab.transform.localScale.y * 0.5f * Vector3.up;
                GameObject droppedReward = Instantiate(droppedRewardPrefab, transform.position + droppedRewardOffset, Quaternion.identity);

                // Assign the reward to the instantiated gameObject
                DroppedRewardInteraction droppedRewardInteraction = droppedReward.GetComponent<DroppedRewardInteraction>();
                droppedRewardInteraction.Reward = reward;
            }


            base.IsDeadCheck();

            if (IsDead)
            {
                enemyLocomotionHandler.NavMeshAgent.ResetPath();

                if (!rewardReleased)
                {
                    rewardReleased = true;

                    // Add exp to player
                    AddExpToPlayer();

                    if (reward != null)
                    {
                        // Release the reward on the ground
                        ReleaseReward();

                        // After it was released, the reward is set to null since enemy hasn't it anymore
                        reward = null;
                    }
                }
            }
        }

        protected override void InCombatCheck()
        {
            InCombat = !IsDead && isPlayerTargeted;
            enemyAnimatorHandler.Animator.SetBool(enemyAnimatorHandler.InCombatHash, InCombat);
        }

        protected override void MoveCheck()
        {
            CanMove = !IsDead && Rigidbody.isKinematic && !IsAttacking && !IsCastingSkill &&
                        !IsHitAnimationOnGoing && enemyLocomotionHandler.NavMeshAgent != null && enemyLocomotionHandler.NavMeshAgent.hasPath;
        }

        protected override void RotationCheck()
        {
            CanRotateTransform = !IsDead && !IsAttacking && !IsCastingSkill &&
                                !IsHitAnimationOnGoing && enemyLocomotionHandler.NavMeshAgent != null && enemyLocomotionHandler.NavMeshAgent.speed > 0;
        }

        private IEnumerator PlayerAlwaysTargetedCoroutine()
        {
            playerAlwaysTargeted = true;
            yield return new WaitForSeconds(playerPositionAlwaysKnownDuration);
            playerAlwaysTargeted = false;
        }

        protected override void AttackCheck()
        {
            bool isInAttackState = enemyStateManager.CurrentState is EnemyAttackState;
            if (WantsToAttack)
            {
                WantsToAttack = isInAttackState && !IsAttacking;
            }

            if (IsAttacking)
            {
                IsAttacking = isInAttackState && !IsHitAnimationOnGoing;
            }

            enemyAnimatorHandler.Animator.SetBool(enemyAnimatorHandler.IsAttackingHash, IsAttacking);
        }

        private void TargetPlayerCheck()
        {
            if (IsDead || playerStatusHandler.IsDead)
            {
                isPlayerTargeted = false;
                return;
            }
            
            if (playerAlwaysTargeted)
            {
                isPlayerTargeted = true;
                return;
            }

            // Perform the distance check first, since the raycast check, for evety single enemy, is more expensive
            float viewDistance = GetFeature(Utils.VIEW_DISTANCE_FEATURE_NAME).CurrentValue;
            if (Vector3.Distance(transform.position, player.transform.position) > viewDistance)
            {
                isPlayerTargeted = false;
                return;
            }
            else
            {
                // Calculate the angle between the forward direction of the enemy and the direction to the player
                Vector3 enemyToPlayerDirection = (player.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, enemyToPlayerDirection);

                if (!isPlayerTargeted)
                {
                    float enemyFOV = GetFeature(Utils.FIELD_OF_VIEW_FEATURE_NAME).CurrentValue;
                    isPlayerTargeted = !IsPlayerViewObstructedByObstacle() && angle <= enemyFOV / 2;
                }
                else
                {
                    isPlayerTargeted = !IsPlayerViewObstructedByObstacle();
                }
            }
        }

        private bool IsPlayerViewObstructedByObstacle()
        {
            int wallLayer = LayerMask.GetMask(Utils.GROUND_LAYER_NAME, Utils.OBSTACLES_LAYER_NAME);
            Vector3 enemyHead = enemyAnimatorHandler.Animator.GetBoneTransform(HumanBodyBones.Head).position;
            Animator playerAnimator = player.GetComponent<Animator>();

            // This is done to check if at least one of the player's bones is perfectly visible and not obstructed by an obstacle
            HumanBodyBones[] visibleBones = Enum.GetValues(typeof(HumanBodyBones))
                                                .Cast<HumanBodyBones>()
                                                .Where(bone => bone != HumanBodyBones.LastBone && playerAnimator.GetBoneTransform(bone) != null &&
                                                                !Physics.Linecast(enemyHead, playerAnimator.GetBoneTransform(bone).position, wallLayer))
                                                .ToArray();

            // The player view is obstructed when no bone is visible,
            // that is when the array of visible bones is empty
            return visibleBones.Length == 0;
        }

        protected override void IsDoingHeavyAttackCheck()
        {
            IsDoingHeavyAttack = false;
        }

        private void ActivateAzazelStrenghtCheck()
        {
            if (enemyStateManager.CurrentState is EnemyAttackState && GetModifierByID(Utils.AZAZEL_STRENGTH_MODIFIER_ID) == null)
            {
                // Get the modifier
                ModifiersJsonMap modifiersJsonMap = JsonDatabase.Instance.GetDataFromJson<ModifiersJsonMap>(Utils.MODIFIERS_JSON_NAME);
                Modifier azazelStrengthModifier = modifiersJsonMap.GetModifierByID(Utils.AZAZEL_STRENGTH_MODIFIER_ID);

                // Apply the modifier
                AddModifier(azazelStrengthModifier);
            }
        }

        private void OnPlayWeaponMeleeWhoosh()
        {
            if (IsAttacking)
            {
                weaponStatusHandler.PlayMeleeLightWhoosh();
            }
        }

        private void OnHitByCharacter(GameObject damageApplier)
        {
            if (!IsDead && damageApplier == player)
            {
                // The enemy notify player presence to himself
                NotifyPlayerPresence();
                
                // Get the enemy alert distance
                float enemyAlertDistance = GetFeature(Utils.ALERT_DISTANCE_FEATURE_NAME).CurrentValue;

                // Get the enemy layer mask
                LayerMask enemyLayerMask = LayerMask.GetMask(Utils.ENEMY_LAYER_NAME);

                // The enemy notify player presence to other enemies (active and near enough)
                FindObjectsOfType<EnemyStatusHandler>(includeInactive: false)
                    .Where(otherEnemyStatusHandler => !otherEnemyStatusHandler.IsDead && otherEnemyStatusHandler.gameObject != gameObject)
                    .ToList()
                    .ForEach(otherEnemyStatusHandler =>
                    {
                        // Get the enemy alert distance
                        float enemyAlertDistance = GetFeature(Utils.ALERT_DISTANCE_FEATURE_NAME).CurrentValue;

                        // Compute the distance from the enemy who has been hit
                        float distanceFromHitEnemy = Vector3.Distance(transform.position, otherEnemyStatusHandler.transform.position);

                        // If an enemy is near enough, notify player presence to him
                        if (distanceFromHitEnemy <= enemyAlertDistance)
                        {
                            otherEnemyStatusHandler.NotifyPlayerPresence();
                        }
                    });
            }
        }

        public void NotifyPlayerPresence()
        {
            if (playerAlwaysTargetedCoroutine != null)
            {
                StopCoroutine(playerAlwaysTargetedCoroutine);
            }

            isPlayerTargeted = true;
            playerAlwaysTargetedCoroutine = StartCoroutine(PlayerAlwaysTargetedCoroutine());
        }

        private void HandleWeaponWhooshes()
        {
            if (!IsAttacking)
            {
                weaponStatusHandler.StopMeleeLightWhoosh();
                weaponStatusHandler.StopMeleeHeavyWhoosh();
            }
        }

        public override void SaveStatusForNextScene() { }

        public override IEnumerator AddInstantForceCoroutine(Vector3 force, float forceDuration)
        {
            Rigidbody.isKinematic = false;
            Rigidbody.AddForce(force, ForceMode.Impulse);

            yield return new WaitForSeconds(forceDuration);

            Rigidbody.isKinematic = true;
        }
    }
}
