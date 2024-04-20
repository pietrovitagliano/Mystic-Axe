// Author: Pietro Vitagliano

using Cinemachine;
using UnityEngine;

namespace MysticAxe
{
    public class MainMenu : Singleton<MainMenu>
    {
        public void OnPlay()
        {
            GameObject playerAxe = GameObject.FindGameObjectWithTag(Utils.PLAYER_AXE_TAG);

            // This axe is just a mock up, since it has no script attached to it.
            // Thus, it will be replaced by the axe child of the player
            GameObject axeNearToCampfire = GameObject.FindGameObjectWithTag(Utils.MOCK_AXE_TAG);

            // Put the player axe in the same place and rotation of the one near to the campfire 
            playerAxe.transform.parent = null;
            playerAxe.transform.SetPositionAndRotation(axeNearToCampfire.transform.position, axeNearToCampfire.transform.rotation);

            // The logic behind axe throw script handler requires these flag to these values in order to work properly
            // (when the axe in on the ground, these flags are set to these exact values)
            AxeStatusHandler axeStatusHandler = playerAxe.GetComponent<AxeStatusHandler>();
            axeStatusHandler.IsThrown = false;
            axeStatusHandler.IsReturning = false;
            axeStatusHandler.HasHitWall = true;

            // The axe collider is disabled in order to disable collisions and triggers
            // with other collider (otherwise the ground hit sound will be played, since the axe is on the ground)
            axeStatusHandler.WeaponCollider.enabled = false;

            // This is necessary to avoid the any kind of collision since the axe is on the ground.
            // The collisions will start to work again after the player will recall the axe
            axeStatusHandler.Rigidbody.isKinematic = true;

            // Destroy the old axe
            Destroy(axeNearToCampfire);

            // Get the prioritis of the 3 virtual cameras
            int exploringCameraPriority = GameObject.FindGameObjectWithTag(Utils.EXPLORING_CAMERA_TAG).GetComponent<CinemachineVirtualCamera>().Priority;
            int aimingCameraPriority = GameObject.FindGameObjectWithTag(Utils.AIMING_CAMERA_TAG).GetComponent<CinemachineVirtualCamera>().Priority;
            int targetingCameraPriority = GameObject.FindGameObjectWithTag(Utils.TARGETING_CAMERA_TAG).GetComponent<CinemachineVirtualCamera>().Priority;

            // Change campfire virtual camera's priority, in order to switch to one of the other virtual cameras
            CinemachineVirtualCamera campfireCamera = GameObject.FindGameObjectWithTag(Utils.CAMPFIRE_CAMERA_TAG).GetComponent<CinemachineVirtualCamera>();
            campfireCamera.Priority = Mathf.Min(exploringCameraPriority, aimingCameraPriority, targetingCameraPriority) - 1;
        }

        public void OnQuit()
        {
            MenuManager.Instance.QuitFunction();
        }
    }
}