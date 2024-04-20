// Author: Pietro Vitagliano

using System.Linq;
using UnityEngine.InputSystem;

namespace MysticAxe
{
    public class InputMapHandler : Singleton<InputMapHandler>
    {
        private PlayerInput playerInput;

        public PlayerInput PlayerInput { get => playerInput; }

        public enum InputMapType
        {
            Player,
            UI
        }

        private void Start()
        {
            playerInput = GetComponent<PlayerInput>();
            playerInput.defaultActionMap = InputMapType.Player.ToString();
        }

        private void Update()
        {
            HandleMap();
        }

        public void SetInputEnabled(bool enabled)
        {
            playerInput.enabled = enabled;
        }

        private void HandleMap()
        {
            if (playerInput.enabled)
            {                
                // If no MenuManager.Instance is present or its menu list it's empty, it means that the game is loading, thus the UI input map will be used.
                // Of course, if a menu is active, the UI input map will be used as well
                bool uiMapNeeded = MenuManager.Instance == null || MenuManager.Instance.MenuList == null || MenuManager.Instance.MenuList.Any(menu => menu != null && menu.activeInHierarchy);
                
                if (uiMapNeeded)
                {
                    if (playerInput.currentActionMap.name == InputMapType.Player.ToString())
                    {
                        playerInput.SwitchCurrentActionMap(InputMapType.UI.ToString());
                    }
                }
                else
                {
                    if (playerInput.currentActionMap.name == InputMapType.UI.ToString())
                    {
                        playerInput.SwitchCurrentActionMap(InputMapType.Player.ToString());
                    }
                }
            }
        }
    }
}