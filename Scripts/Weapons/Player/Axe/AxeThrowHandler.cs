// Author: Pietro Vitagliano

using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    public class AxeThrowHandler : MonoBehaviour
    {
        private AxeStatusHandler axeStatusHandler;
        private Transform player;

        [Header("Throw")]
        [SerializeField, Range(1, 200)] private float throwAcceleration = 55f;
        [SerializeField, Range(100, 4000)] private float maxRotationSpeed = 1800;
        [SerializeField, Range(1, 1000)] private float aimingOffset = 500;
        [SerializeField, Range(0, 1)] private float initialRotationMultiplier = 0.45f;
        [SerializeField, Min(0.1f)] private float timeToReachMaxRotationSpeed = 0.3f;
        [SerializeField] private Quaternion axeRotationWhenThrown = Quaternion.Euler(-31.5f, -101.2f, -2.75f);
        private Vector3 throwDirection = Vector3.zero;

        [Header("Throw Direction Correction")]
        [SerializeField, Range(0, 2)] private float correctThrowSphereRadius = 0.2f;
        [SerializeField, Range(1, 50)] private float correctThrowDistance = 25f;
        
        [Header("Curve After Hit Enemy")]
        [SerializeField, Range(0.1f, 5)] private float controlPointForwardFactor = 1.8f;
        [SerializeField, Range(0.1f, 5)] private float endPointForwardFactor = 0.5f;
        [SerializeField, Range(0.1f, 5)] private float enemyHeadOffset = 1.2f;
        [SerializeField, Range(0.1f, 5)] private float curveDuration = 0.4f;
        [SerializeField, Range(0.1f, 5)] private float lookAtPlayerAfterCurveDuration = 0.2f;
        [SerializeField, Range(0.1f, 5)] private float floatInAirAfterCurveDuration = 0.8f;
        [SerializeField] private Quaternion axeRotationWhenCurveIsEnd = Quaternion.Euler(-20, 140, -15);
        [SerializeField, Min(1f)] private float rotationSpeedWhenCurveIsEnd = 40f;
        [SerializeField, Range(0f, 100f)] private float maxFallingRotationSpeed = 4;
        [SerializeField, Range(0.1f, 1.5f)] private float timeToReachMaxFallingRotationSpeed = 0.8f;
        [SerializeField, Range(0f, 10f)] private float fallingAccelerationMagnitude = 1f;
        private Sequence curveAfterEnemyHitSequence = null;
        private Coroutine handleRotationDuringCurveCoroutine = null;
        private bool enemyHitDuringThrow;

        [Header("Wall Hit")]
        [SerializeField] private Vector3 capsuleCenterOffset = new Vector3(0, -0.09f, 0);
        [SerializeField] private float capsuleRadius = 0.05f;
        [SerializeField] private float capsuleHeight = 0.97f;
        [SerializeField, Min(1)] private int predictedFramesAt60FPS = 2;
        [SerializeField, Min(1)] private float axeSpeedWhenThrowEnded = 5f;
        [SerializeField, Min(0.1f)] private float axeSlowDownTime = 1.5f;
        [SerializeField] private Vector3 axeRotationToLookForward = new Vector3(0, -90, 0);
        [SerializeField, Range(0, 90)] private float lookForwardXMaxAngle = 35f;
        [SerializeField, Min(0.01f)] private float lookForwardTime = 0.25f;
        private Tween axeHitWallTween = null;
        private Coroutine rotateAfterWallHitCoroutine = null;

        [Header("Sounds Settings")]
        [SerializeField, Range(0.1f, 1)] private float catchSoundStartTime = 0.85f;
        [SerializeField, Range(0f, 2)] private float minWhooshesSoundPitch = 1.3f;
        [SerializeField, Range(0f, 1)] private float minWhooshesSoundVolume = 0.04f;
        private float maxWhooshesSoundPitch;
        private float maxWhooshesSoundVolume;

        public float InitialRotationMultiplier { get => initialRotationMultiplier; }
        public float MaxRotationSpeed { get => maxRotationSpeed; }
        public float TimeToReachMaxRotationSpeed { get => timeToReachMaxRotationSpeed; }

        private void Start()
        {
            player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG).transform;
            axeStatusHandler = GetComponent<AxeStatusHandler>();
            
            SoundCategory axeThrowWhooshes = AudioManager.Instance.FindSoundCategory(Utils.playerAxeThrowWhooshesAudioName);
            maxWhooshesSoundPitch = axeThrowWhooshes.Pitch;
            maxWhooshesSoundVolume = axeThrowWhooshes.Volume;
        }

        private void Update()
        {
            HandleAxeThrow();
            HandleAxeWhooshesSoundSettings();            
        }

        public void ThrowAxe()
        {
            transform.parent = null;
            axeStatusHandler.CurrentRotationSpeed = initialRotationMultiplier * maxRotationSpeed;

            // Axe collider is no more a physic one
            axeStatusHandler.WeaponCollider.isTrigger = true;

            // The isKinematic property is set to false to enable physics
            axeStatusHandler.Rigidbody.isKinematic = false;

            // Axe can have velocity and angular velocity different from zero.
            // It's necessary to set them to zero before throwing the axe,
            // otherwise the axe won't follow the thorw direction and its rotation
            // while throw will be wrong
            axeStatusHandler.ResetWeaponRigidbodyVelocityAndRotation();

            // Compute throw direction
            Vector3 throwDirection = GetThrowDirection();

            // Compute axe start rotation when thrown
            transform.rotation = Quaternion.LookRotation(throwDirection, Vector3.up) * axeRotationWhenThrown;

            // Apply a force to the axe in the direction of the camera, and so of the crosshair
            axeStatusHandler.Rigidbody.AddForce(axeStatusHandler.Rigidbody.mass * throwAcceleration * throwDirection, ForceMode.Impulse);

            // Apply a torque to the axe in its forward direction
            axeStatusHandler.Rigidbody.AddTorque(maxRotationSpeed * axeStatusHandler.GetWeaponForwardDirection(), ForceMode.Impulse);

            axeStatusHandler.IsThrown = true;
            axeStatusHandler.HasHitWall = false;
            enemyHitDuringThrow = false;
        }

        private Vector3 GetThrowDirection()
        {
            Camera camera = Camera.main;
            
            float GetAngleWithCharacter(Transform character)
            {
                Vector3 characterUpperChestPosition = GetUpperChestBone(character).position;
                Vector3 cameraToCharacterDirection = (characterUpperChestPosition - camera.transform.position).normalized;

                return Vector3.Angle(camera.transform.forward, cameraToCharacterDirection);
            }

            Transform GetUpperChestBone(Transform character)
            {
                Animator characterAnimator = character.GetComponent<Animator>();

                return characterAnimator.GetBoneTransform(HumanBodyBones.UpperChest);
            }


            LayerMask enemyLayer = LayerMask.GetMask(Utils.ENEMY_LAYER_NAME);
            RaycastHit[] hits = Physics.RaycastAll(camera.transform.position, camera.transform.forward, correctThrowDistance, enemyLayer, QueryTriggerInteraction.Collide)
                                        .Where(hit => 
                                        {
                                            Transform character = hit.collider.transform.root;
                                            CharacterStatusHandler characterStatusHandler = character.GetComponent<CharacterStatusHandler>();
                                            
                                            return !characterStatusHandler.IsDead && characterStatusHandler.IsHitableCollider(hit.collider);
                                        })
                                        .ToArray();

            if (hits.Length == 0)
            {
                // Search for all characters in front of the player, belonging to the enemy layer and get the throw direction
                GameObject enemy = Physics.SphereCastAll(camera.transform.position, correctThrowSphereRadius, camera.transform.forward, correctThrowDistance, enemyLayer, QueryTriggerInteraction.Collide)
                                            .Where(hit =>
                                            {
                                                Transform character = hit.collider.transform.root;
                                                CharacterStatusHandler characterStatusHandler = character.GetComponent<CharacterStatusHandler>();

                                                return !characterStatusHandler.IsDead && characterStatusHandler.IsHitableCollider(hit.collider);
                                            })
                                            .Select(hit => hit.collider.transform.root.gameObject)
                                            .Distinct()
                                            .OrderBy(gameObject => GetAngleWithCharacter(gameObject.transform))
                                            .FirstOrDefault();

                if (enemy != null)
                {
                    Vector3 enemyChest = GetUpperChestBone(enemy.transform).position;

                    return (enemyChest - transform.position).normalized;
                }
            }

            // Return the default direction (from the position of the axe towards the direction of the camera
            Vector3 endPoint = camera.transform.position + aimingOffset * camera.transform.forward;
            return (endPoint - transform.position).normalized;
        }

        private void HandleAxeWhooshesSoundSettings()
        {
            float newPitch = maxWhooshesSoundPitch * axeStatusHandler.CurrentRotationSpeed / maxRotationSpeed;
            AudioManager.Instance.SetPitch(Utils.playerAxeThrowWhooshesAudioName, gameObject, newPitch, minWhooshesSoundPitch);

            if (handleRotationDuringCurveCoroutine == null)
            {
                float newVolume = maxWhooshesSoundVolume * axeStatusHandler.CurrentRotationSpeed / maxRotationSpeed;
                AudioManager.Instance.SetVolume(Utils.playerAxeThrowWhooshesAudioName, gameObject, newVolume, minWhooshesSoundVolume);
            }
        }

        private void HandleAxeThrow()
        {
            if (!axeStatusHandler.IsAxeOnBody() && !axeStatusHandler.HasHitWall && !enemyHitDuringThrow && !axeStatusHandler.IsReturning)
            {
                HandleWallCollision();

                if (axeStatusHandler.IsThrown && !enemyHitDuringThrow)
                {
                    axeStatusHandler.CurrentRotationSpeed = Mathf.Lerp(axeStatusHandler.CurrentRotationSpeed, maxRotationSpeed, Time.deltaTime / timeToReachMaxRotationSpeed);
                    axeStatusHandler.RotateAxe();
                }
            }
        }

        private int GetNumberOfPredictedFrames()
        {
            float fps = 1f / Time.deltaTime;

            int predictedFrames = Mathf.RoundToInt(60 / fps);
            predictedFrames = Mathf.Clamp(predictedFrames, 1, predictedFrames);

            return predictedFramesAt60FPS * predictedFrames;
        }
        
        private void HandleWallCollision(float lookAheadDistance = 0)
        {
            int wallLayerMask = LayerMask.GetMask(Utils.GROUND_LAYER_NAME, Utils.OBSTACLES_LAYER_NAME);

            Vector3[] collisionInfo = GetCollisionInfo(wallLayerMask, lookAheadDistance);
            if (collisionInfo != null)
            {
                Vector3 collisionPoint = collisionInfo[0];
                Vector3 axeNearestPoint = collisionInfo[1];

                SimulateWallCollision(collisionPoint, axeNearestPoint);
            }
        }

        private Vector3[] GetCollisionInfo(LayerMask layerMask, float lookAheadDistance = 0)
        {
            if (lookAheadDistance == 0)
            {
                lookAheadDistance = GetNumberOfPredictedFrames();
            }

            Vector3 axeVelocityDirection = axeStatusHandler.Rigidbody.velocity.normalized;

            if (throwDirection == Vector3.zero && axeVelocityDirection != Vector3.zero)
            {
                throwDirection = axeVelocityDirection;
            }

            object[] axeCapsuleInfo = ComputeAxeCapsuleInfo();
            
            Vector3 capsuleStart = (Vector3)axeCapsuleInfo[0];
            Vector3 capsuleEnd = (Vector3)axeCapsuleInfo[1];
            float capsuleActualRadius = (float)axeCapsuleInfo[2];

            Vector3[] hitPoints = Physics.CapsuleCastAll(capsuleStart, capsuleEnd, capsuleActualRadius, axeVelocityDirection, lookAheadDistance, layerMask, QueryTriggerInteraction.Ignore)
                                        .Select(hit => hit.point)
                                        .ToArray();

            if (hitPoints.Length == 0)
            {
                return null;
            }
            else
            {
                Vector3[] axePoints = GetPointList(capsuleStart, capsuleEnd, numberOfIntermediatePoints: 7).ToArray();

                return ComputeNearestWallAndAxePoint(hitPoints, axePoints);
            }
        }

        private Vector3[] ComputeNearestWallAndAxePoint(Vector3[] wallPoints, Vector3[] axePoints)
        {            
            Vector3 nearestWallPoint = Vector3.zero;
            float nearestDistance = float.MaxValue;
            foreach (Vector3 wallPoint in wallPoints)
            {
                float currentDistance = Mathf.Min(axePoints.Select(point => Vector3.Distance(point, wallPoint)).ToArray());

                if (currentDistance < nearestDistance)
                {
                    nearestDistance = currentDistance;
                    nearestWallPoint = wallPoint;
                }
            }

            Vector3 axeNearestPoint = axePoints.OrderBy(point => Vector3.Distance(point, nearestWallPoint)).FirstOrDefault();

            Vector3[] results = new Vector3[2];
            results[0] = nearestWallPoint;
            results[1] = axeNearestPoint;

            return results;
        }

        private object[] ComputeAxeCapsuleInfo()
        {
            // Compute start and end points of the capsule
            float capsuleActualHeight = capsuleHeight * transform.localScale.y;
            Vector3 capsuleActualCenterOffset = capsuleCenterOffset * transform.localScale.y;
            Vector3 capsuleCenter = transform.position + capsuleActualCenterOffset;
            Vector3 capsuleStart = capsuleCenter + (transform.rotation * new Vector3(0, capsuleActualHeight / 2, 0));
            Vector3 capsuleEnd = capsuleCenter + (transform.rotation * new Vector3(0, -capsuleActualHeight / 2, 0));

            // Compute capsule radius
            float capsuleActualRadius = capsuleRadius * transform.localScale.x;
            
            return new object[] { capsuleStart, capsuleEnd, capsuleActualRadius };
        }

        private List<Vector3> GetPointList(Vector3 start, Vector3 end, int numberOfIntermediatePoints)
        {
            List<Vector3> points = new List<Vector3>();

            if (numberOfIntermediatePoints > 0 && start != end)
            {
                List<Vector3> intermediatePoints = new List<Vector3>();
                intermediatePoints.Add((start + end) / 2);

                int pointsToFind = (numberOfIntermediatePoints - 1) / 2;

                List<Vector3> leftHalfIntermediatePoints = GetPointList(start, intermediatePoints[0], pointsToFind);
                intermediatePoints.InsertRange(0, leftHalfIntermediatePoints);

                List<Vector3> rightHalfIntermediatePoints = GetPointList(intermediatePoints[intermediatePoints.Count - 1], end, pointsToFind);
                intermediatePoints.AddRange(rightHalfIntermediatePoints);

                points.AddRange(intermediatePoints);
            }

            if (points.Count == 0)
            {
                points.Add(start);
                points.Add(end);
            }
            else
            {
                points.Insert(0, start);
                points.Add(end);
            }

            return points;
        }

        private void SimulateWallCollision(Vector3 collisionPoint, Vector3 axeNearestPoint)
        {
            float distanceToWall = Vector3.Distance(axeNearestPoint, collisionPoint);
            Vector3 axeSpeed = axeStatusHandler.Rigidbody.velocity;
            float duration = distanceToWall / axeSpeed.magnitude;

            Vector3 positionToReach = transform.position + distanceToWall * axeSpeed.normalized;
            axeHitWallTween = transform.DOMove(positionToReach, duration)
                .OnComplete(() =>
                {
                    ThrowEnded();
                    ReduceAxeSpeed();
                    WallHit();
                    SimulateWallReactionToHit();
                });
        }

        private void ReduceAxeSpeed()
        {
            // Reduces axe velocity to an acceptable value to detect collisions
            Vector3 axeSpeed = axeStatusHandler.Rigidbody.velocity;
            float axeVelocityMagnitude = axeSpeed.magnitude;
            axeVelocityMagnitude = Mathf.Clamp(axeVelocityMagnitude, 0.01f, axeSpeedWhenThrowEnded);
            
            axeStatusHandler.Rigidbody.velocity = axeSpeed.normalized * axeVelocityMagnitude;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!axeStatusHandler.IsAxeOnBody() && !axeStatusHandler.WeaponCollider.isTrigger && !axeStatusHandler.IsReturning)
            {
                WallCollisionCheck(collision.collider);
            }
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (!axeStatusHandler.IsAxeOnBody() && axeStatusHandler.WeaponCollider.isTrigger && !axeStatusHandler.IsReturning)
            {
                int enemyLayerValue = LayerMask.NameToLayer(Utils.ENEMY_LAYER_NAME);

                if (otherCollider.gameObject.layer == enemyLayerValue)
                {
                    Transform character = otherCollider.transform.root;
                    CharacterStatusHandler characterHitStatusHandler = character.GetComponent<CharacterStatusHandler>();

                    if (!axeStatusHandler.HasCharacterBeenAlreadyHit(character.gameObject) && !characterHitStatusHandler.IsDead &&
                        characterHitStatusHandler.IsHitableCollider(otherCollider) && axeStatusHandler.IsThrown)
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

                        if (!enemyHitDuringThrow)
                        {
                            enemyHitDuringThrow = true;

                            // If axe hit enemy while thrown and
                            // the tween that has to bring axe to wall is not active
                            if (!axeHitWallTween.IsActive() && !curveAfterEnemyHitSequence.IsActive())
                            {
                                Vector3 startPoint = transform.position;
                                CurveAfterEnemyHit(character, startPoint);
                            }
                        }
                    }
                }
                else
                {
                    WallCollisionCheck(otherCollider);
                }
            }
        }
        
        private void WallCollisionCheck(Collider otherCollider)
        {
            if (rotateAfterWallHitCoroutine == null)
            {
                int wallLayerValue = LayerMask.NameToLayer(Utils.GROUND_LAYER_NAME);

                if (otherCollider.gameObject.layer == wallLayerValue)
                {
                    axeStatusHandler.Rigidbody.constraints = RigidbodyConstraints.None;

                    if (!axeStatusHandler.HasHitWall)
                    {
                        if (!axeHitWallTween.IsActive())
                        {
                            ThrowEnded();
                            WallHit();
                        }
                    }
                    else
                    {
                        AudioManager.Instance.PlaySoundOneShot(Utils.playerAxeWallHitAudioName, gameObject);
                    }
                }
            }
        }

        private void CurveAfterEnemyHit(Transform enemyTransform, Vector3 startPoint)
        {
            Vector3[] ComputeCurvePoints()
            {
                Vector3 instantiatePointsDirection = axeStatusHandler.Rigidbody.velocity.normalized;
                
                // Compute the Y of the end position of the curve
                float enemyHeight = enemyTransform.transform.localScale.y * enemyTransform.GetComponent<CapsuleCollider>().height;
                float endPointY = enemyTransform.transform.position.y + enemyHeight + enemyHeadOffset;

                // Compute the control point of the curve
                Vector3 controlPoint = startPoint + controlPointForwardFactor * instantiatePointsDirection;
                controlPoint.y = startPoint.y + (endPointY - startPoint.y) / 2;

                // Compute the end position of the curve
                Vector3 endPoint = controlPoint + endPointForwardFactor * instantiatePointsDirection;
                endPoint.y = endPointY;

                // Create an array of points along the curve
                return new Vector3[] { startPoint, controlPoint, endPoint };
            }

            float ComputeAxeVelocityDuringCurve(Vector3[] curvePoints, float curveDuration)
            {
                float distance = 0;
                for (int i = 0; i < curvePoints.Length - 1; i++)
                {
                    distance += Vector3.Distance(curvePoints[i], curvePoints[i + 1]);
                }

                return distance / curveDuration;
            }
            
            // Compute the points of the curve
            Vector3[] curvePoints = ComputeCurvePoints();

            // Compute axe speed during curve
            float axeSpeedDuringCurve = ComputeAxeVelocityDuringCurve(curvePoints, curveDuration);
            axeStatusHandler.Rigidbody.velocity = axeSpeedDuringCurve * axeStatusHandler.Rigidbody.velocity.normalized;
            
            // Axe rotation is handled manually, so any physics rotation is disabled
            axeStatusHandler.Rigidbody.angularVelocity = Vector3.zero;

            Vector3 lastPosition = transform.position;

            // Create a sequence curve animation with DOTween
            curveAfterEnemyHitSequence = DOTween.Sequence();
            curveAfterEnemyHitSequence.Append(transform.DOPath(curvePoints, curveDuration, PathType.CatmullRom, PathMode.Full3D, 10, null)
                           .SetEase(Ease.Linear)
                           .OnStart(() =>
                           {
                               handleRotationDuringCurveCoroutine = StartCoroutine(HandleRotationDuringCurveCoroutine());
                           })
                           .OnComplete(() =>
                           {
                               // Axe speed and rotation speed are zero in this moment
                               axeStatusHandler.ResetWeaponRigidbodyVelocityAndRotation();

                               // Axe collider is now a physic one
                               axeStatusHandler.WeaponCollider.isTrigger = false;
                           }));

            curveAfterEnemyHitSequence.Append(transform.DOMove(curvePoints[curvePoints.Length - 1] + 0.1f * Vector3.up, (lookAtPlayerAfterCurveDuration + floatInAirAfterCurveDuration)));

            curveAfterEnemyHitSequence.OnUpdate(() =>
            {
                if (!axeStatusHandler.IsAxeOnBody())
                {
                    if (!axeHitWallTween.IsActive())
                    {
                        HandleWallCollision(lookAheadDistance: 1);
                    }

                    if (axeStatusHandler.HasHitWall || axeHitWallTween.IsActive() || axeStatusHandler.IsReturning)
                    {
                        // Stop the coroutine that handle axe rotation during curve movement
                        if (handleRotationDuringCurveCoroutine != null)
                        {
                            StopCoroutine(handleRotationDuringCurveCoroutine);
                            handleRotationDuringCurveCoroutine = null;
                        }

                        curveAfterEnemyHitSequence.Kill();
                    }
                    else
                    {
                        // Update axe physics speed direction
                        Vector3 velocityDirection = (transform.position - lastPosition).normalized;
                        axeStatusHandler.Rigidbody.velocity = axeSpeedDuringCurve * velocityDirection;
                        lastPosition = transform.position;
                    }
                }
            })
            .OnComplete(() =>
            {
                if (!axeStatusHandler.HasHitWall)
                {
                    axeStatusHandler.ResetWeaponRigidbodyVelocityAndRotation();
                    ThrowEnded();

                    // Axe collider, which has been disabled during rotation coroutine, is enabled again
                    axeStatusHandler.WeaponCollider.enabled = true;

                    // Apply a force to the axe to make the fall more realistic
                    axeStatusHandler.Rigidbody.AddForce(axeStatusHandler.Rigidbody.mass * fallingAccelerationMagnitude * Vector3.down, ForceMode.Impulse);

                    // Apply the maxFallingRotationSpeed in 2 parts: the first is assigned to angular velocity, in order to give to the axe an initial rotation speed,
                    // the second is used to add the remaining part of maxFallingRotationSpeed to the axe as a torque.
                    Vector3 physicsRotationSpeed = maxFallingRotationSpeed * axeStatusHandler.GetWeaponForwardDirection();
                    
                    axeStatusHandler.Rigidbody.angularVelocity = 0.2f * physicsRotationSpeed;
                    axeStatusHandler.Rigidbody.AddTorque(0.8f * physicsRotationSpeed, ForceMode.Impulse);
                }
            });

            curveAfterEnemyHitSequence.Play();
        }

        private IEnumerator HandleRotationDuringCurveCoroutine()
        {
            float startTime = 0.25f * curveDuration;

            float timeElapsed = 0;
            while (timeElapsed < startTime)
            {
                timeElapsed += Time.deltaTime;

                axeStatusHandler.RotateAxe();

                yield return null;
            }

            // Compute time intervals to handle rotation
            float rotateAxeDuration = curveDuration - startTime;
            float totalDuration = lookAtPlayerAfterCurveDuration + rotateAxeDuration;

            // The look at direction if from player to axe
            Vector3 lookAtDirectionDirection = (transform.position - player.transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lookAtDirectionDirection) * axeRotationWhenCurveIsEnd;

            AudioManager.Instance.StopSoundWithFadeOut(Utils.playerAxeThrowWhooshesAudioName, gameObject, 0.85f * rotateAxeDuration);

            timeElapsed = 0;
            while (timeElapsed < totalDuration)
            {
                timeElapsed += Time.deltaTime;

                if (timeElapsed < rotateAxeDuration)
                {
                    axeStatusHandler.CurrentRotationSpeed = Mathf.Lerp(axeStatusHandler.CurrentRotationSpeed, rotationSpeedWhenCurveIsEnd, Time.deltaTime / rotateAxeDuration);
                    axeStatusHandler.RotateAxe();
                }
                // Axe rotation stops after this moment, so its trigger is disabled
                else
                {
                    axeStatusHandler.CurrentRotationSpeed = rotationSpeedWhenCurveIsEnd;
                    axeStatusHandler.WeaponCollider.enabled = false;
                }

                if (timeElapsed >= rotateAxeDuration)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1.15f * Time.deltaTime / lookAtPlayerAfterCurveDuration);
                }

                yield return null;
            }

            timeElapsed = 0;
            while (timeElapsed < floatInAirAfterCurveDuration)
            {
                timeElapsed += Time.deltaTime;

                axeStatusHandler.RotateAxe();

                yield return null;
            }

            axeStatusHandler.CurrentRotationSpeed = 0;

            handleRotationDuringCurveCoroutine = null;
        }

        public void ThrowEnded()
        {
            if (axeStatusHandler.IsThrown)
            {
                throwDirection = Vector3.zero;
                axeStatusHandler.IsThrown = false;

                AudioManager.Instance.StopSound(Utils.playerAxeThrowWhooshesAudioName, gameObject);
            }
        }

        private void WallHit()
        {
            if (!axeStatusHandler.HasHitWall)
            {
                DOTween.Kill(transform);

                // Axe collider is now a physics one
                axeStatusHandler.WeaponCollider.isTrigger = false;
                axeStatusHandler.HasHitWall = true;
                axeStatusHandler.CurrentRotationSpeed = 0;

                AudioManager.Instance.PlaySoundOneShot(Utils.playerAxeWallHitAudioName, gameObject);
            }
        }

        private void SimulateWallReactionToHit()
        {
            Vector3 ComputeReactionVelocity(Vector3 wallNormal)
            {
                // Compute reaction velocity direction
                Vector3 reactionVelocityDirection = Vector3.Reflect(axeStatusHandler.Rigidbody.velocity.normalized, wallNormal);
                reactionVelocityDirection = reactionVelocityDirection.normalized;

                // Compute incident and reflection wannl angles
                float incidentAngle = Vector3.Angle(axeStatusHandler.Rigidbody.velocity.normalized, wallNormal);
                float reflectionAngle = Vector3.Angle(reactionVelocityDirection, wallNormal);

                // Compute reaction velocity using motion's conservation law
                float reactionVelocityMagnitude = axeStatusHandler.Rigidbody.velocity.magnitude * Mathf.Cos(incidentAngle * Mathf.Deg2Rad) / Mathf.Cos(reflectionAngle * Mathf.Deg2Rad);
                reactionVelocityMagnitude = Mathf.Abs(reactionVelocityMagnitude);

                // Reduce randomly reaction velocity magnitude in order to get a more realistic reaction
                return Random.Range(0.75f, 1f) * reactionVelocityMagnitude * reactionVelocityDirection;
            }

            int wallLayerMask = LayerMask.GetMask(Utils.GROUND_LAYER_NAME, Utils.OBSTACLES_LAYER_NAME);

            float maxDistance = 10f;
            if (Physics.Raycast(transform.position, axeStatusHandler.Rigidbody.velocity.normalized, out RaycastHit hitInfo, maxDistance, wallLayerMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 wallNormal = hitInfo.normal.normalized;

                // Compute and apply reaction velocity
                axeStatusHandler.Rigidbody.velocity = ComputeReactionVelocity(wallNormal);

                // Rotate axe with a coroutine, to match reaction velocity direction
                rotateAfterWallHitCoroutine = StartCoroutine(HandleRotationAfterWallHitCoroutine(axeStatusHandler.Rigidbody.velocity.normalized, lookForwardTime));
            }
        }

        private IEnumerator HandleRotationAfterWallHitCoroutine(Vector3 direction, float duration)
        {
            // Add constraints to the axe rotation, so that physics rotations will only affacts the Z axis (that is its forward direction)
            axeStatusHandler.Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

            // Compute rotation towards velocity direction
            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);

            // Add randomness to the rotation that axe will have after hitting the wall
            float randomXAxisAngle = Random.Range(-lookForwardXMaxAngle, lookForwardXMaxAngle);

            // Compute the rotation to add to the one computed with LookRotation, so that axe will actually look forward after hitting the wall
            Quaternion rotationToAdd = Quaternion.Euler(randomXAxisAngle, axeRotationToLookForward.y, axeRotationToLookForward.z);

            Quaternion initialRotation = transform.rotation;
            Quaternion targetRotation = lookRotation * rotationToAdd;

            float timeElapsed = 0;
            while (timeElapsed <= duration)
            {
                transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, Mathf.Clamp01(timeElapsed / duration));
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            // Remove any torque applied before
            axeStatusHandler.Rigidbody.angularVelocity = Vector3.zero;

            // Apply wall reaction torque to the axe
            axeStatusHandler.Rigidbody.AddTorque(maxRotationSpeed * axeStatusHandler.GetWeaponForwardDirection(), ForceMode.Impulse);

            rotateAfterWallHitCoroutine = null;
        }
    }
}