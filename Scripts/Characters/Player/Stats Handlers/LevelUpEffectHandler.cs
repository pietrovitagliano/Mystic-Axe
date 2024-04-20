// Author: Pietro Vitagliano

using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;


namespace MysticAxe
{
    public class LevelUpEffectHandler : MonoBehaviour
    {        
        // levelUpGlowMaterial is made to be added to other materials in a mesh renderer or skinned mesh renderer.
        // It doesn't replace any material (one material only is needed), thus it's not necessary a collection
        // like a list to store it (like DisintagrationObjectHandler)
        [Header("Effects")]
        [SerializeField] private Material levelUpGlowMaterial;      
        private VisualEffect vfxParticles;

        [Header("Shader Graph Keys")]
        [SerializeField] private string shaderGlowAmountReference = "_Glow_Amount";

        [Header("VFX Keys")]
        [SerializeField] private string vfxSphericalParticlesDurationName = "Spherical Particles Duration";
        [SerializeField] private string vfxIsCharacterAliveName = "Is Character Alive";
        [SerializeField] private string vfxDelayName = "Delay";

        [Header("Glow Vanishing Settings")]
        [SerializeField, Range(0.01f, 2)] private float flashVanishingDelay = 0.3f;
        [SerializeField, Range(0.01f, 2)] private float glowVanishingDuration = 0.3f;
        
        [Header("Audio Settings")]
        [SerializeField, Range(0.01f, 2)] private float loopSoundFadeDuration = 0.3f;

        private CharacterStatusHandler characterStatusHandler;
        private Coroutine levelUpEffectCoroutine = null;
        private readonly UnityEvent onFlashEvent = new UnityEvent();
        
        
        public UnityEvent OnFlashEvent { get => onFlashEvent; }
        public float FlashVanishingDelay { get => flashVanishingDelay; }
        

        private void Start()
        {
            // Avoid to call an event when the object is created and set the initial glow amount to 0
            vfxParticles = GetComponentsInChildren<VisualEffect>().ToList().Find(vfx => vfx.visualEffectAsset.name == Utils.LEVEL_UP_VFX_NAME);
            vfxParticles.initialEventName = "";

            // Set the boolean to true to enable the particles when needed
            vfxParticles.SetBool(vfxIsCharacterAliveName, true);

            if (levelUpGlowMaterial != null)
            {
                levelUpGlowMaterial.SetFloat(shaderGlowAmountReference, 0);
            }

            characterStatusHandler = GetComponent<CharacterStatusHandler>();
            characterStatusHandler.OnDeathEvent.AddListener(StopEffectOnDeath);
        }

        public void PlayEffect()
        {
            // If no level up effect is going on, play it
            if (levelUpEffectCoroutine == null)
            {
                // Start the coroutine to handle the level up effect
                levelUpEffectCoroutine = StartCoroutine(HandleEffectCoroutine());
            }
        }

        private void StopEffectOnDeath()
        {
            // Stop the coroutine
            if (levelUpEffectCoroutine != null)
            {
                StopCoroutine(levelUpEffectCoroutine);
                levelUpEffectCoroutine = null;
            }

            // Reset the vfx effect
            vfxParticles.Stop();

            // Set the boolean to false to stop the particles, since vfxParticles.Stop() is not istantanous
            vfxParticles.SetBool(vfxIsCharacterAliveName, false);

            // Reset the glow material if existent
            if (levelUpGlowMaterial != null)
            {
                // Get the current glow amount
                float currentGlowAmount = levelUpGlowMaterial.GetFloat(shaderGlowAmountReference);
                if (currentGlowAmount > 0)
                {
                    // Reset the glow effect
                    StartCoroutine(LerpGlowMaterialCoroutine(currentGlowAmount, 0, 0.25f));
                }
            }

            // Stop loop and explosion sound if they are playing
            AudioManager.Instance.StopSoundWithFadeOut(Utils.levelUpLoopAudioName, gameObject);
            AudioManager.Instance.StopSoundWithFadeOut(Utils.levelUpExplosionAudioName, gameObject);
        }

        private IEnumerator HandleEffectCoroutine()
        {
            float vfxSphericalParticlesDuration = vfxParticles.GetFloat(vfxSphericalParticlesDurationName);
            float vfxDelayBeforeFlash = vfxParticles.GetFloat(vfxDelayName);

            // Play the level up loop audio clip
            AudioManager.Instance.PlaySoundWithFadeIn(Utils.levelUpLoopAudioName, gameObject, loopSoundFadeDuration);
            
            // Play VFX the effect
            vfxParticles.Play();

            // Turn the glow material on
            if (levelUpGlowMaterial != null)
            {
                yield return LerpGlowMaterialCoroutine(0, 1, vfxSphericalParticlesDuration);
            }
            else
            {
                yield return new WaitForSeconds(vfxSphericalParticlesDuration);
            }

            // Wait for the delay of the VFX particles and, at the same time, fade out the sound.
            // In this way, when the explosion effect has to start, the sound is already faded out.
            float timeToWaitBeforeFadeOutStarts = Mathf.Max(vfxDelayBeforeFlash - loopSoundFadeDuration, 0);
            yield return new WaitForSeconds(timeToWaitBeforeFadeOutStarts);

            // Stop the level up loop audio clip
            AudioManager.Instance.StopSoundWithFadeOut(Utils.levelUpLoopAudioName, gameObject, loopSoundFadeDuration);

            // Wait for the fade out to end
            yield return new WaitForSeconds(loopSoundFadeDuration);

            // At this moment, the flash effect starts
            onFlashEvent.Invoke();

            // Play the level up explosion audio clip
            AudioManager.Instance.PlaySound(Utils.levelUpExplosionAudioName, gameObject);

            // Wait for a little delay before the glow material start vanishing
            yield return new WaitForSeconds(flashVanishingDelay);

            if (levelUpGlowMaterial != null)
            {
                // Turn the glow material off
                yield return LerpGlowMaterialCoroutine(1, 0, glowVanishingDuration);
            }

            levelUpEffectCoroutine = null;
        }

        private IEnumerator LerpGlowMaterialCoroutine(float start, float end, float duration)
        {
            float timeElapsed = 0;
            float glowAmount, currentGlowAmount;
            while (timeElapsed <= duration)
            {
                glowAmount = Mathf.Clamp01(timeElapsed / duration);
                currentGlowAmount = Mathf.Lerp(start, end, glowAmount);

                levelUpGlowMaterial.SetFloat(shaderGlowAmountReference, currentGlowAmount);

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            levelUpGlowMaterial.SetFloat(shaderGlowAmountReference, end);
        }
    }
}