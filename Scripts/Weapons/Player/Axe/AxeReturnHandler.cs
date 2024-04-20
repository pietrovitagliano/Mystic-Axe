// Author: Pietro Vitagliano

using Cinemachine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MysticAxe
{
    [RequireComponent(typeof(AxeStatusHandler))]
    public class AxeReturnHandler : MonoBehaviour
    {
        [Serializable]
        private class AxeReturnDistanceTimeInfo
        {
            [SerializeField, Min(0.1f)] private float distanceFromHand;
            [SerializeField, Min(0.1f)] private float returnTime;

            public float DistanceFromHand { get => distanceFromHand; }
            public float ReturnTime { get => returnTime; }
        }

        private AxeThrowHandler axeThrowHandler;
        private AxeStatusHandler axeStatusHandler;
        private Transform weaponHand;

        [Header("Axe Returning")]
        [SerializeField, Range(0.1f, 2)] private float lookAtHandAnimationTime = 0.4f;
        [SerializeField, Range(0.1f, 5)] private float distanceFromHandToStopRotation = 1.5f;
        [SerializeField] private Quaternion axeReturningRotation = Quaternion.Euler(-265f, 0f, -180f);
        [SerializeField] private AxeReturnDistanceTimeInfo[] distanceTimeInfoArray;
        private Transform returningMiddlePoint;
        private float maxReturnDistance;
        private float returnTime = 1;
        private float elapsedReturnTime = 0;
        private Vector3 startPullingAxePosition;
        private Coroutine axeSlerpCoroutine;

        [Header("Detect Enemies While Returning")]
        [SerializeField, Range(1, 8)] private int detectableEnemiesMaxNumber = 4;
        [SerializeField, Range(0.1f, 10)] private float enemyDetectionSphereRadius = 1.75f;
        [SerializeField, Range(0.1f, 3)] private float timeAddedForEnemy = 0.4f;
        private List<Transform> enemyToHitWhileReturningList = new List<Transform>();

        [Header("Axe Return Sound Settings")]
        [SerializeField, Min(0.1f)] private float soundSpeedWhenReturning = 1.3f;
        [SerializeField, Range(0, 1)] private float catchSoundNormalizedStartTime = 0.85f;

        private readonly UnityEvent onCatchAxeEvent = new UnityEvent();

        public float ElapsedReturnTime { get => elapsedReturnTime; }
        public float ReturnTime { get => returnTime; }
        public UnityEvent OnCatchAxeEvent => onCatchAxeEvent;

        private void Start()
        {
            axeThrowHandler = GetComponent<AxeThrowHandler>();
            axeStatusHandler = GetComponent<AxeStatusHandler>();

            maxReturnDistance = distanceTimeInfoArray.Max(info => info.DistanceFromHand);

            Transform player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG).transform;
            weaponHand = Utils.FindGameObjectInTransformWithTag(player, Utils.WEAPON_HOLDER_TAG).transform;
            returningMiddlePoint = Utils.FindGameObjectInTransformWithTag(player, Utils.AXE_RETURNING_MIDDLE_POINT_TAG).transform;
        }

        private void Update()
        {
            HandleAxeReturn();
        }

        public void RecallAxe()
        {
            DOTween.Kill(transform);

            // Axe collider is now a physic one
            axeStatusHandler.WeaponCollider.isTrigger = true;

            // Axe collider is now enabled again (it could be disabled by other scripts)
            axeStatusHandler.WeaponCollider.enabled = true;

            axeStatusHandler.Rigidbody.constraints = RigidbodyConstraints.None;
            axeStatusHandler.Rigidbody.isKinematic = true;
            
            axeStatusHandler.ClearCharacterAlreadyHitSet();
            axeStatusHandler.ResetWeaponRigidbodyVelocityAndRotation();
            axeThrowHandler.ThrowEnded();

            InitializeReturnVariables();

            axeStatusHandler.CurrentRotationSpeed = Mathf.Max(axeThrowHandler.InitialRotationMultiplier * axeThrowHandler.MaxRotationSpeed, axeStatusHandler.CurrentRotationSpeed);

            // Get the direction from axe towards hand
            Vector3 towardsHandDirection = (weaponHand.position - transform.position).normalized;

            // Make the axe look into the direction of player's hand
            AxeLookAtDirectionWithRotation(towardsHandDirection, axeReturningRotation, lookAtHandAnimationTime);

            axeStatusHandler.IsReturning = true;
            AudioManager.Instance.PlayMutuallyExclusiveSoundWithFadeIn(Utils.playerAxeThrowWhooshesAudioName, gameObject, fadeTime: 0.1f);
        }

        private void InitializeReturnVariables()
        {
            // If the axe is too far from player's hand
            // (that is when it is really difficult to see it with the naked eye),
            // teleport it at the max distance from hand
            float distanceFromHand = Vector3.Distance(transform.position, weaponHand.position);
            if (distanceFromHand > maxReturnDistance)
            {
                Vector3 playerHandToAxeDirection = (transform.position - weaponHand.position).normalized;
                transform.position = weaponHand.position + maxReturnDistance * playerHandToAxeDirection;
            }

            startPullingAxePosition = transform.position;
            enemyToHitWhileReturningList = FindEnemiesOnReturningPath();
            returnTime = GetReturnTime();

            if (enemyToHitWhileReturningList.Count > 0)
            {
                // timeAddedForEnemy cannot be too big relative to the returnTime.
                // This clamp is done in order to avoid this situation.
                // Return time can be at most 2 times its original value.
                float timeAddedForEnemyConstrained = Mathf.Clamp(timeAddedForEnemy, 0, returnTime / detectableEnemiesMaxNumber);
                returnTime += timeAddedForEnemyConstrained * enemyToHitWhileReturningList.Count;
            }
        }

        private float GetReturnTime()
        {
            float distanceFromHand = Vector3.Distance(transform.position, weaponHand.position);

            AxeReturnDistanceTimeInfo[] infoArray = distanceTimeInfoArray.OrderBy(info => info.DistanceFromHand)
                                                                        .ToArray();
            AxeReturnDistanceTimeInfo lowerInfo = null;
            AxeReturnDistanceTimeInfo upperInfo = null;

            if (distanceFromHand <= infoArray[0].DistanceFromHand)
            {
                return infoArray[0].ReturnTime;
            }
            else if (distanceFromHand >= infoArray[infoArray.Length - 1].DistanceFromHand)
            {
                return infoArray[infoArray.Length - 1].ReturnTime;
            }
            else
            {
                for (int i = 0; i < infoArray.Length - 1; i++)
                {
                    if (distanceFromHand >= infoArray[i].DistanceFromHand && distanceFromHand <= infoArray[i + 1].DistanceFromHand)
                    {
                        lowerInfo = infoArray[i];
                        upperInfo = infoArray[i + 1];
                        break;
                    }
                }
            }

            float lowerDelta = Mathf.Abs(distanceFromHand - lowerInfo.DistanceFromHand);
            float upperDelta = Mathf.Abs(distanceFromHand - upperInfo.DistanceFromHand);
            AxeReturnDistanceTimeInfo axeReturnInfo = lowerDelta < upperDelta ? lowerInfo : upperInfo;

            return axeReturnInfo.ReturnTime;
        }
        
        private void AxeLookAtDirectionWithRotation(Vector3 direction, Quaternion desiredLocalRotation, float lookAtDuration)
        {
            // Compute the rotation that axe has to possess while returning back
            Quaternion targetRotation = Quaternion.LookRotation(direction) * desiredLocalRotation;

            // Set the axe rotation to the end rotation
            axeSlerpCoroutine = StartCoroutine(AxeSlerpRotationCoroutine(targetRotation: targetRotation, duration: lookAtDuration));
        }

        private IEnumerator AxeSlerpRotationCoroutine(Quaternion targetRotation, float duration)
        {            
            float timeElapsed = 0;
            while (timeElapsed < duration)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1.15f * Time.deltaTime / duration);
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            transform.rotation = targetRotation;
            axeSlerpCoroutine = null;
        }

        private void HandleAxeReturn()
        {
            IEnumerator HandlePhysicsOnCharacterDeathCoroutine()
            {
                // Store the current position of the axe
                Vector3 initialPosition = transform.position;

                // Wait for 1 frame
                yield return null;
                
                // Compute the velocity direction
                Vector3 velocityDirection = (transform.position - initialPosition).normalized;

                // Compute the velocity magnitude the axe has to have in order to reach
                // player's hand (the Clamp is used in order to handle the division -> space / time)
                float remainingTime = Mathf.Clamp(returnTime - elapsedReturnTime, 0.01f, returnTime);
                float distanceFromHand = Vector3.Distance(transform.position, weaponHand.position);
                float velocityMagnitude = distanceFromHand / remainingTime;

                // When the player is dead, physics is enabled and the axe collider
                // becomes a physics one as well, in order to let the axe fall on the ground
                axeStatusHandler.WeaponCollider.isTrigger = false;
                axeStatusHandler.Rigidbody.isKinematic = false;
                axeStatusHandler.EnablesRigidBodyProperties(propertiesEnabled: true);

                // Apply the velocity to the axe rigidbody
                axeStatusHandler.Rigidbody.velocity = velocityMagnitude * velocityDirection;

                // Apply a torque to the axe rigidbody in order to make it rotate
                axeStatusHandler.Rigidbody.angularVelocity = axeStatusHandler.CurrentRotationSpeed * axeStatusHandler.GetWeaponForwardDirection();

                // Stop throw whoosh sound
                AudioManager.Instance.StopSoundWithFadeOut(Utils.playerAxeThrowWhooshesAudioName, gameObject);

                // The player is dead, thus the axe returning is stopped
                axeStatusHandler.IsReturning = false;
            }

            if (axeStatusHandler.IsReturning)
            {
                HandleAxePositionWhileReturning();
                HandleAxeRotationWhileReturning();

                if (axeStatusHandler.PlayerStatusHandler.IsDead)
                {
                    StartCoroutine(HandlePhysicsOnCharacterDeathCoroutine());
                }
            }
        }

        private void HandleAxePositionWhileReturning()
        {
            // Normalize the elapsed time
            float elapsedReturnTimeNormalized = elapsedReturnTime / returnTime;
            if (elapsedReturnTimeNormalized <= 1)
            {
                if (elapsedReturnTimeNormalized >= catchSoundNormalizedStartTime)
                {
                    // Stop throw sound
                    AudioManager.Instance.StopSound(Utils.playerAxeThrowWhooshesAudioName, gameObject);

                    // Play catch sound
                    AudioManager.Instance.PlayMutuallyExclusiveSound(Utils.playerAxeCatchAudioName, gameObject);
                }

                if (enemyToHitWhileReturningList.Count > 0)
                {
                    List<Vector3> returningPointList = ComputeReturningPointList();
                    transform.position = GetCatmullRomSplinePoint(elapsedReturnTimeNormalized, returningPointList);
                }
                else
                {
                    transform.position = GetQuadraticCurvePoint(elapsedReturnTimeNormalized, startPullingAxePosition, returningMiddlePoint.position, weaponHand.position);
                }

                elapsedReturnTime += Time.deltaTime;
            }
        }

        private void HandleAxeRotationWhileReturning()
        {
            // Normalize the elapsed time
            float elapsedReturnTimeNormalized = elapsedReturnTime / returnTime;

            // Compute the axe distance from hand
            float distanceFromHand = Vector3.Distance(transform.position, weaponHand.transform.position);

            // Axe is far enough from hand, thus it rotates and can cut enemies
            if (elapsedReturnTimeNormalized <= 0.3f || distanceFromHand > distanceFromHandToStopRotation)
            {
                // The rotation speed is gradually increased
                axeStatusHandler.CurrentRotationSpeed = Mathf.Lerp(axeStatusHandler.CurrentRotationSpeed,
                                                                    axeThrowHandler.MaxRotationSpeed,
                                                                    Time.deltaTime / axeThrowHandler.TimeToReachMaxRotationSpeed);

                axeStatusHandler.RotateAxe();
            }
            // Axe is near to hand, thus it doesn't rotate and can't cut enemies: this is why, axe's trigger is disabled
            else
            {
                if (axeStatusHandler.WeaponCollider.enabled)
                {
                    // The axe rotation slows down here, so it can't cut anyone anymore
                    axeStatusHandler.WeaponCollider.enabled = false;
                    axeStatusHandler.CurrentRotationSpeed = 0;

                    if (axeSlerpCoroutine != null)
                    {
                        StopCoroutine(axeSlerpCoroutine);
                    }

                    // Compute the time left before axe return to the hand
                    float timeLeft = returnTime - elapsedReturnTime;
                    axeSlerpCoroutine = StartCoroutine(AxeSlerpRotationCoroutine(targetRotation: weaponHand.rotation, duration: timeLeft));
                }
            }
        }

        public void CatchAxe()
        {
            if (!axeStatusHandler.PlayerStatusHandler.IsDead)
            {
                if (axeSlerpCoroutine != null)
                {
                    StopCoroutine(axeSlerpCoroutine);
                    axeSlerpCoroutine = null;
                }

                DOTween.Kill(transform);

                elapsedReturnTime = 0;
                axeStatusHandler.CurrentRotationSpeed = 0;
                axeStatusHandler.IsReturning = false;
                axeStatusHandler.HasHitWall = false;

                AudioManager.Instance.StopSound(Utils.playerAxeCatchAudioName, gameObject);

                transform.parent = weaponHand;

                axeStatusHandler.ResetWeaponRigidbodyVelocityAndRotation();
                Utils.ResetTransformLocalPositionAndRotation(transform);

                // Clear the list
                enemyToHitWhileReturningList.Clear();

                // Catch axe event is invoked
                onCatchAxeEvent.Invoke();

                // Shake
                GetComponent<CinemachineImpulseSource>().GenerateImpulse(Vector3.right);

                // Stop throw sound (if has not already been stopped)
                AudioManager.Instance.StopSound(Utils.playerAxeThrowWhooshesAudioName, gameObject);
            }
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (!axeStatusHandler.IsAxeOnBody() && axeStatusHandler.WeaponCollider.isTrigger && axeStatusHandler.IsReturning)
            {
                int enemyLayerValue = LayerMask.NameToLayer(Utils.ENEMY_LAYER_NAME);

                if (otherCollider.gameObject.layer == enemyLayerValue &&
                    !axeStatusHandler.HasCharacterBeenAlreadyHit(otherCollider.gameObject))
                {
                    Transform character = otherCollider.transform.root;
                    CharacterStatusHandler characterHitStatusHandler = character.GetComponent<CharacterStatusHandler>();

                    if (!axeStatusHandler.HasCharacterBeenAlreadyHit(character.gameObject) && !characterHitStatusHandler.IsDead &&
                        characterHitStatusHandler.IsHitableCollider(otherCollider) && axeStatusHandler.IsReturning)
                    {
                        // This is a hack to avoid the axe to hit the enemy twice in a time interval too short.
                        // If enemy is hit after timeToHitSameEnemyAgain (declared inside AxeStatusHandler),
                        // it means that the enemy was effectively hit again.
                        StartCoroutine(axeStatusHandler.MarkCharacterAsHitCoroutine(character.gameObject));

                        // Apply damage to enemy
                        axeStatusHandler.ApplyDamage(characterHitStatusHandler);
                        axeStatusHandler.HitCharacter(characterHitStatusHandler, weaponPosition: transform.position);
                        axeStatusHandler.WeaponBloodSpawner.MakeCharacterBleed(characterHitStatusHandler, otherCollider);

                        if (characterHitStatusHandler.IsDead)
                        {
                            // Play enemy death hit sound
                            AudioManager.Instance.PlayMutuallyExclusiveSound(Utils.deathHitAudioName, gameObject);
                        }
                        else
                        {
                            // Play enemy hit sound
                            AudioManager.Instance.PlaySound(Utils.playerAxeEnemyLightHitAudioName, gameObject);
                        }
                    }
                }
            }
        }

        private List<Vector3> ComputeReturningPointList()
        {
            List<Vector3> enemyPositions = enemyToHitWhileReturningList.Where(transform => !transform.GetComponent<CharacterStatusHandler>().IsDead)
                                                                        .Select(transform => transform.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.UpperChest).position)
                                                                        .ToList();

            List<Vector3> allPoints = new List<Vector3>(enemyPositions.Count + 3);

            allPoints.Add(startPullingAxePosition);
            allPoints.AddRange(enemyPositions);
            allPoints.Add(returningMiddlePoint.position);
            allPoints.Add(weaponHand.position);

            return allPoints;
        }

        private List<Transform> FindEnemiesOnReturningPath()
        {
            int enemyLayerMask = LayerMask.GetMask(Utils.ENEMY_LAYER_NAME);
            Transform player = weaponHand.root;
            
            Vector3 axeToPlayerDirection = (player.position - transform.position).normalized;
            Vector3 sphereCenter = transform.position + axeToPlayerDirection * enemyDetectionSphereRadius;
            Vector3 sphereCastEndPoint = player.position - axeToPlayerDirection * enemyDetectionSphereRadius;
            
            float playerToAxeDistance = Vector3.Distance(player.position, transform.position);
            float sphereCastMaxDistance = Vector3.Distance(sphereCenter, sphereCastEndPoint);

            return Physics.SphereCastAll(sphereCenter, enemyDetectionSphereRadius, axeToPlayerDirection, sphereCastMaxDistance, enemyLayerMask, QueryTriggerInteraction.Collide)
                        .Where(hit =>
                        {
                            Transform character = hit.collider.transform.root;
                            CharacterStatusHandler characterStatusHandler = character.GetComponent<CharacterStatusHandler>();
                            
                            bool distanceCondition = Vector3.Distance(character.position, transform.position) < playerToAxeDistance;
                            
                            return !characterStatusHandler.IsDead && characterStatusHandler.IsHitableCollider(hit.collider) && distanceCondition;
                        })
                        .Select(hit => hit.transform.root.gameObject)
                        .Distinct()
                        .OrderBy(gameObject => Vector3.Distance(gameObject.transform.position, transform.position))
                        .Take(detectableEnemiesMaxNumber)
                        .Select(gameObject => gameObject.transform)
                        .ToList();
        }

        /*
         * This method calculates a point on a Bezier curve,
         * given the start point (p0), an array of optional intermediate control points (optPoints),
         * and the control point (p1) and the end point (p2).
         * The time value t between 0 and 1 represents the position
         * of the point on the curve.
         * 
         * The curve starts at point p0 when t is 0, and ends at point p2 when t is 1.
         * The optional points in the array act as additional control points that determine the shape of the curve.
         */
        private Vector3 GetQuadraticCurvePoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float x = 1 - t;
            
            return x * x * p0 + 2 * x * t * p1 + t * t * p2;
        }

        /*
        * This method calculates a point on a Catmull-Rom spline,
        * given a list of points that define the spline (including the start and end points)
        * and a time value t between 0 and 1 that represents the position
        * of the point on the spline.
        * The spline starts at the first point in the list when t is 0,
        * and ends at the last point in the list when t is 1.
        * The spline passes through each point in the list, using the neighboring points
        * to determine the shape of the curve.
        * The Catmull-Rom spline is a type of cubic Hermite spline, which provides
        * smooth transitions between points while ensuring the curve passes through
        * each control point.
        */
        private Vector3 GetCatmullRomSplinePoint(float t, List<Vector3> points)
        {
            static Vector3 CatmullRom(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
            {
                float t2 = t * t;
                float t3 = t2 * t;

                Vector3 a = -0.5f * p0 + 1.5f * p1 - 1.5f * p2 + 0.5f * p3;
                Vector3 b = p0 - 2.5f * p1 + 2f * p2 - 0.5f * p3;
                Vector3 c = -0.5f * p0 + 0.5f * p2;
                Vector3 d = p1;

                return a * t3 + b * t2 + c * t + d;
            }
            
            int numPoints = points.Count;
            float segmentLength = 1f / (numPoints - 1);
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(t / segmentLength), numPoints - 2);
            float localT = (t - segmentIndex * segmentLength) / segmentLength;

            Vector3 p0 = points[Mathf.Max(segmentIndex - 1, 0)];
            Vector3 p1 = points[segmentIndex];
            Vector3 p2 = points[segmentIndex + 1];
            Vector3 p3 = points[Mathf.Min(segmentIndex + 2, numPoints - 1)];

            return CatmullRom(localT, p0, p1, p2, p3);
        }
    }
}