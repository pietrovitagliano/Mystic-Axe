// Author: Pietro Vitagliano

using System.Collections;
using UnityEngine;

namespace MysticAxe
{
    public class ParticleSystemFadeHandler : FadeHandler
    {
        private ParticleSystem[] particleSystems;

        protected override void Awake()
        {
            base.Awake();

            particleSystems = GetComponentsInChildren<ParticleSystem>();

            float newAlpha = startVisible ? 1 : 0;
            SetAlpha(newAlpha);
        }

        private void SetAlpha(float newAlpha)
        {
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                Material childMaterial = particleSystem.GetComponent<Renderer>().material;
                childMaterial = ChangeAlphaMaterial(childMaterial, newAlpha);
                particleSystem.GetComponent<Renderer>().material = childMaterial;
            }
        }

        protected override void Fade()
        {
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                Material childMaterial = particleSystem.GetComponent<Renderer>().material;
                
                float currentAlpha = childMaterial.color.a;
                float newAlpha = InterpolateToTargetAlpha(currentAlpha: currentAlpha);

                childMaterial = ChangeAlphaMaterial(childMaterial, newAlpha);
                particleSystem.GetComponent<Renderer>().material = childMaterial;
            }
        }

        public override void ShowInstantly()
        {
            TargetAlpha = 1;
            SetAlpha(1);
        }

        public override void HideInstantly()
        {
            TargetAlpha = 0;
            SetAlpha(0);
        }

        protected override void Show()
        {
            base.Show();

            foreach (ParticleSystem particleSystem in particleSystems)
            {
                particleSystem.Play();
            }
        }

        protected override void Hide()
        {
            base.Hide();

            foreach (ParticleSystem particleSystem in particleSystems)
            {
                StartCoroutine(StopParticleSystemCorutine(particleSystem));
            }
        }

        private Material ChangeAlphaMaterial(Material material, float newAlpha)
        {
            Color newColor = material.color;
            newColor.a = newAlpha;
            material.color = newColor;
            
            return material;
        }
        
        private IEnumerator StopParticleSystemCorutine(ParticleSystem particleSystem)
        {
            float currentAlpha;
            
            do
            {
                currentAlpha = particleSystem.GetComponent<Renderer>().material.color.a;
                yield return null;
            } while (currentAlpha != 0);

            particleSystem.Stop();
        }
    }
}
