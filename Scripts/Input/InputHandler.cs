// Author: Pietro Vitagliano

using UnityEngine;
using UnityEngine.InputSystem;

namespace MysticAxe
{
    public class InputHandler : Singleton<InputHandler>
    {
        private Vector2 movementInput;
        private Vector2 cameraRotationInput;
        private bool dodgePressed;
        private bool fastRunPressed;
        private bool jumpPressed;
        private bool resetCamera;
        private bool equipAxePressed;
        private bool aimPressed;
        private bool throwAxePressed;
        private bool recallAxePressed;
        private bool blockPressed;
        private bool targetEnemyPressed;
        private bool lightAttackPressed;
        private bool heavyAttackPressed;
        private bool firstSkillPressed;
        private bool secondSkillPressed;
        private bool interactionPressed;
        private bool useConsumablePressed;
        private bool changeConsumablePressed;
        private bool pausePressed;


        private int skillIndexInput = 0;

        private enum SkillIndexEnum
        {
            First = 1,
            Second = 2
        }


        public Vector2 MovementInput { get => movementInput; set => movementInput = value; }
        public Vector2 CameraRotationInput { get => cameraRotationInput; }
        public bool FastRunPressed { get => fastRunPressed; set => fastRunPressed = value; }
        public bool JumpPressed { get => jumpPressed; }
        public bool ResetCamera { get => resetCamera; set => resetCamera = value; }
        public bool EquipAxePressed { get => equipAxePressed; set => equipAxePressed = value; }
        public bool DodgePressed { get => dodgePressed; set => dodgePressed = value; }
        public bool AimPressed { get => aimPressed; }
        public bool ThrowAxePressed { get => throwAxePressed; set => throwAxePressed = value; }
        public bool RecallAxePressed { get => recallAxePressed; set => recallAxePressed = value; }
        public bool BlockPressed { get => blockPressed; }
        public bool TargetEnemyPressed { get => targetEnemyPressed; set => targetEnemyPressed = value; }
        public bool LightAttackPressed { get => lightAttackPressed; set => lightAttackPressed = value; }
        public bool HeavyAttackPressed { get => heavyAttackPressed; set => heavyAttackPressed = value; }
        public bool FirstSkillPressed { get => firstSkillPressed; set => firstSkillPressed = value; }
        public bool SecondSkillPressed { get => secondSkillPressed; set => secondSkillPressed = value; }
        public bool InteractionButtonPressed { get => interactionPressed; set => interactionPressed = value; }
        public int SkillIndexInput { get => skillIndexInput; set => skillIndexInput = value; }
        public bool UseConsumablePressed { get => useConsumablePressed; set => useConsumablePressed = value; }
        public bool ChangeConsumablePressed { get => changeConsumablePressed; set => changeConsumablePressed = value; }
        public bool PausePressed { get => pausePressed; set => pausePressed = value; }


        public void OnInteract(InputAction.CallbackContext callbackContext) 
        {
            if (callbackContext.started)
            {
                interactionPressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    interactionPressed = false;
                }
            }
        }

        public void OnUseConsumable(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                useConsumablePressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    useConsumablePressed = false;
                }
            }
        }

        public void OnChangeConsumable(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                changeConsumablePressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    changeConsumablePressed = false;
                }
            }
        }

        public void OnMove(InputAction.CallbackContext callbackContext)
        {
            Vector2 movementInput = callbackContext.ReadValue<Vector2>();
            movementInput.x = Mathf.Abs(movementInput.x) >= 0.07f ? movementInput.x : 0;
            movementInput.y = Mathf.Abs(movementInput.y) >= 0.07f ? movementInput.y : 0;
            
            this.movementInput = Vector2.ClampMagnitude(movementInput, 1f);
            
            Debug.Log("InputScript movementInput = " + this.movementInput);
            Debug.Log("InputScript movementInput.magnitude = " + this.movementInput.magnitude);
        }

        public void OnRotateCamera(InputAction.CallbackContext callbackContext)
        {
            Vector2 cameraRotationInput = callbackContext.ReadValue<Vector2>();
            cameraRotationInput.x = Mathf.Abs(cameraRotationInput.x) >= 0.07f ? cameraRotationInput.x : 0;
            cameraRotationInput.y = Mathf.Abs(cameraRotationInput.y) >= 0.07f ? cameraRotationInput.y : 0;
            
            this.cameraRotationInput = Vector2.ClampMagnitude(cameraRotationInput, 1f);
            
            Debug.Log("InputScript cameraRotationInput = " + this.cameraRotationInput);
            Debug.Log("InputScript cameraRotationInput.magnitude = " + this.cameraRotationInput.magnitude);
        }

        public void OnFastRun(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                fastRunPressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    fastRunPressed = false;
                }
            }
        }

        public void OnJump(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.performed)
            {
                jumpPressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    jumpPressed = false;
                }
            }
        }

        public void OnResetCamera(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                resetCamera = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    resetCamera = false;
                }
            }
        }

        public void OnDodge(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                dodgePressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    dodgePressed = false;
                }
            }
        }

        public void OnEquipUnequip(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                equipAxePressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    equipAxePressed = false;
                }
            }
        }

        public void OnAim(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.performed)
            {
                aimPressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    aimPressed = false;
                }
            }
        }

        public void OnThrow(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                throwAxePressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    throwAxePressed = false;
                }
            }
        }

        public void OnRecall(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                recallAxePressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    recallAxePressed = false;
                }
            }
        }

        public void OnBlock(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.performed)
            {
                blockPressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    blockPressed = false;
                }
            }
        }

        public void OnTargetEnemy(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                targetEnemyPressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    targetEnemyPressed = false;
                }
            }
        }

        public void OnLightAttack(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                lightAttackPressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    lightAttackPressed = false;
                }
            }
        }

        public void OnHeavyAttack(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                heavyAttackPressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    heavyAttackPressed = false;
                }
            }
        }

        public void OnLightSkill(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                skillIndexInput = (int)SkillIndexEnum.First;
                firstSkillPressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    skillIndexInput = 0;
                    firstSkillPressed = false;
                }
            }
        }

        public void OnHeavySkill(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                skillIndexInput = (int)SkillIndexEnum.Second;
                secondSkillPressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    skillIndexInput = 0;
                    secondSkillPressed = false;
                }
            }
        }

        public void OnPause(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.started)
            {
                pausePressed = true;
            }
            else
            {
                if (callbackContext.canceled)
                {
                    pausePressed = false;
                }
            }
        }
    }
}
