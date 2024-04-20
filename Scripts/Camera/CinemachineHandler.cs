// Author: Pietro Vitagliano

using System.Collections;
using UnityEngine;
using Cinemachine;
using System;

namespace MysticAxe
{
    public class CinemachineHandler : MonoBehaviour
    {        
        private Transform player;
        private PlayerStatusHandler playerStatusHandler;
        private PlayerAnimatorHandler playerAnimatorHandler;

        private LockOnHandler lockOnHandler;

        private CinemachineVirtualCamera exploringCamera;
        private CinemachineVirtualCamera aimingCamera;
        private CinemachineVirtualCamera targetingCamera;
        private CinemachinePOV exploringCameraPOV, aimingCameraPOV;
        private CinemachineFramingTransposer targetingCameraFramingTransposer;
        private CinemachineComposer targetingCameraComposer;
        
        [Header("Camera Aiming")]
        [SerializeField, Range(0.01f, 0.5f)] private float timeToAlignToTargetingCamera = 0.05f;
        private Transform aimingTargetForBodyOrientation;
        private Vector3 targetLocalInitialPosition;

        [Header("Camera Targeting")]
        [SerializeField, Min(0.1f)] private float targetingCameraAligningTime = 0.4f;
        [SerializeField] private Vector3 targetingFramingTransposerWhenTargetingOff = new Vector3(0f, 1.25f, 0);
        [SerializeField] private Vector3 targetingComposerWhenTargetingOff = new Vector3(3.5f, 0, 100);
        [SerializeField, Range(0.1f, 2)] private float framingTransposerMaxValue = 1.35f;
        [SerializeField, Range(0.1f, 2)] private float lockOnHeightThreshold = 0.4f;

        [Header("Cinemachine Confiner Settings")]
        [SerializeField] private Vector3 boxColliderCenter = new Vector3(0, 2.15f, 0);
        [SerializeField] private Vector3 boxColliderSize = new Vector3(6.5f, 4f, 6.5f);

        [Header("Camera Zoom")]
        [SerializeField, Range(1, 20)] private float incrementInFOV = 12;
        private float baseCameraFOV, maxCameraFOVFastRun;
        
        [Header("Camera Reset")]
        [SerializeField, Min(0.1f)] private float resetTime = 0.4f;
        [SerializeField, Min(0.1f)] private float timeToZoom = 0.4f;
        private Vector2 initialAxisValues;
        private bool isCameraResetting;

        private RectTransform canvasCrosshair;
        private RectTransform canvasLockOnTarget;

        public CinemachineVirtualCamera ExploringCamera { get => exploringCamera; }
        public CinemachineVirtualCamera AimingCamera { get => aimingCamera; }
        public CinemachineVirtualCamera TargetingCamera { get => targetingCamera; }

        public bool IsCameraResetting { get => isCameraResetting; }

        
        private void Start()
        {
            lockOnHandler = GetComponent<LockOnHandler>();

            player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG).transform;
            playerStatusHandler = player.GetComponent<PlayerStatusHandler>();
            playerAnimatorHandler = player.GetComponent<PlayerAnimatorHandler>();

            canvasCrosshair = GameObject.FindGameObjectWithTag(Utils.CANVAS_CROSSHAIR_TAG).GetComponent<RectTransform>();
            canvasLockOnTarget = FindObjectOfType<LockOnHandler>().CanvasLockOnTarget;

            aimingTargetForBodyOrientation = GameObject.FindGameObjectWithTag(Utils.TARGET_WHILE_AIMING_TAG).transform;

            InitializeCMCamerasParameters();
            InitializeCinemachineConfiners();
            
            initialAxisValues = new Vector2(Utils.GetUnsignedEulerAngle(exploringCameraPOV.m_HorizontalAxis.Value), exploringCameraPOV.m_VerticalAxis.Value);

            // Reset exploring camera at the beginning
            ResetCamera();

            baseCameraFOV = exploringCamera.m_Lens.FieldOfView;
            maxCameraFOVFastRun = baseCameraFOV + incrementInFOV;

            targetLocalInitialPosition = aimingTargetForBodyOrientation.localPosition;
        }

        private void Update()
        {
            HandleCameraSwitching();
            HandleResetCamera();
            HandleZoomWhileRunning();
            HandleAimingTargetWhileAiming();
            HandleCanvasCrosshairVisibility();
            HandleCanvasLockOnVisibility();
        }

