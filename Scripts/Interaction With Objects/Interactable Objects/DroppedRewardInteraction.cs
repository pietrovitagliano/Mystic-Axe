// Author: Pietro Vitagliano

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace MysticAxe
{
    public class DroppedRewardInteraction : AbstractObjectInteraction
    {
        [Header("Gather Reward Settings")]
        [SerializeField, Range(0.1f, 1)] private float fadeOutDelay = 0.3f;
        [SerializeField, Range(0.1f, 2)] private float fadeOutDuration = 0.7f;
        private Reward reward;

        private List<Material> materialList = new List<Material>();
        private VisualEffect vfxParticles;
        
        public Reward Reward { get => reward; set => reward = value; }


        protected override void Start()
        {
            base.Start();

            foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
            {
                materialList.AddRange(meshRenderer.materials);
            }

            vfxParticles = GetComponentInChildren<VisualEffect>();
        }


        protected override void InitializeInteractability()
        {
            IsInteractable = true;
        }

        protected override void UpdateInteractability() { }

        protected override void InteractWithObject()
        {
            IsInteractable = false;

            if (reward != null)
            {
                StartCoroutine(GatherRewardCoroutine());
            }
        }

        private IEnumerator GatherRewardCoroutine()
        {
            yield return new WaitForSeconds(fadeOutDelay);

            // Give the reward to the player
            GameObject player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);
            reward.GiveReward(player);

            vfxParticles.Stop();
            
            float timeElapsed = 0;
            while (timeElapsed < fadeOutDuration || vfxParticles.aliveParticleCount > 0)
            {
                timeElapsed += Time.deltaTime;

                float newAlpha = Mathf.Lerp(1, 0, Mathf.Clamp01(timeElapsed / fadeOutDuration));
                materialList.ForEach(material => material.color = new Color(material.color.r, material.color.g, material.color.b, newAlpha));

                yield return null;
            }
            
            // Destroy the reward
            Destroy(gameObject);
        }
    }
}