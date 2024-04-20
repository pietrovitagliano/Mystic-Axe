// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    public class BaseEnemyUIAlphaHandler : UIAlphaHandler
    {
        private RectTransform canvasLockOnTarget;


        protected override void Start()
        {
            base.Start();

            canvasLockOnTarget = FindObjectOfType<LockOnHandler>().CanvasLockOnTarget;
        }

        protected override void InitializeCharacter()
        {
            character = transform.root.gameObject;
        }
        
        protected override void HandleUIAppearence()
        {
            bool isTargeted = canvasLockOnTarget.IsChildOf(character.transform);

            if (isTargeted)
            {
                ShowCharacterUI();
            }
            else
            {
                // If canvasLockOnTarget.parent == null, player is not targeting any enemy,
                // otherwise he is targeting an enemy different from this one.
                // Thus, if player change target (only if change, not if he is not targeting any enemy),
                // the UI of the last enemy targeted will be instantly hidden.
                bool hideInstantaly = canvasLockOnTarget.parent != null;
                HideCharacterUI(hideInstantly: hideInstantaly);
            }
        }
    }
}