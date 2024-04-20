// Author: Pietro Vitagliano

using UnityEngine;
using UnityEngine.AI;

namespace MysticAxe
{
    [RequireComponent(typeof(EnemyAnimatorHandler))]
    [RequireComponent(typeof(EnemyStatusHandler))]
    [RequireComponent(typeof(EnemyStateManager))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyLocomotionHandler : CharacterLocomotionHandler
    {
        private EnemyStateManager enemyStateManager;
        private EnemyStatusHandler enemyStatusHandler;
        private EnemyAnimatorHandler enemyAnimatorHandler;
        private NavMeshAgent navMeshAgent;

        public NavMeshAgent NavMeshAgent { get => navMeshAgent; }

        private void Start()
        {            
            enemyStateManager = GetComponent<EnemyStateManager>();
            enemyStatusHandler = GetComponent<EnemyStatusHandler>();
            enemyAnimatorHandler = GetComponent<EnemyAnimatorHandler>();
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        protected override void Update()
        {
            base.Update();
        }

        protected override void HandleSpeed()
        {
            UpdateAnimatorMovementParams();
            
            float targetSpeed = ComputeTargetSpeed();
            float acceleration = ComputeAccelaration(targetSpeed);

            enemyStatusHandler.Speed = Utils.FloatInterpolation(enemyStatusHandler.Speed, targetSpeed, acceleration * Time.deltaTime);
            enemyAnimatorHandler.Animator.SetFloat(enemyAnimatorHandler.SpeedHash, enemyStatusHandler.Speed);

            float currentSpeedFactor = enemyStatusHandler.GetFeature(Utils.SPEED_FEATURE_NAME).CurrentValue;
            navMeshAgent.speed = currentSpeedFactor * enemyStatusHandler.Speed;

            // Decrease speed taking into account the magnitude of the movement
            Vector2 movement = new Vector2(enemyAnimatorHandler.Animator.GetFloat(enemyAnimatorHandler.MovementXHash),
                                            enemyAnimatorHandler.Animator.GetFloat(enemyAnimatorHandler.MovementYHash));

            if (movement.magnitude > 0)
            {
                navMeshAgent.speed *= Mathf.Clamp01(movement.magnitude);
            }
        }

        protected override void UpdateAnimatorMovementParams()
        {
            float currentMovementX = enemyAnimatorHandler.Animator.GetFloat(enemyAnimatorHandler.MovementXHash);
            float currentMovementY = enemyAnimatorHandler.Animator.GetFloat(enemyAnimatorHandler.MovementYHash);

            float newMovementX = 0, newMovementY = 0;
            if (navMeshAgent.hasPath || enemyStatusHandler.Speed > 0)
            {
                if (!enemyStatusHandler.InCombat)
                {
                    newMovementX = 0;
                    newMovementY = 1;
                }
                else
                {
                    Vector3 velocityDirection = Vector3.ProjectOnPlane(navMeshAgent.desiredVelocity, Vector3.up).normalized;

                    // With the dot product is possible to compute the magnitude of velocity's projections
                    // along transform.right and transform.forward
                    newMovementX = Vector3.Dot(transform.right, velocityDirection);
                    newMovementY = Vector3.Dot(transform.forward, velocityDirection);

                    Debug.DrawRay(transform.position, velocityDirection * 3, Color.green, 0.1f);
                    Debug.DrawRay(transform.position, newMovementX * transform.right * 3, Color.red, 0.1f);
                    Debug.DrawRay(transform.position, newMovementY * transform.forward * 3, Color.blue, 0.1f);

                    newMovementX = Mathf.Clamp(newMovementX, -1, 1);
                    newMovementY = Mathf.Clamp(newMovementY, -1, 1);
                }
            }

            currentMovementX = Utils.FloatInterpolation(currentMovementX, newMovementX, Time.deltaTime / MovementLerpTime);
            currentMovementY = Utils.FloatInterpolation(currentMovementY, newMovementY, Time.deltaTime / MovementLerpTime);

            enemyAnimatorHandler.Animator.SetFloat(enemyAnimatorHandler.MovementXHash, currentMovementX);
            enemyAnimatorHandler.Animator.SetFloat(enemyAnimatorHandler.MovementYHash, currentMovementY);
        }

        private float ComputeTargetSpeed()
        {
            float speed = 0;
            if (enemyStatusHandler.CanMove)
            {
                bool walkSpeedCondition = enemyStateManager.CurrentState is EnemyPatrollingState;

                bool targetingSpeedCondition = enemyStateManager.CurrentState is EnemyAttackState && !enemyStatusHandler.WantsToAttack;
                
                bool runSpeedCondition = enemyStateManager.CurrentState is EnemyChasingPlayerState ||
                                    (enemyStateManager.CurrentState is EnemyAttackState && enemyStatusHandler.WantsToAttack);

                if (walkSpeedCondition)
                {
                    speed = WalkSpeed;
                }
                else if (targetingSpeedCondition)
                {
                    speed = TargetingSpeed;
                }
                else if (runSpeedCondition)
                {
                    speed = BaseRunSpeed;
                }
            }

            return speed;
        }

        protected override void HandleRotation()
        {
            if (enemyStatusHandler.CanRotateTransform)
            {
                Vector3 rotationDirection;

                // If player is targeted look at him
                if (enemyStatusHandler.IsPlayerTargeted)
                {
                    rotationDirection = (enemyStatusHandler.Player.transform.position - transform.position).normalized;
                }
                else
                {
                    // If enemy is reaching a destination, he looks at the direction of his velocity along the path.
                    // Otherwise, if the destination has been reached, look at forward.
                    if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
                    {
                        rotationDirection = navMeshAgent.desiredVelocity.normalized;
                    }
                    else
                    {
                        rotationDirection = transform.forward;
                    }
                }

                Utils.RotateTransformTowardsDirection(transform, rotationDirection, RotationSpeed);
            }
        }
        
        private float ComputeAccelaration(float targetSpeed)
        {
            float acceleration;            
            if (targetSpeed == 0)
            {
                acceleration = BaseRunSpeed / StopMovingTime;

            }
            else if (targetSpeed == WalkSpeed)
            {
                acceleration = WalkSpeed / StartMovingTime;
            }
            else if (targetSpeed == TargetingSpeed)
            {
                acceleration = TargetingSpeed / StartMovingTime;
            }
            else
            {
                acceleration = BaseRunSpeed / StartMovingTime;
            }

            return acceleration;
        }

        public bool IsPathAvailable(Vector3 destination)
        {
            NavMeshPath path = new NavMeshPath();
            navMeshAgent.CalculatePath(destination, path);

            return path.status == NavMeshPathStatus.PathComplete;
        }

        protected override void OnPlayWalkSound()
        {
            bool walkSoundCondition = !enemyStatusHandler.IsAttacking && !enemyStatusHandler.IsCastingSkill;

            if (walkSoundCondition)
            {
                AudioManager.Instance.PlaySound(Utils.enemyWalkAudioName, gameObject);
            }
        }

        protected override void OnPlayRunSound()
        {
            bool runSoundCondition = !enemyStatusHandler.IsAttacking && !enemyStatusHandler.IsCastingSkill;

            if (runSoundCondition)
            {
                AudioManager.Instance.PlaySound(Utils.enemyRunAudioName, gameObject);
            }
        }

        protected override void OnPlayLightLandingSound() { }

        protected override void OnPlayHeavyLandingSound()
        {
            AudioManager.Instance.PlaySound(Utils.enemyHeavyLandingAudioName, gameObject);
        }
    }
}