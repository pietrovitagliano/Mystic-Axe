// Author: Pietro Vitagliano

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MysticAxe.SoundCategory;

namespace MysticAxe
{
    // This class contains all the game's sound.
    // It handles their properties and how the sounds has to be played
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Fade Settings")]
        [SerializeField, Range(0.1f, 3)] private float defaultSoundFadeTime = 0.25f;
        [SerializeField, Range(0.1f, 3)] private float explorationOSTFadeDuration = 1f;
        [SerializeField, Range(0.1f, 3)] private float battleOSTFadeDuration = 1f;
        [SerializeField, Range(0.1f, 3)] private float mutedOSTFadeDuration = 0.5f;


        [Header("Sound Categories Settings")]
        [SerializeField] private List<SoundCategory> soundCategoryList;
        private Dictionary<string, SoundCategory> soundCategoriesDict;

        private Dictionary<GameObject, Dictionary<string, Coroutine>> fadeInDict = new Dictionary<GameObject, Dictionary<string, Coroutine>>();
        private Dictionary<GameObject, Dictionary<string, Coroutine>> fadeOutDict = new Dictionary<GameObject, Dictionary<string, Coroutine>>();

        private PlayerStatusHandler playerStatusHandler;

        private bool areOSTsPlayable = true;


        // The 2D sounds initialization is requested in the Awake,
        // since they could be used by other classes in their Start method
        protected override void Awake()
        {
            base.Awake();

            // Add to the AudioManager all the 2D sounds
            foreach (SoundCategory sound in soundCategoryList)
            {
                if (sound.Is2DSound())
                {
                    sound.InitializeAudioSource(gameObject);
                }
            }

            // Convert the array of SoundCategory into a dictionary
            // for a more efficient search
            soundCategoriesDict = soundCategoryList.ToDictionary(soundCategory => soundCategory.Name, soundCategory => soundCategory);

            // Deallocate the array
            soundCategoryList.Clear();
            soundCategoryList = null;

            UpdatePlayerStatusReference();
            HandleGameOST();
        }

        private void Update()
        {
            UpdatePlayerStatusReference();
            HandleGameOST();
            Handle3DSoundsOnPause();
        }

