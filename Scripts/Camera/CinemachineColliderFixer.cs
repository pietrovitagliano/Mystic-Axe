// Author: Pietro Vitagliano

using Cinemachine;
using UnityEngine;

namespace MysticAxe
{
    public class CinemachineColliderFixer : MonoBehaviour
    {
        private CinemachineVirtualCamera targetingCamera;
        private CinemachineCollider targetingCinemachineCollider;
        
        private Transform player;
        private PlayerStatusHandler playerStatusHandler;
        private RectTransform canvasLockOnTarget;
        private LayerMask targetingTransparentLayersMask;
        private float minimumTargetDistance;
        
        private void Start()
        {
            player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG).transform;
            playerStatusHandler = player.GetComponent<PlayerStatusHandler>();
            
            canvasLockOnTarget = FindObjectOfType<LockOnHandler>().CanvasLockOnTarget;
            
            targetingCamera = FindObjectOfType<CinemachineHandler>().TargetingCamera;
            targetingCinemachineCollider = targetingCamera.GetComponent<CinemachineCollider>();
            targetingTransparentLayersMask = targetingCinemachineCollider.m_TransparentLayers;
            minimumTargetDistance = targetingCinemachineCollider.m_MinimumDistanceFromTarget;
        }

        private void Update()
        {
            HandleTargetingCameraTransparentLayers();
            HandleTargetingCameraMinDistanceFromTarget();
        }

        private void HandleTargetingCameraTransparentLayers()
        {
            if (playerStatusHandler.IsTargetingEnemy || CinemachineCore.Instance.IsLive(targetingCamera))
            {
                targetingCinemachineCollider.m_TransparentLayers = LayerMask.GetMask();
            }
            else
            {
                targetingCinemachineCollider.m_TransparentLayers = targetingTransparentLayersMask;
            }
        }

        private void HandleTargetingCameraMinDistanceFromTarget()
        {
            targetingCinemachineCollider.m_MinimumDistanceFromTarget = minimumTargetDistance;

            if (playerStatusHandler.IsTargetingEnemy && canvasLockOnTarget.parent != null)
            {
                Vector3 playerPosition = player.transform.position;
                Vector3 targetPosition = canvasLockOnTarget.parent.position;

                // Add distance from player to target
                targetingCinemachineCollider.m_MinimumDistanceFromTarget += Vector3.Distance(targetPosition, playerPosition);
            }
        }
    }
}