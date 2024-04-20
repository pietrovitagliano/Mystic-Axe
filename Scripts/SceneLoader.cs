// Author: Pietro Vitagliano

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MysticAxe
{
    public class SceneLoader : Singleton<SceneLoader>
    {
        [Header("Scene Loader Components")]
        [SerializeField] private RectTransform loadingScreen;
        [SerializeField] private RectTransform crossFadeTransition;
        private CanvasFadeHandler loadingScreenFadeHandler;
        private CanvasFadeHandler transitionFadeHandler;
        
        [Header("Loading Screen Components")]
        [SerializeField] private Image rhombusImage;
        [SerializeField] private TMP_Text loadingText;

        [Header("Rhombus Animation Settings")]
        [SerializeField, Range(0.1f, 3)] private float fillingTime = 1.2f;
        [SerializeField, Range(0, 1)] private float initialFillAmount = 0;
        [SerializeField] private bool initialClockWiseValue = true;
        private Coroutine loadingRhombusAnimationCoroutine = null;

        private readonly UnityEvent onSceneLoadedEvent = new UnityEvent();


        public UnityEvent OnSceneLoaded => onSceneLoadedEvent;


        private void Start()
        {
            loadingScreenFadeHandler = loadingScreen.GetComponent<CanvasFadeHandler>();
            transitionFadeHandler = crossFadeTransition.GetComponent<CanvasFadeHandler>();
            
            loadingScreenFadeHandler.HideInstantly();
            transitionFadeHandler.HideInstantly();

            // Reset the square image settings
            rhombusImage.fillAmount = initialFillAmount;
            rhombusImage.fillClockwise = initialClockWiseValue;

            if (SceneManager.GetActiveScene().name == Utils.START_SCENE)
            {
                AsyncChangeScene(Utils.MAIN_MENU_SCENE);
            }
        }

        private void Update()
        {
            HandleResetTimeScale();
        }

        public void AsyncChangeScene(string sceneName, bool savePlayerStatus = true, float fadeInDuration = 0)
        {            
            // Save player status before loading the next scene, if the flag is set to true
            GameObject player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);
            if (player != null && savePlayerStatus)
            {
                player.GetComponent<PlayerStatusHandler>().SaveStatusForNextScene();
            }
            
            gameObject.SetActive(true);

            // Start the coroutine to change the scene
            StartCoroutine(ChangeSceneCoroutine(sceneName, fadeInDuration: fadeInDuration));
        }

        private void HandleResetTimeScale()
        {
            if (gameObject.activeInHierarchy && Time.timeScale < 1)
            {
                Time.timeScale = 1;
            }
        }

        private IEnumerator FadeTransition(float fadeInDuration = 0)
        {
            // Fade In To Black
            transitionFadeHandler.Show(fadeInDuration: fadeInDuration);

            // Wait until the show is complete
            while (transitionFadeHandler.CanvasGroup.alpha < 1)
            {
                yield return null;
            }

            // Show the loading screen if invisible and vice versa
            if (loadingScreenFadeHandler.CanvasGroup.alpha == 0)
            {
                loadingScreenFadeHandler.ShowInstantly();
            }
            else
            {
                loadingScreenFadeHandler.HideInstantly();
            }

            // Fade Out From Black
            transitionFadeHandler.Hide();

            // Wait until the hide is complete
            while (transitionFadeHandler.CanvasGroup.alpha > 0)
            {
                yield return null;
            }
        }

        private IEnumerator ChangeSceneCoroutine(string nextSceneName, float fadeInDuration)
        {
            // Here the scene has started to change.
            // During the change, the OSTs are not playable
            AudioManager.Instance.SetOSTsPlayable(playable: false);

            // Store the name of the current scene, to unload it later
            string currentSceneName = SceneManager.GetActiveScene().name;

            // To change scene, it's necessary to unload the current one and loading the next one.
            // In order to do that, an active scene is required. That's why the empty scene is loaded and activated
            AsyncOperation loadingEmptySceneOperation = SceneManager.LoadSceneAsync(Utils.EMPTY_SCENE, LoadSceneMode.Additive);

            yield return FadeTransition(fadeInDuration: fadeInDuration);
            
            // Start a coroutine that handle the square loading animation
            loadingRhombusAnimationCoroutine = StartCoroutine(LoadingRhombusAnimationCoroutine());

            // Wait until the empty scene is fully loaded
            while (!loadingEmptySceneOperation.isDone)
            {
                yield return null;
            }

            // The empty scene is fully loaded, thus activate it
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(Utils.EMPTY_SCENE));

            // Unload the current scene asynchronally
            AsyncOperation unloadingOperation = SceneManager.UnloadSceneAsync(currentSceneName);

            // Load the next scene asynchronally.
            // This operation will wait for the unloading to end,
            // before activating the new scene)
            yield return LoadSceneCoroutine(nextSceneName, operationToWaitBeforeActivation: unloadingOperation);

            // Here the scene has changed, the OSTs are playable again
            AudioManager.Instance.SetOSTsPlayable(playable: true);

            // Enable all inputs (in case they are disabled)
            InputMapHandler.Instance.SetInputEnabled(enabled: true);

            yield return FadeTransition();

            // Stop the loading square animation
            if (loadingRhombusAnimationCoroutine != null)
            {
                StopCoroutine(loadingRhombusAnimationCoroutine);
                loadingRhombusAnimationCoroutine = null;
            }

            // Reset the rhombus image settings
            rhombusImage.fillAmount = initialFillAmount;
            rhombusImage.fillClockwise = initialClockWiseValue;

            gameObject.SetActive(false);
        }

        private IEnumerator LoadingRhombusAnimationCoroutine()
        {
            // Reset the rhombus image settings
            rhombusImage.fillAmount = initialFillAmount;

            while (true)
            {
                // Reset clockwise
                rhombusImage.fillClockwise = initialClockWiseValue;

                // Fill the rhombus
                float timeElapsed = 0;
                while (timeElapsed < fillingTime)
                {
                    timeElapsed += Time.deltaTime;

                    rhombusImage.fillAmount = Mathf.Lerp(0, 1, Mathf.Clamp01(timeElapsed / fillingTime));

                    yield return null;
                }

                // Reverse clockwise
                rhombusImage.fillClockwise = !rhombusImage.fillClockwise;

                // Unfill the rhombus
                timeElapsed = 0;
                while (timeElapsed < fillingTime)
                {
                    timeElapsed += Time.deltaTime;

                    rhombusImage.fillAmount = Mathf.Lerp(1, 0, Mathf.Clamp01(timeElapsed / fillingTime));

                    yield return null;
                }
            }
        }

        private IEnumerator LoadSceneCoroutine(string sceneName, AsyncOperation operationToWaitBeforeActivation = null)
        {
            AsyncOperation loadingOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            loadingOperation.allowSceneActivation = operationToWaitBeforeActivation == null;

            while (!loadingOperation.isDone)
            {
                if (operationToWaitBeforeActivation != null && operationToWaitBeforeActivation.isDone)
                {
                    loadingOperation.allowSceneActivation = true;
                }

                yield return null;
            }

            // Invoke the event to notify that the scene has been loaded
            onSceneLoadedEvent.Invoke();
        }
    }
}