// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    public class EnemyChasingPlayerState : CharacterAliveState
    {
        private EnemyStatusHandler enemyStatusHandler;
        private EnemyLocomotionHandler enemyLocomotionHandler;
        private EnemyStateManager enemyStateManager;

        private float maxDistanceToChasePlayer;

        public float MaxDistanceToChasePlayer { get => maxDistanceToChasePlayer; }

        public override void InitializeState(CharacterStateManager characterStateManager)
        {
            base.InitializeState(characterStateManager);
            
            enemyStatusHandler = Character.GetComponent<EnemyStatusHandler>();
            enemyLocomotionHandler = Character.GetComponent<EnemyLocomotionHandler>();
            enemyStateManager = characterStateManager as EnemyStateManager;

            UpdateChasingDistance();
        }

        protected override void AliveEnterState()
        {
            UpdateChasingDistance();
        }

        protected override void AliveUpdateState()
        {
            UpdateChasingDistance();
            HandleChasing();
            HandleChangingState();
        }

        private void HandleChasing()
        {
            if (enemyStatusHandler.IsPlayerTargeted)
            {
                bool playerTooFar = enemyLocomotionHandler.NavMeshAgent.remainingDistance > maxDistanceToChasePlayer;

                // Chase the player
                if (playerTooFar)
                {
                    enemyLocomotionHandler.NavMeshAgent.SetDestination(enemyStatusHandler.Player.transform.position);
                }
                // Switch to the attack state
                else
                {
                    enemyLocomotionHandler.NavMeshAgent.ResetPath();
                    SwitchToAttackState();
                }
            }
        }

        private void HandleChangingState()
        {
            if (!enemyStatusHandler.IsPlayerTargeted && enemyStatusHandler.PlayerLastPosition != null && enemyStatusHandler.PlayerLastPosition is Vector3 playerLastPosition)
            {
                // If no path is available or the enemy has reached the last known position of the player, return to patrolling state
                if (!enemyLocomotionHandler.IsPathAvailable(playerLastPosition) ||
                    enemyLocomotionHandler.NavMeshAgent.remainingDistance <= enemyLocomotionHandler.NavMeshAgent.stoppingDistance)
                {
                    enemyStatusHandler.PlayerLastPosition = null;
                }
                else
                {
                    enemyLocomotionHandler.NavMeshAgent.SetDestination(playerLastPosition);
                }
            }

            // PlayerLastPosition is null when player is dead or PlayerLastPosition has been reached
            if (enemyStatusHandler.PlayerLastPosition == null)
            {
                enemyLocomotionHandler.NavMeshAgent.ResetPath();
                enemyStateManager.ChangeState(enemyStateManager.PatrollingState);
            }
        }

        private void SwitchToAttackState()
        {
            // Check if enemy has a weapon. If so, switch to the attack state
            bool isEnemyEquipped = Utils.FindGameObjectInTransformWithTag(Character, Utils.WEAPON_HOLDER_TAG)
                                        .GetComponentInChildren<WeaponStatusHandler>() != null;
            if (isEnemyEquipped)
            {
                enemyStateManager.ChangeState(enemyStateManager.AttackState);
            }
        }

        private void UpdateChasingDistance()
        {
            // The enemy range is reduced with enemyStatusHandler.RangeFactor,
            // since if enemy has a ranged weapon (bow, crossbow, etc), he has to come closer than his max range.
            // In this way, if player will slightly move, enemy will continue to attack from distance and
            // won't stop to attack in order to come closer
            float enemyReducedRange = enemyStateManager.RangeFactor * enemyStatusHandler.GetFeature(Utils.RANGE_FEATURE_NAME).CurrentValue;

            maxDistanceToChasePlayer = Mathf.Max(enemyStateManager.MinChasingDistance, enemyReducedRange);
        }
    }
}