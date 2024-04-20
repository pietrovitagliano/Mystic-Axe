// Author: Pietro Vitagliano

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MysticAxe
{
    public class CanvasUnscaledFadeHandler : CanvasFadeHandler
    {
        protected override void Show()
        {
            gameObject.SetActive(true);                
            base.Show();
        }

        public override void ShowInstantly()
        {
            gameObject.SetActive(true);
            base.ShowInstantly();
        }

        public void HideAndDeactivate()
        {
            base.Hide();
            TurnInactiveWhenHidden();
        }

        public void HideInstantlyAndDeactivate()
        {
            base.HideInstantly();
            gameObject.SetActive(false);
        }

        protected override float InterpolateToTargetAlpha(float currentAlpha)
        {
            float fadeDuration = TargetAlpha == 1 ? FadeInDuration : FadeOutDuration;

            return Mathf.MoveTowards(currentAlpha, TargetAlpha, Time.unscaledDeltaTime / fadeDuration);
        }

        private async void TurnInactiveWhenHidden()
        {
            while (CanvasGroup.alpha > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(Time.unscaledDeltaTime));
            }

            gameObject.SetActive(false);
        }
    }
}