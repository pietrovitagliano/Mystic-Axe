// Author: Pietro Vitagliano

using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace MysticAxe
{
    public class EnemyPatrollingState : CharacterAliveState
    {
        private EnemyLocomotionHandler enemyLocomotionHandler;
        private EnemyStatusHandler enemyStatusHandler;
        private EnemyStateManager enemyStateManager;

        private float timeToWaitBeforeStartPatrolling, timeToWaitToReachNextWayPoint;
        private bool notWalkableWallReached;
        private Vector3[] wayPoints;
        private int wayPointsNumber;
        private int wayPointIndex = 0;

        public override void InitializeState(CharacterStateManager characterStateManager)
        {
            base.InitializeState(characterStateManager);
            
            enemyStatusHandler = Character.GetComponent<EnemyStatusHandler>();
            enemyLocomotionHandler = Character.GetComponent<EnemyLocomotionHandler>();
            enemyStateManager = characterStateManager as EnemyStateManager;

            InitializeWayPoints();
        }

        protected override void AliveEnterState()
        {
            timeToWaitBeforeStartPatrolling = enemyStateManager.TimeToWaitBeforeStartPatrolling;
            timeToWaitToReachNextWayPoint = enemyStateManager.TimeToWaitToReachNextWayPoint;
        }

        protected override void AliveUpdateState()
        {
            // Update the time to wait before start patrolling
            timeToWaitBeforeStartPatrolling = timeToWaitBeforeStartPatrolling > 0 ? timeToWaitBeforeStartPatrolling - Time.deltaTime : 0;

            if (!enemyStatusHandler.IsPlayerTargeted && timeToWaitBeforeStartPatrolling <= 0)
            {
               HandlePatrolling();
            }
            else if(enemyStatusHandler.IsPlayerTargeted)
            {
                enemyStateManager.ChangeState(enemyStateManager.ChasingPlayerState);
            }
        }

        private void HandlePatrolling()
        {
            // The way point has been reached
            if (enemyLocomotionHandler.NavMeshAgent.remainingDistance <= enemyLocomotionHandler.NavMeshAgent.stoppingDistance)
            {
                // Wait for a while after reaching the way point
                enemyLocomotionHandler.NavMeshAgent.ResetPath();
                timeToWaitToReachNextWayPoint -= Time.deltaTime;

                // The wait is over
                if (timeToWaitToReachNextWayPoint <= 0)
                {
                    wayPointIndex = wayPointIndex < wayPointsNumber - 1 ? wayPointIndex + 1 : 0;

                    timeToWaitToReachNextWayPoint = enemyStateManager.TimeToWaitToReachNextWayPoint;
                }
            }

            // Update enemy's destination
            enemyLocomotionHandler.NavMeshAgent.SetDestination(wayPoints[wayPointIndex]);
        }

        private void InitializeWayPoints()
        {
            int groundLayer = LayerMask.GetMask(Utils.GROUND_LAYER_NAME);

            wayPointsNumber = Random.Range(enemyStateManager.MinWayPointsNumber, enemyStateManager.MaxWayPointsNumber + 1);
            wayPoints = new Vector3[wayPointsNumber];

            // The first way point is the enemy's current position
            wayPoints[0] = Character.position;

            // Initial currentDir is enemy forward direction
            Vector3 currentDir = Character.forward;

            notWalkableWallReached = false;
            for (int i = 1; i < wayPointsNumber; i++)
            {
                // Compute the distance of the next way point
                float distance = Random.Range(enemyStateManager.MinDistanceBetweenWayPoints, enemyStateManager.MaxDistanceBetweenWayPoints);

                // If the enemy has reached a wall, it will look back
                currentDir = notWalkableWallReached ? -currentDir : currentDir;

                // Compute the direction of the next way point
                Vector3 nextWayPointDir = Quaternion.Euler(0, Random.Range(-enemyStateManager.LookForNextPointMaxAngle, enemyStateManager.LookForNextPointMaxAngle), 0) * currentDir;

                // Compute the start position of the raycast
                Vector3 start = wayPoints[i - 1] + Vector3.up * enemyStateManager.HeightOffsetToAvoidObstacles;

                // Compute the max slope angle that the agent can walk on
                int agentTypeID = enemyLocomotionHandler.NavMeshAgent.agentTypeID;
                NavMeshBuildSettings settings = NavMesh.GetSettingsByID(agentTypeID);
                float maxSlopeAngle = settings.agentSlope;

                // Perform a raycast to check if a not walkable wall has been reached (due to a too big angle between hit normal and Vector3.up)
                RaycastHit[] hits = Physics.RaycastAll(start, nextWayPointDir, distance, groundLayer, QueryTriggerInteraction.Ignore)
                                            .Where(hit => Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle)
                                            .OrderBy(hit => Vector3.Distance(start, hit.point))
                                            .ToArray();

                notWalkableWallReached = hits.Length > 0;

                // If an obstacle is reached with the given distance,
                // reduce it
                if (notWalkableWallReached)
                {
                    Vector3 obstaclePosition = hits.FirstOrDefault().point;
                    distance = Vector3.Distance(start, obstaclePosition) - enemyStateManager.WallOffset;
                }
                
                // Compute next way point starting from the previuos one, but with the enemy y position,
                // in order to avoid floating way points and to set as default y, the one of the enemy
                // (if no y is found, the default returned is the one of the enemy y)
                Vector3 nextWayPoint = new Vector3(wayPoints[i - 1].x, Character.position.y, wayPoints[i - 1].z) + nextWayPointDir.normalized * distance;

                // If the path is not reachable, look up for the closest ground point and
                // try to use that position as way point
                if (!enemyLocomotionHandler.IsPathAvailable(nextWayPoint))
                {
                    nextWayPoint.y = Utils.LookUpForGroundYInWorldSpace(nextWayPoint);

                    // If even in this case the path is not reachable, repeat this iteraction of the loop
                    if (!enemyLocomotionHandler.IsPathAvailable(nextWayPoint))
                    {
                        i--;
                        continue;
                    }
                }

                // If the new way point is too close to one of the previously computed points, it will be discarded
                bool isNextPointTooClose = false;
                for (int j = 0; j < i; j++)
                {
                    if (Vector3.Distance(nextWayPoint, wayPoints[j]) < enemyStateManager.MinDistanceBetweenWayPoints)
                    {
                        isNextPointTooClose = true;
                        break;
                    }
                }

                // If a too close point is found, repeat this iteraction of the loop
                if (isNextPointTooClose)
                {
                    i--;
                    continue;
                }

                wayPoints[i] = nextWayPoint;
                currentDir = (wayPoints[i] - wayPoints[i - 1]).normalized;
            }
        }
    }
}