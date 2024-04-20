// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    public abstract class FadeHandler : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField, Range(0.01f, 5)] private float fadeInDuration = 0.25f;
        [SerializeField, Range(0.01f, 5)] private float fadeOutDuration = 0.25f;
        [SerializeField] protected bool startVisible = false;

        private float defaultFadeInDuration;
        private float defaultFadeOutDuration;
        private float targetAlpha;

        protected float TargetAlpha { get => targetAlpha; set => targetAlpha = value; }
        protected float FadeInDuration { get => fadeInDuration; }
        protected float FadeOutDuration { get => fadeOutDuration; }

        protected virtual void Awake()
        {
            defaultFadeInDuration = fadeInDuration;
            defaultFadeOutDuration = fadeOutDuration;
        }

        private void Update()
        {
            Fade();
        }
        
        protected abstract void Fade();
        public abstract void ShowInstantly();
        public abstract void HideInstantly();

        public void Show(float fadeInDuration = 0)
        {
            this.fadeInDuration = fadeInDuration == 0 ? defaultFadeInDuration : fadeInDuration;

            Show();
        }
        
        public void Hide(float fadeOutDuration = 0)
        {
            this.fadeOutDuration = fadeOutDuration == 0 ? defaultFadeOutDuration : fadeOutDuration;

            Hide();
        }
        
        protected virtual void Show()
        {
            targetAlpha = 1;
        }

        protected virtual void Hide()
        {
            targetAlpha = 0;
        }

        protected virtual float InterpolateToTargetAlpha(float currentAlpha)
        {
            float fadeDuration = targetAlpha == 1 ? fadeInDuration : fadeOutDuration;

            return Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / fadeDuration);
        }
    }
}
