// Author: Pietro Vitagliano

using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

namespace MysticAxe
{
    public class AxeGraphicsEffectsHandler : MonoBehaviour
    {
        private PlayerStatusHandler playerStatusHandler;
        private AxeStatusHandler axeStatusHandler;
        private AxeReturnHandler axeReturnHandler;

        [Header("Shader Graph Keys")]
        [SerializeField] private string thunderBlessingReference = "_Apply_Shader";

        [Header("VFX Graph Keys")]
        [SerializeField] private string alphaReference = "Alpha";

        [Header("Axe Graphic Components")]
        [SerializeField] private ParticleSystem catchParticleSystem;
        [SerializeField] private ParticleSystem trailParticleSystem;
        [SerializeField] private Material thunderBlessingBladeMaterial;
        [SerializeField] private VisualEffect vfxThunderBlessing;

        [Header("Thunder Blessing Graphic Settings")]
        [SerializeField, Range(0.1f, 2f)] private float thunderBlessingTurnOnDuration = 0.3f;
        [SerializeField, Range(0.1f, 2f)] private float thunderBlessingTurnOffDuration = 0.6f;
        private Coroutine thunderBlessingCoroutine = null;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);
            playerStatusHandler = player.GetComponent<PlayerStatusHandler>();
            axeStatusHandler = GetComponent<AxeStatusHandler>();
            axeReturnHandler = GetComponent<AxeReturnHandler>();

            axeReturnHandler.OnCatchAxeEvent.AddListener(PlayCatchGraphicEffect);

            // At the beginning all the graphics effects are turned off
            catchParticleSystem.Stop();
            trailParticleSystem.Stop();
            
            thunderBlessingBladeMaterial.SetFloat(thunderBlessingReference, 0);
            vfxThunderBlessing.initialEventName = "";
        }

        private void Update()
        {
            HandleTrailGraphicEffect();
            HandleThunderBlessingGraphicEffect();
        }

        private void HandleTrailGraphicEffect()
        {
            bool showTrailConditionAxeOnBody = playerStatusHandler.IsAttacking || playerStatusHandler.IsCastingSkill;
            bool showTrailConditionAxeOnAir = !axeStatusHandler.IsAxeOnBody() && (axeStatusHandler.IsReturning || !axeStatusHandler.HasHitWall);

            if (showTrailConditionAxeOnBody || showTrailConditionAxeOnAir)
            {
                if (!trailParticleSystem.isPlaying)
                {
                    trailParticleSystem.Play();
                }
            }
            else
            {
                if (trailParticleSystem.isPlaying)
                {
                    trailParticleSystem.Stop();
                }
            }
        }

        private void HandleThunderBlessingGraphicEffect()
        {
            bool isThunderBlessingActive = axeStatusHandler.GetModifierByID(Utils.THUNDER_BLESSING_MODIFIER_ID) != null;
            float currentAmount = thunderBlessingBladeMaterial.GetFloat(thunderBlessingReference);

            if (isThunderBlessingActive)
            {
                if (currentAmount < 1 && thunderBlessingCoroutine == null)
                {
                    thunderBlessingCoroutine = StartCoroutine(HandleThunderBlessingCoroutine(turnOn: true));
                }
            }
            else
            {
                if (currentAmount > 0 && thunderBlessingCoroutine == null)
                {
                    thunderBlessingCoroutine = StartCoroutine(HandleThunderBlessingCoroutine(turnOn: false));
                }
            }
        }

        private IEnumerator HandleThunderBlessingCoroutine(bool turnOn)
        {
            float targetAmount, lerpTime;
            if (turnOn)
            {
                vfxThunderBlessing.Play();
                targetAmount = 1;
                lerpTime = thunderBlessingTurnOnDuration;
                AudioManager.Instance.PlayMutuallyExclusiveSoundWithFadeIn(Utils.thunderBlessingLoopAudioName, gameObject, fadeTime: thunderBlessingTurnOnDuration);
            }
            else
            {
                vfxThunderBlessing.Stop();
                targetAmount = 0;
                lerpTime = thunderBlessingTurnOffDuration;
                AudioManager.Instance.StopMutuallyExclusiveSoundWithFadeOut(Utils.thunderBlessingLoopAudioName, gameObject, fadeTime: thunderBlessingTurnOnDuration);
            }

            float startAmount = thunderBlessingBladeMaterial.GetFloat(thunderBlessingReference);

            float timeElapsed = 0;
            while (timeElapsed < lerpTime)
            {
                timeElapsed += Time.deltaTime;

                float currentAmount = Mathf.Lerp(startAmount, targetAmount, Mathf.Clamp01(timeElapsed / lerpTime));
                thunderBlessingBladeMaterial.SetFloat(thunderBlessingReference, currentAmount);

                yield return null;
            }

            thunderBlessingCoroutine = null;
        }

        private void PlayCatchGraphicEffect()
        {
            catchParticleSystem.Play();
        }

        public void SetThunderBlessingVFXAlpha(float alpha)
        {
            vfxThunderBlessing.SetFloat(alphaReference, alpha);
        }
    }
}