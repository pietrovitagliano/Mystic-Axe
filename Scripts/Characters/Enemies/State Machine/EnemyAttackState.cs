// Author: Pietro Vitagliano

using DG.Tweening;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace MysticAxe
{
    public class EnemyAttackState : CharacterAliveState
    {        
        private EnemyAnimatorHandler enemyAnimatorHandler;
        private EnemyStatusHandler enemyStatusHandler;
        private EnemyLocomotionHandler enemyLocomotionHandler;
        private EnemyStateManager enemyStateManager;

        private float timeToWaitBeforeNextAttack;
        private float timeAfterAttack;
        private int attackLayerIndex;

        public override void InitializeState(CharacterStateManager characterStateManager)
        {
            base.InitializeState(characterStateManager);

            enemyStatusHandler = Character.GetComponent<EnemyStatusHandler>();
            enemyAnimatorHandler = Character.GetComponent<EnemyAnimatorHandler>();
            enemyLocomotionHandler = Character.GetComponent<EnemyLocomotionHandler>();
            enemyStateManager = characterStateManager as EnemyStateManager;

            enemyStatusHandler.OnHitByCharacterEvent.AddListener(OnCounterAttackEvent);

            attackLayerIndex = enemyAnimatorHandler.Animator.GetLayerIndex(Utils.ATTACK_LAYER_NAME);
        }
        
        protected override void AliveEnterState()
        {
            timeToWaitBeforeNextAttack = 0;
            timeAfterAttack = Time.time;
        }

        protected override void AliveUpdateState()
        {
            HandleRevolvesAroundPlayer();
            HandleAttackDecision();
            HandleAttackEnd();
            HandleChangingState();
        }

        private void HandleAttackDecision()
        {
            timeToWaitBeforeNextAttack = timeToWaitBeforeNextAttack > 0 ? timeToWaitBeforeNextAttack - Time.deltaTime : timeToWaitBeforeNextAttack;
            timeAfterAttack = enemyStatusHandler.WantsToAttack || enemyStatusHandler.IsAttacking ? Time.time : timeAfterAttack;
            float timeElapsedAfterLastAttack = Time.time - timeAfterAttack;

            // If the time elapsed after the last attack is greater than twice the maxTimeToAttack, the enemy will attack for sure
            if (timeElapsedAfterLastAttack >= enemyStateManager.MaxTimeToAttack)
            {
                enemyStatusHandler.WantsToAttack = true;
                ReachAndAttackPlayer();
            }
            // Otherwise, decide if attacking or not, based on probability
            else
            {
                bool canChooseToAttack = timeToWaitBeforeNextAttack <= 0 && !enemyStatusHandler.WantsToAttack && !enemyStatusHandler.IsAttacking;
                if (canChooseToAttack)
                {
                    float attackProbability = enemyStatusHandler.IsAttacking ? enemyStateManager.ContinueAttackProbability : enemyStateManager.StartAttackProbability;
                    enemyStatusHandler.WantsToAttack = Random.value <= attackProbability;

                    // If enemy doesn't want to attack, wait before making another decision
                    if (!enemyStatusHandler.WantsToAttack)
                    {
                        timeToWaitBeforeNextAttack = Random.Range(enemyStateManager.MinTimeToAttack, enemyStateManager.MaxTimeToAttack);
                    }
                    else
                    {
                        ReachAndAttackPlayer();
                    }
                }
            }
        }

        private async void ReachAndAttackPlayer()
        {
            timeToWaitBeforeNextAttack = 0;
            enemyStatusHandler.IsAttacking = false;

            while (!enemyStatusHandler.IsDead && enemyStatusHandler.WantsToAttack)
            {
                // If enemy wants to attack and hit animation is playing,
                // wait for it to end and then reach and attack the player
                while (enemyStatusHandler.IsHitAnimationOnGoing)
                {
                    await Task.Yield();
                }

                float distanceFromPlayer = Vector3.Distance(Character.position, enemyStatusHandler.Player.transform.position);

                // Player reached
                if (distanceFromPlayer <= ComputeAttackRange())
                {
                    int wallLayer = LayerMask.GetMask(Utils.GROUND_LAYER_NAME, Utils.OBSTACLES_LAYER_NAME);
                    Vector3 enemyChest = enemyAnimatorHandler.Animator.GetBoneTransform(HumanBodyBones.Chest).position;
                    Vector3 playerChest = enemyStatusHandler.Player.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Chest).position;

                    // Compute the angle between enemy forward and the enemy and the player
                    Vector3 enemyToPlayerDirection = (enemyStatusHandler.Player.transform.position - Character.position).normalized;
                    float angle = Vector3.Angle(Character.forward, enemyToPlayerDirection);

                    // Check if there is an obstacle between the enemy and the player and
                    // if the player is in front of the enemy. In this case, perform the attack
                    if (!Physics.Linecast(enemyChest, playerChest, wallLayer) && angle <= enemyStateManager.MaxAngleWithPlayerThreshold)
                    {
                        Attack();
                        
                        break;
                    }
                }

                enemyLocomotionHandler.NavMeshAgent.SetDestination(enemyStatusHandler.Player.transform.position);
                
                await Task.Yield();
            }
        }
        
        public void Attack()
        {
            enemyLocomotionHandler.NavMeshAgent.ResetPath();

            RotateTowardsPlayer(enemyStateManager.AttackRotationDuration);

            int comboChooser = Random.Range(1, enemyStateManager.ComboAvailable + 1);
            enemyStatusHandler.IsAttacking = true;

            enemyAnimatorHandler.Animator.SetInteger(enemyAnimatorHandler.ComboChooserHash, comboChooser);
        }

        private void RotateTowardsPlayer(float rotationDuration)
        {
            Transform player = enemyStatusHandler.Player.transform;

            Vector3 projectedAttackDirection = Vector3.ProjectOnPlane(player.position - Character.position, Vector3.up).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(projectedAttackDirection);

            Character.DORotateQuaternion(targetRotation, rotationDuration)
                    .OnComplete(() =>
                    {
                        Character.LookAt(player);
                    });
        }

        private void HandleAttackEnd()
        {
            bool isComboEnded = enemyAnimatorHandler.Animator.GetNextAnimatorStateInfo(attackLayerIndex).IsName(Utils.emptyAnimationStateName);
            bool stopAttackingCondition = !enemyStatusHandler.IsDead && (isComboEnded || (!enemyAnimatorHandler.Animator.GetCurrentAnimatorStateInfo(attackLayerIndex).IsName(Utils.emptyAnimationStateName) &&
                                                                                            enemyAnimatorHandler.Animator.GetCurrentAnimatorStateInfo(attackLayerIndex).normalizedTime >= 1f));

            enemyStateManager.ConsecutiveComboPerformed = isComboEnded ? 0 : enemyStateManager.ConsecutiveComboPerformed;

            if (stopAttackingCondition)
            {
                enemyStatusHandler.IsAttacking = false;
                enemyAnimatorHandler.Animator.SetInteger(enemyAnimatorHandler.ComboChooserHash, 0);

                timeToWaitBeforeNextAttack = Random.Range(enemyStateManager.MinTimeToAttack, enemyStateManager.MaxTimeToAttack);
            }
        }
        
        private void OnCounterAttackEvent(GameObject damageApplier)
        {
            if (enemyStateManager.CurrentState is EnemyAttackState && !enemyStatusHandler.IsDead && !enemyStatusHandler.WantsToAttack && damageApplier == enemyStatusHandler.Player)
            {
                // If is attacking and a hit is taken, continue to attack, otherwise choose if attack or not based on probability
                enemyStatusHandler.WantsToAttack = enemyStatusHandler.IsAttacking || Random.value <= enemyStateManager.CounterAttackProbability;

                if (enemyStatusHandler.WantsToAttack)
                {
                    ReachAndAttackPlayer();
                }
            }
        }

        private void HandleRevolvesAroundPlayer()
        {
            float destinationFromPlayerDistance = Vector3.Distance(enemyStatusHandler.Player.transform.position, enemyLocomotionHandler.NavMeshAgent.destination);
            bool distanceCondition = enemyLocomotionHandler.NavMeshAgent.remainingDistance <= enemyLocomotionHandler.NavMeshAgent.stoppingDistance ||
                                    destinationFromPlayerDistance < enemyStateManager.MinDistanceAfterAttack;

            // If enemy still has to wait before next attack and destination has been reached or player is too close, revolve around player
            if (timeToWaitBeforeNextAttack > 0 && distanceCondition)
            {
                Vector3 destination = ComputeWayPointOnCircularTrajectory();
                enemyLocomotionHandler.NavMeshAgent.SetDestination(destination);
            }
        }
        
        private Vector3 ComputeWayPointOnCircularTrajectory()
        {
            Vector3 ComputeRadiusMagnitude(Vector3 center)
            {
                float radiusMagnitude = Vector3.Distance(Character.position, center);

                if (radiusMagnitude < enemyStateManager.MinDistanceAfterAttack)
                {
                    radiusMagnitude = Random.Range(enemyStateManager.MinDistanceAfterAttack, enemyStateManager.MaxDistanceAfterAttack);
                }

                Vector3 radiusDirection = (Character.position - center).normalized;

                return radiusMagnitude * radiusDirection;
            }
            
            Vector3 circleCenter = enemyStatusHandler.Player.transform.position;
            circleCenter.y = Character.position.y;

            Vector3 circleRadius = ComputeRadiusMagnitude(circleCenter);

            int casualSign = Random.Range(0, 2) == 0 ? -1 : 1;

            Vector3 targetPosition = circleCenter + Quaternion.Euler(0, casualSign * 45f, 0) * circleRadius;
            targetPosition.y = Character.position.y;
            targetPosition = ComputeNearestReachablePosition(targetPosition);
            targetPosition.y = Character.position.y;

            return targetPosition;
        }

        private void HandleChangingState()
        {
            // If the player is dead, return to patrolling state
            if (enemyStatusHandler.PlayerStatusHandler.IsDead)
            {
                enemyStateManager.ChangeState(enemyStateManager.PatrollingState);
            }
            // Otherwise, if player is too far, switch to chasing state
            else
            {
                // Compute the distance between the enemy and the player
                float distanceFromPlayer = Vector3.Distance(Character.position, enemyStatusHandler.Player.transform.position);

                // Player is too far from enemy
                if (distanceFromPlayer > enemyStateManager.ChasingPlayerState.MaxDistanceToChasePlayer)
                {
                    enemyStateManager.ChangeState(enemyStateManager.ChasingPlayerState);
                }
            }
        }

        private Vector3 ComputeNearestReachablePosition(Vector3 destination)
        {
            Vector3 playerPosition = enemyStatusHandler.Player.transform.position;

            if (destination == playerPosition)
            {
                Vector3 playerToEnemyDirection = (Character.position - playerPosition).normalized;

                destination = playerPosition + ComputeAttackRange() * playerToEnemyDirection;
            }

            if (!enemyLocomotionHandler.IsPathAvailable(destination))
            {
                int walkableArea = NavMesh.GetAreaFromName(Utils.NAVMESH_WALKABLE_AREA_NAME);

                if (NavMesh.FindClosestEdge(destination, out NavMeshHit hit, walkableArea))
                {
                    destination = hit.position;
                }
            }
            
            return destination;
        }

        public float ComputePlayerEnemyCollidersMinDistance()
        {
            float enemyColliderScaledRadius = Character.GetComponent<CapsuleCollider>().radius * Character.localScale.x;
            float playerColliderScaledRadius = enemyStatusHandler.Player.GetComponent<CharacterController>().radius * enemyStatusHandler.Player.transform.localScale.x;

            return enemyColliderScaledRadius + playerColliderScaledRadius;
        }
        
        public float ComputeAttackRange()
        {
            float enemyRange = enemyStatusHandler.GetFeature(Utils.RANGE_FEATURE_NAME).CurrentValue;
            float collidersMinDistance = ComputePlayerEnemyCollidersMinDistance();

            return enemyRange + collidersMinDistance;
        }
    }
}