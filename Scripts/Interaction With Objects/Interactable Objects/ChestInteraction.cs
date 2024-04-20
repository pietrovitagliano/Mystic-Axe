// Author: Pietro Vitagliano

using DG.Tweening;
using System;
using UnityEngine;

namespace MysticAxe
{
    [RequireComponent(typeof(AudioSource))]
    public class ChestInteraction : AbstractObjectInteraction
    {
        [Header("Reward Settings")]
        [SerializeField] private Reward reward;

        [Header("Open Chest Settings")]
        [SerializeField] private GameObject chestLid;
        [SerializeField] private ParticleSystem openParticleSystem;
        [SerializeField] private ParticleSystem glowParticleSystem;
        [SerializeField, Range(-180, 180)] private float openAngle = -115;
        [SerializeField, Range(0.01f, 2)] private float openDelay = 0.2f;
        [SerializeField, Range(0.1f, 5)] private float openTime = 0.7f;

        public Reward Reward { get => reward; }

        protected override void Start()
        {
            base.Start();

            // Set the chest as not opened
            chestLid.transform.localEulerAngles = Vector3.zero;

            // Initialize chest particle systems
            openParticleSystem.Stop();
            glowParticleSystem.Play();

            reward = reward.InitializeReward();
        }

        protected override void InitializeInteractability()
        {
            IsInteractable = true;
        }

        protected override void UpdateInteractability() { }

        protected override void InteractWithObject()
        {
            IsInteractable = false;

            // The rotation is around X-Axis
            Vector3 eulearAnglesVector = openAngle * Vector3.right;
            chestLid.transform.DOLocalRotate(eulearAnglesVector, openTime)
                            .SetDelay(openDelay)
                            .OnStart(() => 
                            {
                                PlayOpenChestEffect();

                                // Play chest opening sound
                                GetComponent<AudioSource>().Play();
                            })
                            .OnComplete(() =>
                            {
                                if (reward != null)
                                {
                                    // Give the reward to the player
                                    GameObject player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);
                                    reward.GiveReward(player);
                                }

                                // Remove the script from the gameObject
                                Destroy(this);
                            });
        }

        private void PlayOpenChestEffect()
        {
            openParticleSystem.Play();
            glowParticleSystem.Stop();
        }
    }
}
