// Author: Pietro Vitagliano

using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    public class LockOnHandler : MonoBehaviour
    {
        private Transform player;
        private InputHandler inputHandler;
        private PlayerStatusHandler playerStatusScript;

        [Header("Canvas Lock On Prefab")]
        [SerializeField] private GameObject canvasLockOnPrefab;
        private RectTransform canvasLockOnTarget;

        [Header("Targeting Settings")]
        [SerializeField, Min(0.1f)] float distanceOverAngleThreshold = 4f;
        [SerializeField, Range(1, 90)] private float maxTargetVerticalAngle = 40;
        [SerializeField, Range(0.1f, 20f)] float maxDistanceToTarget = 15f;

        [Header("Switch Target")]
        [SerializeField, Range(0.1f, 1)] private float switchTargetThreshold = 0.5f;
        [SerializeField, Range(0.1f, 1)] private float switchTargetDeadZone = 0.5f;
        [SerializeField, Range(0.1f, 1)] private float switchTargetTime = 0.3f;
        [SerializeField, Min(0.1f)] float switchTargetAngle = 40;

        private Targetable currentTarget;
        private bool isSwitchingTarget;

        public RectTransform CanvasLockOnTarget { get => canvasLockOnTarget; }
        public bool IsSwitchingTarget { get => isSwitchingTarget; }
        public float MaxDistanceToTarget { get => maxDistanceToTarget; }

        // The canvasLockOnTarget initialization has to be done in the Awake,
        // since there could be other scripts that need it in their Start.
        private void Awake()
        {
            GameObject canvasLockOnTargetGameObject = GameObject.FindGameObjectWithTag(Utils.CANVAS_LOCK_ON_TARGET_TAG);
            if (canvasLockOnTargetGameObject == null)
            {
                canvasLockOnTargetGameObject = Instantiate(canvasLockOnPrefab);
            }

            canvasLockOnTarget = canvasLockOnTargetGameObject.GetComponent<RectTransform>();
        }

        private void Start()
        {
            inputHandler = InputHandler.Instance;

            player = FindObjectsOfType<Transform>(includeInactive: true).ToList().Find(transform => transform.CompareTag(Utils.PLAYER_TAG));
            playerStatusScript = player.GetComponent<PlayerStatusHandler>();
        }

        private void Update()
        {
            HandleStartAndStopTargeting();
            HandleTargeting();            
        }

        private void HandleStartAndStopTargeting()
        {
            if (inputHandler.TargetEnemyPressed)
            {
                inputHandler.TargetEnemyPressed = false;
                
                if (!playerStatusScript.IsTargetingEnemy)
                {
                    Targetable enemy = FindTarget();

                    if (enemy != null)
                    {
                        // Reset camera input and targeting input are the same,
                        // this is why it's necessary to set inputHandler.ResetCamera to false.
                        // IMPORTANT: This script must be executed before the one that handles the camera reset.
                        inputHandler.ResetCamera = false;

                        // Attach the lock on target to the enemy
                        Utils.AttachCanvas(canvasLockOnTarget, enemy.transform);
                    }
                }
                else
                {
                    // Reset camera input and targeting input are the same,
                    // this is why it's necessary to set inputHandler.ResetCamera to false.
                    // IMPORTANT: This script must be executed before the one that handles the camera reset.
                    inputHandler.ResetCamera = false;

                    // Deattach the lock on target from the enemy
                    Utils.DeattachCanvas(canvasLockOnTarget);
                }
            }
        }
        
        private void HandleTargeting()
        {
            if (playerStatusScript.IsTargetingEnemy && canvasLockOnTarget.parent != null)
            {
                currentTarget = canvasLockOnTarget.parent.GetComponent<Targetable>();

                if (currentTarget == null || !IsTargetValid(currentTarget.transform))
                {
                    // Deattach the lock on target from the enemy
                    Utils.DeattachCanvas(canvasLockOnTarget);
                }
                else
                {
                    float cameraHorizontalDelta = inputHandler.CameraRotationInput.x;
                    cameraHorizontalDelta = Mathf.Abs(cameraHorizontalDelta) > switchTargetDeadZone ? cameraHorizontalDelta : 0;

                    float cameraVerticalDelta = inputHandler.CameraRotationInput.y;
                    cameraVerticalDelta = Mathf.Abs(cameraVerticalDelta) > switchTargetDeadZone ? cameraVerticalDelta : 0;
                    Vector2 input = new Vector2(cameraHorizontalDelta, cameraVerticalDelta);
                    
                    if (!isSwitchingTarget && !playerStatusScript.IsAiming && input.magnitude > switchTargetThreshold)
                    {
                        Targetable enemy = ChangeTarget(cameraHorizontalDelta, cameraVerticalDelta);

                        if (enemy != null)
                        {
                            // Attach the lock on target to the enemy
                            Utils.AttachCanvas(canvasLockOnTarget, enemy.transform);

                            StartCoroutine(WaitForNextTargetSwitch(switchTargetTime));                            
                        }
                    }
                }
            }
            else
            {
                currentTarget = null;
            }
        }

        private IEnumerator WaitForNextTargetSwitch(float timeToWait)
        {
            isSwitchingTarget = true;

            float elapsedTime = 0;
            while (elapsedTime < timeToWait)
            {
                elapsedTime = inputHandler.CameraRotationInput.magnitude < 0.35f ? elapsedTime + Time.deltaTime : 0;

                yield return null;
            }

            isSwitchingTarget = false;
        }

        private bool IsTargetValid(Transform target)
        {
            bool IsTargetSightClear(Transform target)
            {
                Camera camera = Camera.main;

                // Compute the direction vector from the camera to the target and its projection on the ground
                Vector3 targetPositionToSee = Utils.GetCharacterHeightPosition(target, Utils.UPPER_CHEST_HEIGHT);
                Vector3 cameraToTargetDirection = (targetPositionToSee - camera.transform.position).normalized;
                Vector3 cameraToTargetProjectedDirection = Vector3.ProjectOnPlane(cameraToTargetDirection, Vector3.up);

                // Compute the angle between camera and target and camera and target projected on the ground
                float cameraToTargetAngle = Vector3.Angle(cameraToTargetProjectedDirection, cameraToTargetDirection);

                if (cameraToTargetAngle <= maxTargetVerticalAngle)
                {
                    // Camera is behind player, thus it's possible to get collision with wall before him.
                    // This is why the raycast has to start from the z of the player
                    Vector3 cameraToPlayerRelativeToCameraDirection = camera.transform.InverseTransformDirection(player.position - camera.transform.position);
                    Vector3 cameraPositionWithOffset = camera.transform.position + cameraToPlayerRelativeToCameraDirection.z * camera.transform.forward;
                    Vector3 rayDirection = (targetPositionToSee - cameraPositionWithOffset).normalized;
                    float rayDistance = Vector3.Distance(cameraPositionWithOffset, targetPositionToSee);

                    // Perform a raycast from the camera to the target to see if there is a wall in between.
                    // The walls near to the camera position (radius = 1) are ignored
                    int wallLayer = LayerMask.GetMask(Utils.GROUND_LAYER_NAME, Utils.OBSTACLES_LAYER_NAME);
                    Collider[] interpenetratedCameraColliders = Physics.OverlapSphere(cameraPositionWithOffset, radius: 1, wallLayer, QueryTriggerInteraction.Ignore);
                    RaycastHit[] wallInterpenetrationHits = Physics.RaycastAll(cameraPositionWithOffset, rayDirection, rayDistance, wallLayer)
                                                                    .Where(hit => !interpenetratedCameraColliders.Contains(hit.collider))
                                                                    .ToArray();


                    // If no wall has been hit, then the target is visible
                    return wallInterpenetrationHits.Length == 0;
                }

                return false;
            }
            
            if (target != null)
            {
                CharacterStatusHandler characterStatusHandler = target.GetComponent<CharacterStatusHandler>();
                float distanceFromTarget = Vector3.Distance(player.transform.position, target.transform.position);

                return !characterStatusHandler.IsDead && target.GetComponent<Targetable>() != null &&
                        distanceFromTarget <= maxDistanceToTarget && IsTargetSightClear(target);
            }

            return false;
        }
        
        private Targetable FindTarget()
        {
            Vector3 playerChest = Utils.GetCharacterHeightPosition(player.transform, Utils.UPPER_CHEST_HEIGHT);

            // Find all valid game objects that belongs to the Enemy layer and have a targetable script component.
            // After, check if these objects are inside the screen.
            int enemyLayer = LayerMask.GetMask(Utils.ENEMY_LAYER_NAME);
            Targetable[] enemies = Physics.OverlapSphere(playerChest, maxDistanceToTarget, enemyLayer, QueryTriggerInteraction.Ignore)
                                        .Where(collider => Utils.IsTransformInsideScreen(collider.transform) && IsTargetValid(collider.transform))
                                        .Select(collider => collider.gameObject.GetComponent<Targetable>())
                                        .ToArray();

            // DEBUG CODE HERE
            /*foreach (Transform enemy in enemies)
            {
                Debug.Log("FindTarget enemy = " + enemy);
                Debug.Log("FindTarget distance = " + Vector3.Distance(sphereCenter, Utils.GetBodyTargetPosition(enemy)));
                Debug.Log("FindTarget angle = " + Vector3.Angle(transform.forward, Utils.GetBodyTargetPosition(enemy) - transform.position));
            }
            */

            return FindNearestEnemyByDistanceAndAngle(enemies);
        }
        
        private Targetable ChangeTarget(float inputX, float inputY)
        {
            Vector3 playerChest = Utils.GetCharacterHeightPosition(player.transform, Utils.UPPER_CHEST_HEIGHT);
            Vector3 sphereCenter = Utils.GetCharacterHeightPosition(currentTarget.transform, Utils.UPPER_CHEST_HEIGHT);
            float sphereRadius = Mathf.Max(maxDistanceToTarget, Vector3.Distance(sphereCenter, playerChest));

            // Except the current target, find all valid game objects that belongs to the Enemy layer,
            // have a targetable script component and are in front of the player.
            // After, orders them by distance from currentTarget
            int enemyLayer = LayerMask.GetMask(Utils.ENEMY_LAYER_NAME);
            Targetable[] enemies = Physics.OverlapSphere(sphereCenter, sphereRadius, enemyLayer, QueryTriggerInteraction.Ignore)
                                    .Where(collider => collider.transform != currentTarget.transform && IsTargetValid(collider.gameObject.transform))
                                    .Select(collider => collider.gameObject.GetComponent<Targetable>())
                                    .ToArray();

            Targetable[] filteredEnemies = enemies.Where(enemy => Vector3.Distance(currentTarget.transform.position, enemy.transform.position) <= maxDistanceToTarget).ToArray();
            Targetable nearestEnemy = FindNearestEnemyByDistance(filteredEnemies, currentTarget.transform, isDistanceFromCameraPOV: true, inputX, inputY);

            // If not enemis are found, search for enemies near to the player
            if (nearestEnemy == null)
            {
                filteredEnemies = enemies.Where(enemy => Vector3.Distance(player.transform.position, enemy.transform.position) <= maxDistanceToTarget).ToArray();
                nearestEnemy = FindNearestEnemyByDistance(filteredEnemies, player.transform, isDistanceFromCameraPOV: true, inputX, inputY);
            }

            return nearestEnemy;
        }

        /**
         * This method is used to find the nearest enemy by distance and angle.
         * It looks for enemies in both X and Y cameraInput direction and returns the nearest one.
         */
        private Targetable FindNearestEnemyByDistanceAndAngle(Targetable[] enemies, bool isDistanceFromCameraPOV = false, float inputX = 0, float inputY = 0)
        {
            float GetAngleFromCameraAndEnemy(Targetable enemy)
            {
                Vector3 cameraPosition = transform.position;
                Vector3 enemyChestPosition = Utils.GetCharacterHeightPosition(enemy.transform, Utils.UPPER_CHEST_HEIGHT);
                Vector3 cameraToEnemyChestDirection = enemyChestPosition - cameraPosition;

                return Vector3.Angle(transform.forward, cameraToEnemyChestDirection);
            }

            // Nearest enemy by angle
            Targetable nearestEnemyByAngle = enemies.OrderBy(enemy => GetAngleFromCameraAndEnemy(enemy))
                                                    .FirstOrDefault(enemy =>
                                                    {
                                                        if (inputX == 0 && inputY == 0)
                                                        {
                                                            return true;
                                                        }
                                                        else
                                                        {
                                                            return IsTargetInInputDirection(currentTarget, enemy, inputX, inputY);
                                                        }
                                                    });

            // Nearest enemy by distance
            Targetable nearestEnemyByDistance = FindNearestEnemyByDistance(enemies, player.transform, isDistanceFromCameraPOV, inputX, inputY);


            Targetable[] nearestEnemiesByAngleAndDistance = { nearestEnemyByAngle, nearestEnemyByDistance };
            nearestEnemiesByAngleAndDistance = nearestEnemiesByAngleAndDistance.Where(enemy => enemy != null).Distinct().ToArray();

            Targetable nearestEnemy = null;
            if (nearestEnemiesByAngleAndDistance.Length <= 1)
            {
                nearestEnemy = nearestEnemiesByAngleAndDistance.FirstOrDefault();
            }
            else
            {
                bool nearestEnemyByDistanceCondition = Vector3.Distance(player.transform.position, nearestEnemyByDistance.transform.position) < distanceOverAngleThreshold;
                nearestEnemy = nearestEnemyByDistanceCondition ? nearestEnemyByDistance : nearestEnemyByAngle;
            }

            return nearestEnemy;
        }

        private float ComputeDistance2D(Vector3 startPosition, Vector3 endPosition)
        {
            Camera camera = Camera.main;
            Vector2 inScreenStartPosition = camera.WorldToScreenPoint(startPosition);
            Vector2 inScreenEndPosition = camera.WorldToScreenPoint(endPosition);

            return Vector2.Distance(inScreenStartPosition, inScreenEndPosition);
        }
        
        private Targetable FindNearestEnemyByDistance(Targetable[] enemies, Transform startTransform, bool isDistanceFromCameraPOV = false, float inputX = 0, float inputY = 0)
        {
            float GetEnemyDistanceFromTransform(Transform startTransform, Targetable enemy, bool isDistanceFromCameraPOV)
            {
                if (isDistanceFromCameraPOV)
                {
                    Vector3 characterChest = startTransform == player ? Utils.GetCharacterHeightPosition(player.transform, Utils.UPPER_CHEST_HEIGHT) : Utils.GetCharacterHeightPosition(startTransform, Utils.UPPER_CHEST_HEIGHT);
                    Vector3 enemyChest = Utils.GetCharacterHeightPosition(enemy.transform, Utils.UPPER_CHEST_HEIGHT);
                    
                    return ComputeDistance2D(characterChest, enemyChest);
                }
                else
                {
                    return Vector3.Distance(startTransform.position, enemy.transform.position);
                }
            }
            
            // Nearest enemy by distance
            return enemies.OrderBy(enemy => GetEnemyDistanceFromTransform(startTransform, enemy, isDistanceFromCameraPOV))
                        .FirstOrDefault(enemy =>
                        {
                            if (inputX == 0 && inputY == 0)
                            {
                                return true;
                            }
                            else
                            {
                                return IsTargetInInputDirection(currentTarget, enemy, inputX, inputY);
                            }
                        });
        }

        private bool IsTargetInInputDirection(Targetable currentEnemy, Targetable newEnemy, float inputX, float inputY)
        {
            Vector3 currentTargetPosition = Utils.GetCharacterHeightPosition(currentEnemy.transform, Utils.UPPER_CHEST_HEIGHT);
            Vector3 newEnemyPosition = Utils.GetCharacterHeightPosition(newEnemy.transform, Utils.UPPER_CHEST_HEIGHT);
            Vector3 currentTargetToNewEnemyProjectedDirection = ProjectEnemyPositionPointRelativeToCurrentTarget(currentTargetPosition, newEnemyPosition);

            // Compute a vector that represents the input direction to switch target
            Vector3 inputSwitchDirection = inputY * Camera.main.transform.up + inputX * Camera.main.transform.right;
            inputSwitchDirection = inputSwitchDirection.normalized;
            
            // Compute the angle between the direction from currentTarget to new enemy and the input direction
            float angle = Vector3.Angle(inputSwitchDirection, currentTargetToNewEnemyProjectedDirection);

            // DEBUG CODE HERE
            /*
            Debug.Log("IsTargetInInputDirection angle " + angle);
            Debug.DrawRay(currentTargetPosition, inputSwitchDirection, Color.blue, 10);
            Debug.DrawRay(currentTargetPosition, currentTargetToNewEnemyProjectedDirection, Color.red, 10);
            */
            
            return angle <= switchTargetAngle;
        }

        private Vector3 ProjectEnemyPositionPointRelativeToCurrentTarget(Vector3 startPoint, Vector3 newPoint)
        {
            // Get the current camera
            Camera camera = Camera.main;

            // Calculate the direction from the startPoint to the newPoint
            Vector3 startPointToNewPointDirection = (newPoint - startPoint).normalized;

            // Return projected direction to the newPoint on plane perpendicular to the camera forward direction
            return Vector3.ProjectOnPlane(startPointToNewPointDirection, camera.transform.forward);
        }
    }
}