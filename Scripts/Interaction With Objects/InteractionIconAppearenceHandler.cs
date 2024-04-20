// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    public class InteractionIconAppearenceHandler : MonoBehaviour
    {
        private ParticleSystemFadeHandler particleSystemFadeHandler;
        private bool isVisible = false;

        public bool IsVisible { get => isVisible; set => isVisible = value; }

        private void Start()
        {
            particleSystemFadeHandler = GetComponent<ParticleSystemFadeHandler>();
            particleSystemFadeHandler.HideInstantly();
        }

        private void Update()
        {
            HandleIconVisibility();
        }

        private void HandleIconVisibility()
        {
            if (isVisible)
            {
                particleSystemFadeHandler.Show();
            }
            else
            {
                particleSystemFadeHandler.Hide();
            }
        }
    }
}
