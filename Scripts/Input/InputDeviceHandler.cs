// Author: Pietro Vitagliano

using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MysticAxe
{
    public class InputDeviceHandler : Singleton<InputDeviceHandler>
    {        
        private bool isUsingKeyboardAndMouse = false;
        private bool isUsingGamepad = false;
        
        public bool IsUsingKeyboardAndMouse { get => isUsingKeyboardAndMouse; }
        public bool IsUsingGamepad { get => isUsingGamepad; }

        // The first check has to be executed in the Awake(),
        // since other scripts might need to know what is
        // the input device, currently in use, in their Awake()
        protected override void Awake()
        {
            base.Awake();

            Cursor.visible = false;

            CheckForInput();
            HandleMouseCursorVisibility();
        }

        private void Update()
        {
            CheckForInput();
            HandleMouseCursorVisibility();
        }

        private void CheckForInput()
        {
            bool inputFromMouseAndKeyboard = isUsingKeyboardAndMouse ? true : CheckForKeyboardAndMouseInput();
            bool inputFromGamepad = isUsingGamepad ? true : CheckForGamepadInput();

            isUsingKeyboardAndMouse = !isUsingGamepad && inputFromMouseAndKeyboard && !inputFromGamepad;
            isUsingGamepad = !isUsingKeyboardAndMouse && !inputFromMouseAndKeyboard && inputFromGamepad;

            Debug.Log($"CheckForInput Using input device: {(isUsingGamepad ? "Gamepad" : "Keyboard and Mouse")}");
        }

        private bool CheckForKeyboardAndMouseInput()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            bool keyBoardInput = false;
            if (keyboard != null)
            {
                keyBoardInput = keyboard.anyKey.isPressed;
            }

            bool mouseInput = false;
            if (mouse != null)
            {
                mouseInput = mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed ||
                            mouse.scroll.ReadValue().magnitude > 0 || mouse.delta.ReadValue().magnitude > 0;
            }

            return keyBoardInput || mouseInput;
        }

        private bool CheckForGamepadInput()
        {
            Gamepad gamepad = Gamepad.current;

            bool gamepadInput = false;
            if (gamepad != null)
            {
                gamepadInput = gamepad.buttonSouth.isPressed ||
                                gamepad.buttonEast.isPressed ||
                                gamepad.buttonWest.isPressed ||
                                gamepad.buttonNorth.isPressed ||
                                gamepad.selectButton.isPressed ||
                                gamepad.startButton.isPressed ||
                                gamepad.leftShoulder.isPressed ||
                                gamepad.rightShoulder.isPressed ||
                                gamepad.leftStickButton.isPressed ||
                                gamepad.rightStickButton.isPressed ||
                                gamepad.leftTrigger.isPressed ||
                                gamepad.rightTrigger.isPressed ||
                                gamepad.leftStick.ReadValue().magnitude > 0 ||
                                gamepad.rightStick.ReadValue().magnitude > 0 ||
                                gamepad.leftStick.ReadValue().magnitude > 0 ||
                                gamepad.rightStick.ReadValue().magnitude > 0;
            }

            return gamepadInput;
        }

        private void HandleMouseCursorVisibility()
        {
            if (MenuManager.Instance != null && MenuManager.Instance.MenuList != null)
            {
                bool isMouseCursorVisible = isUsingKeyboardAndMouse && MenuManager.Instance.MenuList.Any(menu => menu != null && menu.activeInHierarchy);

                if (isMouseCursorVisible)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }
    }
}