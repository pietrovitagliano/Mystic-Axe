// Author: Pietro Vitagliano

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MysticAxe
{
    public class MenuManager : Singleton<MenuManager>
    {
        [Header("Pause Menu Settings")]
        [SerializeField, Range(0.1f, 1)] private float delayToPauseAgain = 0.3f;
        private bool isPauseInputValid = true;

        [Header("Game Over Menu Settings")]
        [SerializeField, Range(0.1f, 3)] private float showGameOverMenuDelay = 1.8f;

        private List<GameObject> menuList;
        private bool isGamePaused = false;

        private PlayerStatusHandler playerStatusHandler;
        
        public List<GameObject> MenuList { get => menuList; }

        
        private void Start()
        {
            // Fetch all the menus in the scene
            menuList = FindObjectsOfType<MenuNavigationHandler>(includeInactive: true)
                                .Select(menuUIHandler => menuUIHandler.gameObject)
                                .ToList();

            ShowMenuAfterLoadingScene();
            
            SceneLoader.Instance.OnSceneLoaded.AddListener(ShowMenuAfterLoadingScene);
        }

        private void Update()
        {
            UpdatePlayerReference();
            UpdatePauseState();
            HandlePauseMenu();
            HandleGameTime();
        }

        private void LateUpdate()
        {
            Debug.Log("HandleGameTime Is Game Paused: " + isGamePaused);
            Debug.Log("HandleGameTime Time.timeScale: " + Time.timeScale);
        }

        private void ShowMenuAfterLoadingScene()
        {
            // Disable all the menu, since all the menu are inactive at the beginning
            menuList.ForEach(menuUIGameObject =>
            {
                // Activate the gameObject in order to be able to call the next method (it is executed in the Update())
                menuUIGameObject.SetActive(true);

                // Hide (CanvasGroup.alpha) and disable their gameObject
                MenuNavigationHandler menuNavigationHandler = menuUIGameObject.GetComponent<MenuNavigationHandler>();
                menuNavigationHandler.CloseMenu();
            });

            // If the current scene is the main manu scene, show the menu
            if (SceneManager.GetActiveScene().name == Utils.MAIN_MENU_SCENE)
            {
                GameObject mainMenu = menuList.Find(gameObjectMenu => gameObjectMenu == MainMenu.Instance.gameObject);
                mainMenu.GetComponent<MenuNavigationHandler>().OpenMenu();
            }
        }

        private void UpdatePlayerReference()
        {
            if (playerStatusHandler == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);

                if (player != null)
                {
                    playerStatusHandler = player.GetComponent<PlayerStatusHandler>();
                    playerStatusHandler.OnDeathEvent.AddListener(ShowGameOverMenu);
                }
            }
        }

        private void UpdatePauseState()
        {
            if (isGamePaused)
            {
                isGamePaused = menuList.Where(menuUIGameObject => menuUIGameObject != MainMenu.Instance.gameObject && menuUIGameObject != GameOverMenu.Instance.gameObject)
                                        .Any(menuUIGameObject => menuUIGameObject.activeInHierarchy);
            }
            else
            {
                isGamePaused = PauseMenu.Instance.gameObject.activeInHierarchy;
            }
        }

        private void HandlePauseMenu()
        {
            bool canPauseInputBeAccepted = menuList.All(menuUIGameObject => !menuUIGameObject.activeInHierarchy);

            if (canPauseInputBeAccepted && isPauseInputValid && InputHandler.Instance.PausePressed)
            {
                InputHandler.Instance.PausePressed = false;

                AsyncHandlePauseInputValidity();

                PauseMenu.Instance.GetComponent<MenuNavigationHandler>().OpenMenu();
            }
        }

        private void ShowGameOverMenu()
        {
            if (playerStatusHandler.IsDead && !GameOverMenu.Instance.gameObject.activeInHierarchy)
            {
                StartCoroutine(ShowGameOverMenuCoroutine(delay: showGameOverMenuDelay));
            }
        }

        private IEnumerator ShowGameOverMenuCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (menuList.Any(menuUIGameObject => menuUIGameObject.activeInHierarchy))
            {
                menuList.ForEach(menuUIGameObject => menuUIGameObject.GetComponent<MenuNavigationHandler>().CloseMenu());
            }

            GameOverMenu.Instance.GetComponent<MenuNavigationHandler>().OpenMenu();

            while (!GameOverMenu.Instance.gameObject.activeInHierarchy)
            {
                yield return null;
            }
        }

        private async void AsyncHandlePauseInputValidity()
        {
            isPauseInputValid = false;

            // This wait is unscaled, thus it works also in pause
            await Task.Delay(TimeSpan.FromSeconds(delayToPauseAgain));

            isPauseInputValid = true;
        }

        private void HandleGameTime()
        {
            Time.timeScale = isGamePaused ? 0 : 1;
        }

        #region Menu Shared Functions        
        public void MainMenuFunction()
        {
            // Clear the data to pass between scenes
            DataBetweenScenes.Instance.ClearData();

            // Load the main menu scene
            SceneLoader.Instance.AsyncChangeScene(Utils.MAIN_MENU_SCENE);
        }

        public void QuitFunction()
        {
            // Clear the data to pass between scenes
            DataBetweenScenes.Instance.ClearData();

            // Quit the game
            Application.Quit();
        }
        #endregion
    }
}