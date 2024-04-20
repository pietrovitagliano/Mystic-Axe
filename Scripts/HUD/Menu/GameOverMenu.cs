// Author: Pietro Vitagliano

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MysticAxe
{
    public class GameOverMenu : Singleton<GameOverMenu>
    {
        [Header("Next Scene Settings")]
        [SerializeField, Range(0.1f, 2)] private float goldPenalityDelay = 0.3f;
        [SerializeField, Range(0.1f, 2)] private float nextSceneDelay = 0.6f;

        private bool canTryAgainBePressed = true;

        public async void OnTryAgain()
        {
            if (canTryAgainBePressed)
            {
                // Prevent the player from pressing the button again
                canTryAgainBePressed = false;

                // Wait for the gold penality to be applied
                await Utils.AsyncWaitTimeScaled(goldPenalityDelay);

                GameObject player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);

                // After death, player loses 50% of his gold
                CurrenciesHandler currenciesHandler = player.GetComponent<CurrenciesHandler>();
                currenciesHandler.RemoveGold(currenciesHandler.GoldAmount / 2);

                // Get the name of the current scene
                string currentSceneName = SceneManager.GetActiveScene().name;

                if (currentSceneName == Utils.DUNGEON_SCENE)
                {
                    // Save the current level of the dungeon, in order to reload it in the next scene
                    int dungeonCurrentLevel = FindObjectOfType<DungeonGenerationHandler>().CurrentLevel;
                    DataBetweenScenes.Instance.StoreData(DataBetweenScenes.DUNGEON_CURRENT_LEVEL_KEY, dungeonCurrentLevel);
                }

                // Wait a delay before loading the next scene
                await Utils.AsyncWaitTimeScaled(nextSceneDelay);
                
                // Reload the current scene without saving the player status because the player has died
                SceneLoader.Instance.AsyncChangeScene(currentSceneName, savePlayerStatus: false);

                // Wait for the gameObject to be disabled
                while (gameObject.activeInHierarchy)
                {
                    await Task.Yield();
                }

                // Reset the flag
                canTryAgainBePressed = true;
            }
        }

        public void OnMainMenu()
        {
            MenuManager.Instance.MainMenuFunction();
        }

        public void OnQuit()
        {
            MenuManager.Instance.QuitFunction();
        }
    }
}