// Author: Pietro Vitagliano

using System;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    public class Targetable : MonoBehaviour
    {
        private Transform player;
        private RectTransform canvasLockOnTarget;
        private RectTransform targetUIStats;

        [Serializable]
        private class CanvasTransformInfo
        {
            [SerializeField, Range(0.1f, 1)] private float scaleFactor = 1;
            [SerializeField, Range(0.1f, 1)] private float minScale = 1;
            [SerializeField, Range(1f, 3)] private float maxScale = 1;

            public CanvasTransformInfo(float scaleFactor, float minScale, float maxScale)
            {
                this.scaleFactor = scaleFactor;
                this.minScale = minScale;
                this.maxScale = maxScale;
            }

            public float ScaleFactor { get => scaleFactor; }
            public float MinScale { get => minScale; }
            public float MaxScale { get => maxScale; }
        }

        [Header("Targeting Variables")]
        [SerializeField, Range(0.01f, 1)] private float enemyStatsHeadOffset = 0.05f;
        [SerializeField] private CanvasTransformInfo lockOnInfo = new CanvasTransformInfo(0.18f, 0.9f, 1.8f);
        [SerializeField] private CanvasTransformInfo enemyUIStatsInfo = new CanvasTransformInfo(0.16f, 0.9f, 1.6f);

        private bool isTargetedByPlayer = false;

        private void Start()
        {
            player = GameObject.FindWithTag(Utils.PLAYER_TAG).transform;
            canvasLockOnTarget = FindObjectOfType<LockOnHandler>().CanvasLockOnTarget;

            targetUIStats = GetComponentsInChildren<RectTransform>()
                            .Where(rectTransform => rectTransform.CompareTag(Utils.BASE_ENEMY_STATS_TAG))
                            .FirstOrDefault();            
        }

        private void Update()
        {
            isTargetedByPlayer = canvasLockOnTarget.IsChildOf(transform);

            HandleLockOnTargetAppearance();
            HandleTargetUIStatsAppearence();
        }

        private void HandleLockOnTargetAppearance()
        {
            if (isTargetedByPlayer)
            {
                // Compute lock on target scale based on distance to player
                canvasLockOnTarget.rotation = ComputeRotationRelativeToCamera(canvasLockOnTarget);
                canvasLockOnTarget.localScale = ComputeScaleBasedOnPlayerDistance(lockOnInfo);

                // Compute and assign lock on target position
                canvasLockOnTarget.position = Utils.GetCharacterHeightPosition(transform, Utils.UPPER_CHEST_HEIGHT);
            }
        }

        private void HandleTargetUIStatsAppearence()
        {
            if (targetUIStats != null && targetUIStats.GetComponent<CanvasGroup>().alpha > 0)
            {
                // Compute targetStats scale based on distance to player
                targetUIStats.rotation = ComputeRotationRelativeToCamera(targetUIStats, perpendicularToGround: true);
                targetUIStats.localScale = ComputeScaleBasedOnPlayerDistance(enemyUIStatsInfo);

                // Compute target stats position
                targetUIStats.position = Utils.GetCharacterHeightPosition(transform, Utils.FULL_HEIGHT);
                targetUIStats.position += enemyStatsHeadOffset * Vector3.up;
            }
        }

        private Vector3 ComputeScaleBasedOnPlayerDistance(CanvasTransformInfo info)
        {
            float playerToTargetDistance = Vector3.Distance(player.position, transform.position);
            float newScale = info.ScaleFactor * playerToTargetDistance;
            newScale = Mathf.Clamp(newScale, info.MinScale, info.MaxScale);
            
            return newScale * Vector3.one;
        }
        
        private Quaternion ComputeRotationRelativeToCamera(RectTransform uiRectTransform, bool perpendicularToGround = false)
        {
            Camera camera = Camera.main;
            
            // The rect transform has to look at in the direction that goes from the camera to the enemy
            Vector3 rotationDirection = uiRectTransform.position - camera.transform.position;
            rotationDirection = perpendicularToGround ? Vector3.ProjectOnPlane(rotationDirection, Vector3.up) : rotationDirection;
            
            return Quaternion.LookRotation(rotationDirection);
        }
    }
}