        private void UpdatePlayerStatusReference()
        {
            if (playerStatusHandler == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);

                if (player != null)
                {
                    playerStatusHandler = player.GetComponent<PlayerStatusHandler>();
                }
            }
        }

        private void Handle3DSoundsOnPause()
        {
            AudioListener.pause = Time.timeScale == 0;
        }

        public void SetOSTsPlayable(bool playable)
        {
            areOSTsPlayable = playable;
        }

        private void StopOSTs(float fadeDuration, params string[] soundCategoryNames)
        {
            foreach (string soundCategoryName in soundCategoryNames)
            {
                StopMutuallyExclusiveSoundWithFadeOut(soundCategoryName, gameObject, fadeDuration);
            }
        }
            
        private void HandleGameOST()
        {
            if (playerStatusHandler != null)
            {
                // Check if all the OSTs need to be stopped
                if (!areOSTsPlayable)
                {
                    // Stop all exploration and battle OSTs and exit from the method
                    StopOSTs(fadeDuration: mutedOSTFadeDuration, Utils.safeAreaOSTAudioName, Utils.dungeonOSTAudioName, Utils.battleOSTAudioName, Utils.bossFightOSTAudioName);
                    return;
                }
                
                // Check if the player is in a boss fight
                DungeonBossFightHandler dungeonBossFightHandler = FindObjectOfType<DungeonBossFightHandler>();
                bool isBossFightOnGoing = SceneManager.GetActiveScene().name == Utils.BOSS_FIGHT_SCENE &&
                                            dungeonBossFightHandler != null &&
                                            dungeonBossFightHandler.IsBossFightOnGoing;

                // If the player is in a boss fight or an enemy is nearby, play the battle OST
                if (isBossFightOnGoing || playerStatusHandler.EnemyNearby)
                {
                    // Stop all exploration OST with fade out
                    StopOSTs(fadeDuration: explorationOSTFadeDuration, Utils.safeAreaOSTAudioName, Utils.dungeonOSTAudioName);

                    if (!IsPlayingAny(gameObject, Utils.safeAreaOSTAudioName, Utils.dungeonOSTAudioName))
                    {
                        // Play battle OST mutually exclusive
                        string battleOST = isBossFightOnGoing ? Utils.bossFightOSTAudioName : Utils.battleOSTAudioName;
                        PlayMutuallyExclusiveSoundWithFadeIn(battleOST, gameObject, battleOSTFadeDuration);
                    }
                }
                // Else if the player is in a safe area or in a dungeon, play the exploration OST
                else
                {
                    // Stop all battle OST with fade out
                    StopOSTs(fadeDuration: battleOSTFadeDuration, Utils.battleOSTAudioName, Utils.bossFightOSTAudioName);

                    if (!IsPlayingAny(gameObject, Utils.battleOSTAudioName, Utils.bossFightOSTAudioName))
                    {
                        // Play exploration OST mutually exclusive
                        string explorationOST = SceneManager.GetActiveScene().name == Utils.MAIN_MENU_SCENE ? Utils.safeAreaOSTAudioName : Utils.dungeonOSTAudioName;
                        PlayMutuallyExclusiveSoundWithFadeIn(explorationOST, gameObject, explorationOSTFadeDuration);
                    }
                }
            }
        }

        private void InitializationCheck(SoundCategory sound, GameObject source)
        {
            // If source has not been initialized yet,
            // adds 2 dict for it and initialize it
            if (!sound.HasBeenInitialized(source))
            {
                if (!fadeInDict.ContainsKey(source))
                {
                    fadeInDict.Add(source, new Dictionary<string, Coroutine>());
                }

                if (!fadeOutDict.ContainsKey(source))
                {
                    fadeOutDict.Add(source, new Dictionary<string, Coroutine>());
                }

                sound.InitializeAudioSource(source);
            }
        }

        public void SetPitch(string soundCategoryName, GameObject source, float pitch)
        {
            SoundCategory matchingNameSound = FindSoundCategory(soundCategoryName);
            
            if (matchingNameSound != null)
            {
                InitializationCheck(matchingNameSound, source);

                matchingNameSound.SetPitch(source, pitch);
            }
        }

        public void SetPitch(string soundCategoryName, GameObject source, float pitch, float minPitch)
        {
            pitch = Mathf.Clamp(pitch, minPitch, pitch);
            SetPitch(soundCategoryName, source, pitch);
        }

        public void SetVolume(string soundCategoryName, GameObject source, float volume)
        {
            SoundCategory matchingNameSound = FindSoundCategory(soundCategoryName);

            if (matchingNameSound != null)
            {
                InitializationCheck(matchingNameSound, source);

                matchingNameSound.SetVolume(source, volume);
            }
        }

        public void SetVolume(string soundCategoryName, GameObject source, float volume, float minVolume)
        {
            volume = Mathf.Clamp(volume, minVolume, volume);
            SetVolume(soundCategoryName, source, volume);
        }

        public void PlayMutuallyExclusiveSound(string soundCategoryName, GameObject source)
        {
            if (!IsPlaying(soundCategoryName, source))
            {
                PlaySound(soundCategoryName, source);
            }
        }

        public void PlayMutuallyExclusiveSoundWithFadeIn(string soundCategoryName, GameObject source, float fadeTime = 0)
        {
            if (!IsPlaying(soundCategoryName, source))
            {
                PlaySoundWithFadeIn(soundCategoryName, source, fadeTime);
            }
        }

        public void PlaySound(string soundCategoryName, GameObject source)
        {
            SoundCategory matchingNameSound = FindSoundCategory(soundCategoryName);

            if (matchingNameSound != null)
            {
                InitializationCheck(matchingNameSound, source);

                matchingNameSound.Play(source);
            }
        }

        public void PlaySound(string soundCategoryName, GameObject source, float delay)
        {
            IEnumerator PlayWithDelayCoroutine(float delay)
            {
                yield return new WaitForSeconds(delay);

                PlaySound(soundCategoryName, source);
            }

            StartCoroutine(PlayWithDelayCoroutine(delay));
        }

        public void PlaySoundOneShot(string soundCategoryName, GameObject source)
        {
            SoundCategory matchingNameSound = FindSoundCategory(soundCategoryName);

            if (matchingNameSound != null)
            {
                InitializationCheck(matchingNameSound, source);

                matchingNameSound.PlayOneShot(source);
            }
        }

        public void PlaySoundWithFadeIn(string soundCategoryName, GameObject source, float fadeTime = 0)
        {
            fadeTime = fadeTime == 0 ? defaultSoundFadeTime : fadeTime;

            SoundCategory matchingNameSound = FindSoundCategory(soundCategoryName);

            if (matchingNameSound != null)
            {
                InitializationCheck(matchingNameSound, source);

                Coroutine fadeInCoroutine = StartCoroutine(PlaySoundWithFadeInCoroutine(matchingNameSound, source, fadeTime));

                if (fadeInDict.TryGetValue(source, out Dictionary<string, Coroutine> fadeInCoroutinesDict))
                {
                    fadeInCoroutinesDict.TryAdd(soundCategoryName, fadeInCoroutine);
                }
            }
        }

        public bool IsPlaying(string soundCategoryName, GameObject source)
        {
            SoundCategory matchingNameSound = FindSoundCategory(soundCategoryName);

            if (matchingNameSound != null)
            {
                InitializationCheck(matchingNameSound, source);
            }

            return matchingNameSound.IsPlaying(source);
        }

        public bool IsPlayingAny(GameObject source, params string[] soundCategoryNames)
        {
            return soundCategoryNames.Any(soundCategoryName =>
            {
                return IsPlaying(soundCategoryName, source);
            });
        }

        public void StopSound(string soundCategoryName, GameObject source)
        {
            if (IsPlaying(soundCategoryName, source))
            {
                StopFadeCoroutines(soundCategoryName, source);

                SoundCategory matchingNameSound = FindSoundCategory(soundCategoryName);

                if (matchingNameSound != null)
                {
                    InitializationCheck(matchingNameSound, source);

                    matchingNameSound.Stop(source);
                }
            }
        }

        public void StopSoundWithFadeOut(string soundCategoryName, GameObject source, float fadeTime = 0)
        {
            if (IsPlaying(soundCategoryName, source))
            {
                fadeTime = fadeTime == 0 ? defaultSoundFadeTime : fadeTime;

                SoundCategory matchingNameSound = FindSoundCategory(soundCategoryName);

                if (matchingNameSound != null)
                {
                    InitializationCheck(matchingNameSound, source);

                    StopFadeCoroutines(soundCategoryName, source);

                    Coroutine fadeOutCoroutine = StartCoroutine(StopSoundWithFadeOutCoroutine(matchingNameSound, source, fadeTime));

                    if (fadeOutDict.TryGetValue(source, out Dictionary<string, Coroutine> fadeOutCoroutinesDict))
                    {
                        fadeOutCoroutinesDict.TryAdd(soundCategoryName, fadeOutCoroutine);
                    }
                }
            }
        }
        
        public void StopMutuallyExclusiveSoundWithFadeOut(string soundCategoryName, GameObject source, float fadeTime = 0)
        {
            if (IsPlaying(soundCategoryName, source))
            {
                fadeTime = fadeTime == 0 ? defaultSoundFadeTime : fadeTime;

                SoundCategory matchingNameSound = FindSoundCategory(soundCategoryName);

                if (matchingNameSound != null)
                {
                    InitializationCheck(matchingNameSound, source);

                    Coroutine fadeOutCoroutine = GetFadeCoroutine(soundCategoryName, ref fadeOutDict, source);
                    if (fadeOutCoroutine == null)
                    {
                        fadeOutCoroutine = StartCoroutine(StopSoundWithFadeOutCoroutine(matchingNameSound, source, fadeTime));

                        if (fadeOutDict.TryGetValue(source, out Dictionary<string, Coroutine> fadeOutCoroutinesDict))
                        {
                            fadeOutCoroutinesDict.TryAdd(soundCategoryName, fadeOutCoroutine);
                        }
                    }
                }
            }
        }

        private void StopFadeCoroutines(string soundCategoryName, GameObject source) 
        {
            SoundCategory matchingNameSound = FindSoundCategory(soundCategoryName);

            if (matchingNameSound != null)
            {
                InitializationCheck(matchingNameSound, source);

                Coroutine fadeInCoroutine = GetFadeCoroutine(soundCategoryName, ref fadeInDict, source);
                Coroutine fadeOutCoroutine = GetFadeCoroutine(soundCategoryName, ref fadeOutDict, source);

                if (fadeInCoroutine != null)
                {
                    StopCoroutine(fadeInCoroutine);

                    if (fadeInDict.TryGetValue(source, out Dictionary<string, Coroutine> fadeInCoroutinesDict))
                    {
                        fadeInCoroutinesDict.Remove(soundCategoryName);
                    }
                }

                if (fadeOutCoroutine != null)
                {
                    StopCoroutine(fadeOutCoroutine);

                    if (fadeOutDict.TryGetValue(source, out Dictionary<string, Coroutine> fadeOutCoroutinesDict))
                    {
                        fadeOutCoroutinesDict.Remove(soundCategoryName);
                    }
                }
            }
        }

        private IEnumerator PlaySoundWithFadeInCoroutine(SoundCategory soundCategory, GameObject source, float fadeTime)
        {
            AudioSourceWithMetadata audioSourceWithMetadata = soundCategory.GetAudioSourceWithMetadata(source);
            AudioSource audioSource = audioSourceWithMetadata.AudioSource;
            
            float targetVolume = audioSourceWithMetadata.DefaultVolume;
            soundCategory.Play(source);

            float timeElapsed = 0;
            while (timeElapsed < fadeTime)
            {
                timeElapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(0, targetVolume, Mathf.Clamp01(timeElapsed / fadeTime));

                yield return null;
            }

            audioSource.volume = targetVolume;

            if (fadeInDict.TryGetValue(source, out Dictionary<string, Coroutine> fadeInCoroutinesDict))
            {
                fadeInCoroutinesDict.Remove(soundCategory.Name);
            }
        }

        private IEnumerator StopSoundWithFadeOutCoroutine(SoundCategory soundCategory, GameObject source, float fadeTime)
        {
            AudioSourceWithMetadata audioSourceWithMetadata = soundCategory.GetAudioSourceWithMetadata(source);
            AudioSource audioSource = audioSourceWithMetadata.AudioSource;

            float startVolume = audioSource.volume;
            
            float timeElapsed = 0;
            while (timeElapsed < fadeTime)
            {
                timeElapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0, Mathf.Clamp01(timeElapsed / fadeTime));

                yield return null;
            }

            soundCategory.Stop(source);
            audioSource.volume = startVolume;

            if (fadeOutDict.TryGetValue(source, out Dictionary<string, Coroutine> fadeOutCoroutinesDict))
            {
                fadeOutCoroutinesDict.Remove(soundCategory.Name);
            }
        }

        public SoundCategory FindSoundCategory(string soundCategoryName)
        {
            return soundCategoriesDict.TryGetValue(soundCategoryName, out SoundCategory soundCategory) ? soundCategory : null;
        }

        private Coroutine GetFadeCoroutine(string soundCategoryName, ref Dictionary<GameObject, Dictionary<string, Coroutine>> fadeCoroutineDict, GameObject source)
        {
            Coroutine fadeInCoroutine = null;
            if (fadeCoroutineDict.TryGetValue(source, out Dictionary<string, Coroutine> coroutineDict))
            {
                coroutineDict.TryGetValue(soundCategoryName, out fadeInCoroutine);
            }

            return fadeInCoroutine;
        }
    }
}