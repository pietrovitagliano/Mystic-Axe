// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasFadeHandler : FadeHandler
    {
        private CanvasGroup canvasGroup;
        
        public CanvasGroup CanvasGroup { get => canvasGroup; }

        protected override void Awake()
        {
            base.Awake();
            
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = startVisible ? 1 : 0;
            TargetAlpha = canvasGroup.alpha;
        }

        protected override void Fade()
        {
            canvasGroup.alpha = InterpolateToTargetAlpha(currentAlpha: canvasGroup.alpha);
        }

        public override void ShowInstantly()
        {
            TargetAlpha = 1;
            canvasGroup.alpha = 1;
        }

        public override void HideInstantly()
        {
            TargetAlpha = 0;
            canvasGroup.alpha = 0;
        }
    }
}