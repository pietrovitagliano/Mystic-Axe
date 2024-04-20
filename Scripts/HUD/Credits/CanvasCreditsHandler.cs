using System.Collections;
using UnityEngine;

namespace MysticAxe{
    public class CanvasCreditsHandler : MonoBehaviour
    {
        [Header("Credits Settings")]
        [SerializeField, Range(0.1f, 5)] private float creditsFadeDuration = 0.8f;
        [SerializeField, Range(0.1f, 10)] private float creditsDuration = 7f;

        [SerializeField] private RectTransform crossfadeTransition;
        [SerializeField] private RectTransform credits;

        private CanvasFadeHandler creditsFadeHandler, crossfadeTransitionFadeHandler;

        private bool creditsEnded = true;

        public bool CreditsEnded { get => creditsEnded; }

        private void Start()
        {
            // Initialize the CanvasFadeHandlers
            crossfadeTransitionFadeHandler = crossfadeTransition.GetComponent<CanvasFadeHandler>();
            creditsFadeHandler = credits.GetComponent<CanvasFadeHandler>();

            // Hide the canvas instantly
            crossfadeTransitionFadeHandler.HideInstantly();
            creditsFadeHandler.HideInstantly();
        }

        public void ShowCredits()
        {
            if (creditsEnded){
                StartCoroutine(ShowCreditsCoroutine());
            }
        }

        private IEnumerator ShowCreditsCoroutine()
        {
            creditsEnded = false;

            // Compute the half duration of the credits fade
            float halfCreditsFadeDuration = creditsFadeDuration * 0.5f;

            // Show the black transition canvas in half duration
            crossfadeTransitionFadeHandler.Show(halfCreditsFadeDuration);

            // Wait for the half duration of the credits fade
            yield return new WaitForSeconds(halfCreditsFadeDuration);

            // Show the credits canvas in the other half duration
            creditsFadeHandler.Show(halfCreditsFadeDuration);

            // Wait for the other half duration of the credits fade
            yield return new WaitForSeconds(halfCreditsFadeDuration);

            // Wait for the credits duration
            yield return new WaitForSeconds(creditsDuration);

            // Now the credits are ended
            creditsEnded = true;
        }
    }
}