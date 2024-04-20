// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    public class EquipUnequipAxeRigHandler : AbstractRigWeightHandler
    {
        private Transform axe;
        private Transform weaponHand;
        private Transform weaponFolder;

        protected override void Start()
        {
            base.Start();

            axe = GameObject.FindGameObjectWithTag(Utils.PLAYER_AXE_TAG).transform;
            weaponFolder = Utils.FindGameObjectInTransformWithTag(transform, Utils.WEAPON_FOLDER_TAG).transform;
            weaponHand = Utils.FindGameObjectInTransformWithTag(transform, Utils.WEAPON_HOLDER_TAG).transform;
        }

        protected override void Update()
        {
            UpdateRigWeight();
        }

        private void OnEquipAxe()
        {
            axe.transform.parent = weaponHand;
            Utils.ResetTransformLocalPositionAndRotation(axe.transform);

            RigOff();
        }

        private void OnUnequipAxe()
        {
            axe.transform.parent = weaponFolder;
            Utils.ResetTransformLocalPositionAndRotation(axe.transform);

            RigOff();
        }

        private void RightHandRigWeightOn() 
        {
            RigOn();
        }
    }
}