        private IEnumerator ResetTargetingWhenTargetingOff()
        {
            // Wait until the transition has completed
            while (CinemachineCore.Instance.IsLive(targetingCamera))
            {
                yield return null;
            }

            targetingCamera.LookAt = player;
            targetingCameraFramingTransposer.m_TrackedObjectOffset = targetingFramingTransposerWhenTargetingOff;
            targetingCameraComposer.m_TrackedObjectOffset = targetingComposerWhenTargetingOff;
        }

        private IEnumerator ResetExploringCameraWhenTargetingOn()
        {
            // Wait until the transition has completed
            while (CinemachineCore.Instance.IsLive(exploringCamera))
            {
                yield return null;
            }

            // Reset camera until player is targeting something
            while (playerStatusHandler.IsTargetingEnemy)
            {
                Vector2 resetAxisValues = ComputeResetAxisValues();

                exploringCameraPOV.m_HorizontalAxis.Value = resetAxisValues.x;
                exploringCameraPOV.m_VerticalAxis.Value = resetAxisValues.y;
                
                yield return null;
            }
        }

        private void AlignAimingCameraToTargetingCameraLookAt()
        {
            Vector3 aimingCameraToLockOnDirection = targetingCamera.LookAt.position - aimingCamera.transform.position;
            Vector3 newAngles = Quaternion.LookRotation(aimingCameraToLockOnDirection, Vector3.up).eulerAngles;

            // DEBUG CODE HERE
            //Debug.DrawRay(aimingCamera.transform.position, 2 * aimingCamera.transform.forward, Color.red, 0.1f);
            //Debug.DrawRay(aimingCamera.transform.position, aimingCameraToLockOnDirection, Color.green, 0.1f);

            // Compute newHorizontalValue
            float newHorizontalValue = Utils.GetSignedEulerAngle(newAngles.y);

            // Compute newVerticalValue
            // Note that it's clamped between aimingVirtualCameraPOV m_MinimumValue and m_MaximumValue
            float newVerticalValue = Utils.GetSignedEulerAngle(newAngles.x);
            newVerticalValue = Mathf.Clamp(newVerticalValue, aimingCameraPOV.m_VerticalAxis.m_MinValue, aimingCameraPOV.m_VerticalAxis.m_MaxValue);

            // Create a Vector2 with the new values
            Vector2 newAxisValues = new Vector2(newHorizontalValue, newVerticalValue);

            // Gradually assign the new values
            AlignCameraPOV(ref aimingCameraPOV, newAxisValues, timeToAlignToTargetingCamera);
        }

        private void AlignTargetingCameraWithEnemyHeight()
        {
            Vector3[] alignmentInfo = ComputeTargetingCameraAlignmentInfo();
            Vector3 newFramingTransposer = alignmentInfo[0];
            Vector3 newComposer = alignmentInfo[1];

            // Instantly assign new values
            if (lockOnHandler.IsSwitchingTarget)
            {
                targetingCameraFramingTransposer.m_TrackedObjectOffset = newFramingTransposer;
                targetingCameraComposer.m_TrackedObjectOffset = newComposer;
            }
            // Gradually assign new values
            else
            {
                // Vertical Framing Transposer Alignment
                Vector3 currentFramingTransposer = targetingCameraFramingTransposer.m_TrackedObjectOffset;
                targetingCameraFramingTransposer.m_TrackedObjectOffset = Vector3.Lerp(currentFramingTransposer, newFramingTransposer, Time.deltaTime / targetingCameraAligningTime);

                // Vertical Composer Alignment
                Vector3 currentComposer = targetingCameraComposer.m_TrackedObjectOffset;
                targetingCameraComposer.m_TrackedObjectOffset = Vector3.Lerp(currentComposer, newComposer, Time.deltaTime / targetingCameraAligningTime);
            }
        }

