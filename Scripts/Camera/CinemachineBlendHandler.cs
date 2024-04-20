// Author: Pietro Vitagliano

using System;
using UnityEngine;
using Cinemachine;
using static Cinemachine.CinemachineBlenderSettings;
using static Cinemachine.CinemachineBlendDefinition;

namespace MysticAxe
{
    public class CinemachineBlendHandler : MonoBehaviour
    {
        [Serializable]
        private class CinemachineBlendInfo
        {
            [SerializeField] private CinemachineVirtualCamera fromVirtualCamera;
            [SerializeField] private CinemachineVirtualCamera toVirtualCamera;
            [SerializeField] private Style style;
            [SerializeField] private float blendTime;
            
            public CinemachineBlendInfo(CinemachineVirtualCamera fromVirtualCamera, CinemachineVirtualCamera toVirtualCamera, Style style, float blendTime)
            {
                this.fromVirtualCamera = fromVirtualCamera;
                this.toVirtualCamera = toVirtualCamera;
                this.style = style;
                this.blendTime = blendTime;
            }

            public CinemachineVirtualCamera FromVirtualCamera { get => fromVirtualCamera; }
            public CinemachineVirtualCamera ToVirtualCamera { get => toVirtualCamera; }
            public Style Style { get => style; }
            public float BlendTime { get => blendTime; }

            public CinemachineBlendDefinition GetBlendDefinition () => new CinemachineBlendDefinition(style, blendTime);
        }

        [Header("Cinemachine Blend Info")]
        private CinemachineBrain cinemachineBrain;
        private CinemachineVirtualCamera exploringCamera;
        private CinemachineVirtualCamera targetingCamera;

        [Header("Targeting to Exploring Camera Default Settings")]
        [SerializeField] private Style defaultStyle = Style.EaseInOut;
        [SerializeField, Min(0.1f)] private float defaultBlendTime = 0.4f;
        [SerializeField, Min(0.1f)] private float runningBlendTime = 0.7f;
        private CinemachineBlendInfo defaultTargetingToExploringBlendInfo;

        private PlayerLocomotionHandler playerLocomotionHandler;
        private PlayerAnimatorHandler playerAnimatorHandler;

        private void Start()
        {
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
            exploringCamera = GameObject.FindGameObjectWithTag(Utils.EXPLORING_CAMERA_TAG).GetComponent<CinemachineVirtualCamera>();
            targetingCamera = GameObject.FindGameObjectWithTag(Utils.TARGETING_CAMERA_TAG).GetComponent<CinemachineVirtualCamera>();
            defaultTargetingToExploringBlendInfo = new CinemachineBlendInfo(targetingCamera, exploringCamera, defaultStyle, defaultBlendTime);
            
            Transform player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG).transform;
            playerLocomotionHandler = player.GetComponent<PlayerLocomotionHandler>();
            playerAnimatorHandler = player.GetComponent<PlayerAnimatorHandler>();
        }

        private void Update()
        {
            HandleTargetingToExploringBlend();
        }

        private void HandleTargetingToExploringBlend()
        {
            CinemachineBlendDefinition targetingToExploringBlendDefinition = defaultTargetingToExploringBlendInfo.GetBlendDefinition();

            // Get current player speed
            float playerSpeed = playerAnimatorHandler.Animator.GetFloat(Utils.speedParam);

            if (playerSpeed > playerLocomotionHandler.BaseRunSpeed)
            {
                // Blend duration from targeting camera to exploring camera when running
                targetingToExploringBlendDefinition.m_Time = runningBlendTime;
            }

            // Set the custom blend definition from targeting camera to exploring camera
            SetBlendTime(targetingCamera, exploringCamera, targetingToExploringBlendDefinition);
        }

        private void SetBlendTime(CinemachineVirtualCamera fromVirtualCamera, CinemachineVirtualCamera toVirtualCamera, CinemachineBlendDefinition blendDefinition)
        {
            CinemachineBlenderSettings brainBlendSettings = cinemachineBrain.m_CustomBlends;

            // Search for the blend definition
            for (int i = 0; i < brainBlendSettings.m_CustomBlends.Length; i++)
            {
                CustomBlend customBlend = brainBlendSettings.m_CustomBlends[i];
                if (customBlend.m_From == fromVirtualCamera.name && customBlend.m_To == toVirtualCamera.name)
                {
                    brainBlendSettings.m_CustomBlends[i].m_Blend.m_Time = blendDefinition.m_Time;
                    brainBlendSettings.m_CustomBlends[i].m_Blend.m_Style = blendDefinition.m_Style;
                }
            }
        }
    }
}