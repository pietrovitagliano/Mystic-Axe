// Author: Pietro Vitagliano

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace MysticAxe
{
    // This class handle the disintegration of an object with at least one skinned mesh renderer
    // Thus, it has to be attached to humanoid or semi-humanoid characters with skinned mesh renderer
    // (like enemies or the player) that has to be disintegrated
    public class DisintegrateObjectHandler : MonoBehaviour
    {
        [Header("Shader Graph Keys")]
        [SerializeField] private string shaderDissolveAmountReference = "_DissolveAmount";

        [Header("VFX Keys")]
        [SerializeField] private string vfxDurationName = "Duration";

        [Header("Dissolve Settings")]
        [SerializeField, Range(0.1f, 10)] private float dissolveDuration = 3f;
        [SerializeField, Range(0.1f, 5)] private float particlesAndDissolveDelta = 1.1f;
        [SerializeField, Range(0.1f, 1)] private float disableCollisionsThreshold = 0.8f;

        private VisualEffect[] vfxParticlesArray;
        private List<Material> materials = new List<Material>();

        [Range(0, 1)] private float currentDissolveAmount = 0;


        private void Start()
        {
            vfxParticlesArray = GetComponentsInChildren<VisualEffect>()
                                .Where(vfx => vfx.visualEffectAsset.name == Utils.DISINTEGRATION_VFX_NAME)
                                .ToArray();

            // Particles duration can't be equal to the dissolve duration, since they wouldn't be syncronized.
            // That's why particlesAndDissolveDelta is used
            float VFXParticlesDuration = dissolveDuration - particlesAndDissolveDelta;

            foreach (VisualEffect vfx in vfxParticlesArray)
            {
                // Avoid to play the vfx when the object is created
                vfx.initialEventName = "";

                // Set the duration of the VFX particles
                vfx.SetFloat(vfxDurationName, VFXParticlesDuration);
            }

            InitializeMaterialsArray();
        }

        private void InitializeMaterialsArray()
        {
            // Find all SkinnedMeshRenderer and MeshRenderer components
            SkinnedMeshRenderer[] skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();

            // Combine the of materials of all SkinnedMeshRenderer and MeshRenderer components
            materials = skinnedMeshRenderers.SelectMany(renderer => renderer.materials)
                                            .Concat(meshRenderers.SelectMany(renderer => renderer.materials))
                                            .Where(material => material.shader.name.Contains(Utils.DISSOLVE_SHADER_GRAPH_NAME))
                                            .ToList();

            // The default value of the dissolve amount is 0 (the object is not dissolved)
            materials.ForEach(material => material.SetFloat(shaderDissolveAmountReference, 0));
        }

        public void OnDisintegrate()
        {
            StartCoroutine(DisintegrateObjectCoroutine());
        }

        private IEnumerator DisintegrateObjectCoroutine()
        {
            // Play all the vfx objects to finish
            foreach (VisualEffect vfx in vfxParticlesArray)
            {
                vfx.Play();
            }

            // Start the dissolve effect
            float timeElapsed = 0;
            while (timeElapsed < dissolveDuration)
            {
                timeElapsed += Time.deltaTime;

                foreach (Material material in materials)
                {
                    currentDissolveAmount = Mathf.Lerp(0, 1, Mathf.Clamp01(timeElapsed / dissolveDuration));
                    material.SetFloat(shaderDissolveAmountReference, currentDissolveAmount);
                }

                HandleCollisions();

                yield return null;
            }

            // Wait for all the vfx objects to finish
            foreach (VisualEffect vfx in vfxParticlesArray)
            {
                while (vfx.aliveParticleCount > 0)
                {
                    yield return null;
                }
            }

            // When the dissolve is complete and the particles are finished,
            // the gameobject is destroyed
            Destroy(gameObject);
        }

        private void HandleCollisions()
        {
            // Once the dissolve effect has reached at least the disableCollisionsThreshold, the colliders are disabled,
            // and for each rigidbody isKinematic is enabled and useGravity is disabled.
            // In this way the collisions are disabled but the character will remains where he has been dissolved.
            if (currentDissolveAmount >= disableCollisionsThreshold)
            {
                foreach (Rigidbody rigidbody in GetComponentsInChildren<Rigidbody>())
                {
                    rigidbody.isKinematic = true;
                    rigidbody.useGravity = false;
                }

                foreach (Collider collider in GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }
            }
        }
    }
}