        private Vector3[] ComputeTargetingCameraAlignmentInfo()
        {
            // Lerp the value so that it is 0 when the target is at the max distance
            // and it is equal to itself when the target is at a distance equal to 0
            float LerpOnTargetDistance(Transform target, float valueToLerp)
            {
                float playerDistanceFromTarget = Vector3.Distance(player.position, target.position);

                return valueToLerp * Mathf.Abs(playerDistanceFromTarget - lockOnHandler.MaxDistanceToTarget) / lockOnHandler.MaxDistanceToTarget;
            }

            // Get the current target
            Transform target = canvasLockOnTarget.parent;

            // Compute player distance from target
            float playerDistanceFromTarget = Vector3.Distance(player.position, target.position);

            // Initialize newYFramingTransposer and newYComposer
            float newYFramingTransposer = targetingFramingTransposerWhenTargetingOff.y;
            float newYComposer = targetingComposerWhenTargetingOff.y;

            // Update newYFramingTransposer and newYComposer taking into account player distance from target
            // and target height relative to the player
            float valueToNormalize;
            if (playerDistanceFromTarget <= lockOnHandler.MaxDistanceToTarget)
            {
                float playerHeadHeight = Utils.GetCharacterHeightPosition(player.transform, Utils.HEAD_HEIGHT).y;
                float lockOnHeight = canvasLockOnTarget.transform.position.y;

                // Player and enemy have a significant difference in height
                if (Mathf.Abs(playerHeadHeight - lockOnHeight) > lockOnHeightThreshold)
                {
                    Debug.Log("Player and enemy have a significant difference in height");

                    valueToNormalize = (playerHeadHeight - lockOnHeight) / 2;
                    
                    newYFramingTransposer -= LerpOnTargetDistance(target, valueToNormalize);
                    newYComposer += LerpOnTargetDistance(target, valueToNormalize);
                }
                // Player and enemy have a small difference in height
                else
                {
                    Debug.Log("Player and enemy have a small difference in height");

                    valueToNormalize = Mathf.Abs(playerHeadHeight - lockOnHeight);
                    newYFramingTransposer -= LerpOnTargetDistance(target, valueToNormalize);

                    valueToNormalize = (playerHeadHeight - lockOnHeight) / 3f;
                    newYComposer += LerpOnTargetDistance(target, valueToNormalize);
                }

                // Clamp the max value that newYFramingTransposer and can assume
                newYFramingTransposer = Mathf.Min(newYFramingTransposer, framingTransposerMaxValue);
            }

            Vector3[] results = new Vector3[2];
            results[0] = newYFramingTransposer * Vector3.up;
            results[1] = newYComposer * Vector3.up;

            return results;
        }
        
        private void HandleCameraSwitching()
        {   
            // Player is aiming
            if (playerStatusHandler.IsAiming)
            {
                aimingCamera.Priority = 2;

                if (playerStatusHandler.IsTargetingEnemy)
                {
                    targetingCamera.Priority = 1;
                    exploringCamera.Priority = 0;
                }
                else
                {
                    exploringCamera.Priority = 1;
                    targetingCamera.Priority = 0;
                }

                // Align exploration camera to aiming camera
                exploringCameraPOV.m_HorizontalAxis.Value = aimingCameraPOV.m_HorizontalAxis.Value;
                exploringCameraPOV.m_VerticalAxis.Value = aimingCameraPOV.m_VerticalAxis.Value;
            }
            // Player is not aiming
            else
            {
                aimingCamera.Priority = 1;

                // It could be possible that even if player is targeting, canvasLockOnTarget.parent is null
                // (for example when an enemy is killed and the canvas is deattached).
                // This is why its is also checked if canvasLockOnTarget.parent != null
                if (playerStatusHandler.IsTargetingEnemy && canvasLockOnTarget.parent != null)
                {
                    targetingCamera.Priority = 2;
                    exploringCamera.Priority = 0;

                    // Targeting On First Time
                    if (targetingCamera.LookAt == player)
                    {
                        targetingCamera.LookAt = canvasLockOnTarget;
                        targetingCameraComposer.m_TrackedObjectOffset = Vector3.zero;

                        StartCoroutine(ResetExploringCameraWhenTargetingOn());
                    }
                    else
                    {
                        AlignAimingCameraToTargetingCameraLookAt();
                        AlignTargetingCameraWithEnemyHeight();
                    }
                }
                else
                {
                    exploringCamera.Priority = 2;
                    targetingCamera.Priority = 0;

                    // Targeting Off First Time
                    if (targetingCamera.LookAt != player)
                    {
                        StartCoroutine(ResetTargetingWhenTargetingOff());
                    }
                    else
                    {
                        // Align aiming camera to exploration camera
                        aimingCameraPOV.m_HorizontalAxis.Value = exploringCameraPOV.m_HorizontalAxis.Value;
                        aimingCameraPOV.m_VerticalAxis.Value = exploringCameraPOV.m_VerticalAxis.Value;
                    }
                }
            }
        }
        
        private Vector2 ComputeResetAxisValues()
        {
            float playerEulerRotationYAxis = Utils.GetUnsignedEulerAngle(player.transform.eulerAngles.y);
            
            return new Vector2(initialAxisValues.x + playerEulerRotationYAxis, initialAxisValues.y);
        }
        
