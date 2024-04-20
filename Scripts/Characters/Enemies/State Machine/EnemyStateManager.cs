// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    public class EnemyStateManager : CharacterStateManager
    {
        private EnemyPatrollingState patrollingState = new EnemyPatrollingState();
        private EnemyChasingPlayerState chasingPlayerState = new EnemyChasingPlayerState();
        private EnemyAttackState attackState = new EnemyAttackState();

        private EnemyStatusHandler enemyStatusHandler;
        private PlayerStatusHandler playerStatusHandler;
        private EnemyAnimatorHandler enemyAnimatorHandler;

        [Header("Patrolling State Settings")]
        [SerializeField, Range(2, 5)] private int minWayPointsNumber = 3;
        [SerializeField, Range(5, 10)] private int maxWayPointsNumber = 6;
        [SerializeField, Range(1, 10)] private float minDistanceBetweenWayPoints = 6;
        [SerializeField, Range(10, 20)] private float maxDistanceBetweenWayPoints = 15;
        [SerializeField, Range(0, 180)] private float lookForNextPointMaxAngle = 90;
        [SerializeField, Range(0.1f, 3f)] private float wallOffset = 1.5f;
        [SerializeField, Range(0.1f, 3f)] private float heightOffsetToAvoidObstacles = 1f;
        [SerializeField, Range(0.1f, 5)] private float timeToWaitBeforeStartPatrolling = 2.5f;
        [SerializeField, Range(0.1f, 5)] private float timeToWaitToReachNextWayPoint = 1.7f;

        [Header("Chasing State Settings")]
        [SerializeField, Range(1f, 20)] private float minChasingDistance = 5f;

        [Header("Attack State Settings")]
        [SerializeField, Range(0f, 360f)] private float maxAngleWithPlayerThreshold = 6f;
        [SerializeField, Range(0.1f, 1f)] private float attackRotationDuration = 0.25f;
        [SerializeField, Range(0.1f, 1)] private float startAttackProbability = 0.6f;
        [SerializeField, Range(0.1f, 1)] private float counterAttackProbability = 0.8f;
        [SerializeField, Range(0.1f, 1)] private float continueAttackProbability = 0.75f;
        [SerializeField, Range(1, 6)] private int comboAvailable = 2;
        [SerializeField, Range(1, 6)] private int maxConsecutiveCombo = 3;
        [SerializeField, Range(0.1f, 1)] private float rangeFactor = 0.75f;
        [SerializeField, Range(1f, 5)] private float minTimeToAttack = 1.5f;
        [SerializeField, Range(1f, 10)] private float maxTimeToAttack = 5f;
        [SerializeField, Range(1f, 10)] private float minDistanceAfterAttack = 3f;
        [SerializeField, Range(1f, 20)] private float maxDistanceAfterAttack = 5f;
        private int consecutiveComboPerformed = 0;


        public EnemyPatrollingState PatrollingState { get => patrollingState;  }
        public EnemyChasingPlayerState ChasingPlayerState { get => chasingPlayerState; }
        public EnemyAttackState AttackState { get => attackState; }
        public int MinWayPointsNumber { get => minWayPointsNumber; }
        public int MaxWayPointsNumber { get => maxWayPointsNumber; }
        public float MinDistanceBetweenWayPoints { get => minDistanceBetweenWayPoints; }
        public float MaxDistanceBetweenWayPoints { get => maxDistanceBetweenWayPoints; }
        public float LookForNextPointMaxAngle { get => lookForNextPointMaxAngle; }
        public float WallOffset { get => wallOffset; }
        public float HeightOffsetToAvoidObstacles { get => heightOffsetToAvoidObstacles; }
        public float TimeToWaitBeforeStartPatrolling { get => timeToWaitBeforeStartPatrolling; }
        public float TimeToWaitToReachNextWayPoint { get => timeToWaitToReachNextWayPoint; }
        public float MinChasingDistance { get => minChasingDistance; }
        public float MaxAngleWithPlayerThreshold { get => maxAngleWithPlayerThreshold; }
        public float AttackRotationDuration { get => attackRotationDuration; }
        public float StartAttackProbability { get => startAttackProbability; }
        public float CounterAttackProbability { get => counterAttackProbability; }
        public float ContinueAttackProbability { get => continueAttackProbability; }
        public int ComboAvailable { get => comboAvailable; }
        public float RangeFactor { get => rangeFactor; }
        public float MinTimeToAttack { get => minTimeToAttack; }
        public float MaxTimeToAttack { get => maxTimeToAttack; }
        public float MinDistanceAfterAttack { get => minDistanceAfterAttack; }
        public float MaxDistanceAfterAttack { get => maxDistanceAfterAttack; }
        public int ConsecutiveComboPerformed { get => consecutiveComboPerformed; set => consecutiveComboPerformed = value; }

        
        protected override void InitializeStateManager()
        {
            base.InitializeStateManager();
            
            patrollingState.InitializeState(this);
            chasingPlayerState.InitializeState(this);
            attackState.InitializeState(this);

            enemyStatusHandler = GetComponent<EnemyStatusHandler>();
            playerStatusHandler = enemyStatusHandler.Player.GetComponent<PlayerStatusHandler>();
            enemyAnimatorHandler = GetComponent<EnemyAnimatorHandler>();
        }

        protected override void EnterFirstState()
        {
            ChangeState(patrollingState);
        }

        /// <summary>
        /// Called by the animator when the enemy is performing an attack animation (not the last one of a combo)
        /// </summary>
        public void OnChooseIfAttackGoesOn()
        {
            if (!enemyStatusHandler.IsDead && !playerStatusHandler.IsDead)
            {
                float distanceFromPlayer = Vector3.Distance(transform.position, enemyStatusHandler.Player.transform.position);
                float attackRange = AttackState.ComputeAttackRange();

                if (distanceFromPlayer <= attackRange)
                {
                    enemyStatusHandler.WantsToAttack = Random.value <= continueAttackProbability && CurrentState is EnemyAttackState;

                    if (enemyStatusHandler.WantsToAttack)
                    {
                        AttackState.Attack();
                        enemyAnimatorHandler.Animator.SetTrigger(enemyAnimatorHandler.ContinueToAttackHash);
                    }
                }
            }
        }

        /// <summary>
        /// Called by the animator when the enemy is performing the last attack of a combo
        /// </summary>
        public void OnChooseIfStartingNewAttack()
        {
            // Since it's the last attack of a combo, it means that a combo has just been performed
            consecutiveComboPerformed++;

            // If the enemy has not performed the maximum number of consecutive combos,
            // he will decide if he wants to start a new attack or not
            if (consecutiveComboPerformed < maxConsecutiveCombo && !enemyStatusHandler.IsDead && !playerStatusHandler.IsDead)
            {
                float distanceFromPlayer = Vector3.Distance(transform.position, enemyStatusHandler.Player.transform.position);
                float attackRange = AttackState.ComputeAttackRange();

                if (distanceFromPlayer <= attackRange)
                {
                    enemyStatusHandler.WantsToAttack = Random.value <= startAttackProbability && CurrentState is EnemyAttackState;

                    if (enemyStatusHandler.WantsToAttack)
                    {
                        AttackState.Attack();
                        enemyAnimatorHandler.Animator.SetTrigger(enemyAnimatorHandler.ContinueToAttackHash);
                    }
                }
            }
        }
    }
}