        private void HandleResetCamera()
        {
            bool targetingCondition = !playerStatusHandler.IsTargetingEnemy && canvasLockOnTarget.parent == null;
            if (!playerStatusHandler.IsAiming && targetingCondition && InputHandler.Instance.ResetCamera)
            {
                InputHandler.Instance.ResetCamera = false;
                
                ResetCamera(resetTime, enableInputAfterReset: true);
            }
        }

        public void ResetCamera()
        {
            Vector2 resetAxisValues = ComputeResetAxisValues();
            AlignCameraPOV(exploringCameraPOV, resetAxisValues);
        }

        public void ResetCamera(float resetDuration, bool enableInputAfterReset)
        {
            if (!isCameraResetting)
            {
                StartCoroutine(ResetCameraCoroutine(exploringCameraPOV, resetDuration, enableInputAfterReset));
            }
        }

        private IEnumerator ResetCameraCoroutine(CinemachinePOV cameraPOV, float resetDuration, bool enableInputAfterReset = true)
        {
            isCameraResetting = true;
            EnableCameraInput(enabled: false);

            // Compute axis valus to reset the camera. Along the Y-Axis, the camera will always reset at 50%
            // of its max height (even if player is on a sloping floor)
            Vector2 resetAxisValues = ComputeResetAxisValues();
            Vector2 startAxisValue = new Vector2(cameraPOV.m_HorizontalAxis.Value, cameraPOV.m_VerticalAxis.Value);
            
            float timeElapsed = 0;
            while (timeElapsed < resetDuration)
            {
                float tempLerpedHorizontalValue = Mathf.LerpAngle(startAxisValue.x, resetAxisValues.x, Mathf.Clamp01(timeElapsed / resetDuration));
                cameraPOV.m_HorizontalAxis.Value = Utils.GetUnsignedEulerAngle(tempLerpedHorizontalValue);
                cameraPOV.m_VerticalAxis.Value = Mathf.LerpAngle(startAxisValue.y, resetAxisValues.y, Mathf.Clamp01(timeElapsed / resetDuration));

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            AlignCameraPOV(cameraPOV, resetAxisValues);

            isCameraResetting = false;

            if (enableInputAfterReset)
            {
                EnableCameraInput(enabled: true);
            }
        }

        private void AlignCameraPOV(CinemachinePOV cameraPOV, Vector2 newAxisValues)
        {
            cameraPOV.m_HorizontalAxis.Value = Utils.GetUnsignedEulerAngle(newAxisValues.x);
            cameraPOV.m_VerticalAxis.Value = newAxisValues.y;
        }

        private void AlignCameraPOV(ref CinemachinePOV cameraPOV, Vector2 newAxisValues, float lerpTime)
        {
            float tempLerpedHorizontalValue = Mathf.LerpAngle(cameraPOV.m_HorizontalAxis.Value, newAxisValues.x, Time.deltaTime / lerpTime);
            cameraPOV.m_HorizontalAxis.Value = Utils.GetUnsignedEulerAngle(tempLerpedHorizontalValue);
            cameraPOV.m_VerticalAxis.Value = Mathf.LerpAngle(cameraPOV.m_VerticalAxis.Value, newAxisValues.y, Time.deltaTime / lerpTime);
        }
        
        public void EnableCameraInput(bool enabled)
        {
            exploringCamera.GetComponent<CinemachineInputProvider>().enabled = enabled;
            aimingCamera.GetComponent<CinemachineInputProvider>().enabled = enabled;
        }

        private void HandleZoomWhileRunning()
        {
            if (!playerStatusHandler.IsAiming)
            {
                float currentFOV = exploringCamera.m_Lens.FieldOfView;
                float targetFOV = playerStatusHandler.IsRunningFast ? maxCameraFOVFastRun : baseCameraFOV;

                float delta = Mathf.Abs(maxCameraFOVFastRun - baseCameraFOV);
                exploringCamera.m_Lens.FieldOfView = Utils.FloatInterpolation(currentFOV, targetFOV, Time.deltaTime * delta / timeToZoom);
            }
        }

        private void HandleAimingTargetWhileAiming()
        {
            //DEBUG CODE HERE
            /*Debug.Log("Debug HandleTargetWhileAiming cameraPositionTargetRelative.y: " + cameraPositionPlayerRelative.y);
            Debug.Log("Debug HandleTargetWhileAiming targetLocalInitialPosition.y: " + targetLocalInitialPosition.y);
            Debug.Log("Debug HandleTargetWhileAiming cameraYDistanceFromTargetInitialPosition: " + cameraYDistanceFromTargetInitialPosition);
            Debug.Log("Debug HandleTargetWhileAiming newTargetLocalY: " + newTargetLocalY);
            Debug.Log("Debug HandleTargetWhileAiming target.localPosition.y: " + target.localPosition.y);*/

            // Get the camera position locally to player
            Vector3 cameraPositionPlayerRelative = player.InverseTransformPoint(Camera.main.transform.position);

            // Get the camera distance from target initial local position (its a player's child)
            float cameraYDistanceFromTargetInitialPosition = cameraPositionPlayerRelative.y - targetLocalInitialPosition.y;

            // Compute the new target local position based on the camera's distance from target initial local position
            // The minus is needed, because when the camera goes up, target goes down and vice versa
            float newTargetLocalY = targetLocalInitialPosition.y - cameraYDistanceFromTargetInitialPosition;

            // Create a new target local position based on the new Y value
            Vector3 newTargetLocalPosition = new Vector3(targetLocalInitialPosition.x, newTargetLocalY, targetLocalInitialPosition.z);

            // Assign the new local position
            //targetForBodyOrientation.localPosition = newTargetLocalPosition;

            Transform playerHead = playerAnimatorHandler.Animator.GetBoneTransform(HumanBodyBones.Head);

            Vector3 aimingCameraToPlayerHeadDirection = playerHead.position - aimingCamera.transform.position;
            float aimingCameraToPlayerHeadDistance = Vector3.Distance(playerHead.position, aimingCamera.transform.position);
            
            Vector3 newTargetPosition = aimingCamera.transform.position + 2 * aimingCameraToPlayerHeadDistance * aimingCameraToPlayerHeadDirection;
            aimingTargetForBodyOrientation.position = new Vector3(aimingTargetForBodyOrientation.position.x, newTargetPosition.y, aimingTargetForBodyOrientation.position.z);
        }
        
        private void HandleCanvasCrosshairVisibility()
        {
            CanvasFadeHandler canvasCrosshairFadeScript = canvasCrosshair.GetComponent<CanvasFadeHandler>();
            if (playerStatusHandler.IsAiming)
            {
                canvasCrosshairFadeScript.Show();
            }
            else
            {
                canvasCrosshairFadeScript.Hide();
            }
        }

        private void HandleCanvasLockOnVisibility()
        {
            CanvasFadeHandler camvasLockOnFadeScript = canvasLockOnTarget.GetComponent<CanvasFadeHandler>();

            if (playerStatusHandler.IsTargetingEnemy)
            {
                camvasLockOnFadeScript.Show();
            }
            else
            {
                camvasLockOnFadeScript.Hide();
            }
        }

        private void InitializeCMCamerasParameters()
        {            
            exploringCamera = GameObject.FindGameObjectWithTag(Utils.EXPLORING_CAMERA_TAG).GetComponent<CinemachineVirtualCamera>();
            exploringCamera.Follow = player;
            exploringCamera.LookAt = player;
            exploringCameraPOV = exploringCamera.GetCinemachineComponent<CinemachinePOV>();
            
            aimingCamera = GameObject.FindGameObjectWithTag(Utils.AIMING_CAMERA_TAG).GetComponent<CinemachineVirtualCamera>();
            aimingCamera.Follow = player;
            aimingCamera.LookAt = player;
            aimingCameraPOV = aimingCamera.GetCinemachineComponent<CinemachinePOV>();

            targetingCamera = GameObject.FindGameObjectWithTag(Utils.TARGETING_CAMERA_TAG).GetComponent<CinemachineVirtualCamera>();
            targetingCamera.Follow = player;
            targetingCamera.LookAt = player;
            targetingCameraFramingTransposer = targetingCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            targetingCameraComposer = targetingCamera.GetCinemachineComponent<CinemachineComposer>();
            targetingCameraFramingTransposer.m_TrackedObjectOffset = targetingFramingTransposerWhenTargetingOff;
            targetingCameraComposer.m_TrackedObjectOffset = targetingComposerWhenTargetingOff;
        }

        private void InitializeCinemachineConfiners()
        {
            BoxCollider boxCollider = player.gameObject.AddComponent<BoxCollider>();
            boxCollider.center = boxColliderCenter;
            boxCollider.size = boxColliderSize;
            boxCollider.isTrigger = true;

            exploringCamera.GetComponent<CinemachineConfiner>().m_BoundingVolume = boxCollider;
            aimingCamera.GetComponent<CinemachineConfiner>().m_BoundingVolume = boxCollider;
            targetingCamera.GetComponent<CinemachineConfiner>().m_BoundingVolume = boxCollider;
        }
    }
}