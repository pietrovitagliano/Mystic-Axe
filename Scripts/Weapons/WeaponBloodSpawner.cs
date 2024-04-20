// Author: Pietro Vitagliano

using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MysticAxe
{
    [Serializable]
    public class WeaponBloodSpawner
    {
        [Header("Blood Settings")]
        [SerializeField, Range(0.1f, 2)] private float minBloodScale = 0.6f;
        [SerializeField, Range(0.1f, 2)] private float maxBloodScale = 1.1f;

        private readonly GameObject bloodAttachedPrefab;
        private readonly BFX_BloodSettings[] bloodEffectPrefabArray;

        private const float bloodEffectYAngleOffset = -90;
        private const float attachedBloodAngleOffset = 90;
        private readonly Transform weapon;
        private readonly Light directionalLight;

        public WeaponBloodSpawner(Transform weapon, GameObject bloodAttachedPrefab, BFX_BloodSettings[] bloodEffectPrefabArray)
        {
            this.weapon = weapon;
            this.bloodAttachedPrefab = bloodAttachedPrefab;
            this.bloodEffectPrefabArray = bloodEffectPrefabArray.Where(settings => settings.gameObject.GetComponentsInChildren<BFX_ShaderProperies>().Length > 0)
                                                                .ToArray();
            
            this.directionalLight = GameObject.FindGameObjectWithTag(Utils.SUN_TAG).GetComponent<Light>(); ;
        }
        
        public void MakeCharacterBleed(CharacterStatusHandler characterStatusHandler, Collider hitCollider)
        {
            if (!characterStatusHandler.IsInvulnerable)
            {
                SpawnBlood(hitCollider);
            }
        }
        
        private void SpawnBlood(Collider hitCollider)
        {
            // Get the hit point
            RaycastHit hit = FindCollisionPoint(hitCollider);
            
            // Compute the angle to position the blood effect correctly
            float angle = Mathf.Atan2(hit.normal.x, hit.normal.z) * Mathf.Rad2Deg + bloodEffectYAngleOffset;

            // Choose a random blood effect
            GameObject choosenEffect = ChooseRandomBloodEffect();

            // Instantiate the blood effect and give some randomness to its scale
            GameObject bloodEffectInstantiated = Object.Instantiate(choosenEffect, hit.point, Quaternion.Euler(0, angle, 0));
            bloodEffectInstantiated.transform.localScale = Vector3.one * UnityEngine.Random.Range(minBloodScale, maxBloodScale);
            
            BFX_BloodSettings settings = bloodEffectInstantiated.GetComponent<BFX_BloodSettings>();
            settings.LightIntensityMultiplier = directionalLight.intensity;

            // Handle blood gameObject destruction asyncronously
            AsyncDestroyBloodObject(bloodEffectInstantiated);

            Transform nearestBodyPart = hitCollider.transform.GetComponentsInChildren<Transform>()
                                                            .OrderBy(child => Vector3.Distance(child.position, hit.point))
                                                            .FirstOrDefault();

            if (nearestBodyPart != null)
            {
                GameObject attachedBloodInstantiated = Object.Instantiate(bloodAttachedPrefab);

                Transform attachedBloodTrasform = attachedBloodInstantiated.transform;
                attachedBloodTrasform.position = hit.point;
                attachedBloodTrasform.localRotation = Quaternion.identity;
                attachedBloodTrasform.localScale = Vector3.one * UnityEngine.Random.Range(minBloodScale, maxBloodScale);
                attachedBloodTrasform.LookAt(hit.point + hit.normal, Vector3.zero);
                attachedBloodTrasform.Rotate(attachedBloodAngleOffset, 0, 0);
                attachedBloodTrasform.transform.parent = nearestBodyPart;

                // Handle blood gameObject destruction asyncronously
                AsyncDestroyBloodObject(attachedBloodInstantiated);
            }
        }

        private async void AsyncDestroyBloodObject(GameObject bloodGameObject)
        {
            if (bloodGameObject != null)
            {
                // Compute the time to wait before destroy the gameObject
                float destructionTime = bloodGameObject.GetComponentsInChildren<BFX_ShaderProperies>()
                                                    .Max(properties => properties.GraphTimeMultiplier + properties.TimeDelay);

                // Wait until the blood effect disappears
                await Utils.AsyncWaitTimeScaled(1.1f * destructionTime);

                // Destroy the gameObject
                Object.Destroy(bloodGameObject);
            }
        }

        private RaycastHit FindCollisionPoint(Collider hitCollider)
        {
            // Compute the start point of the raycast
            Vector3 start = weapon.parent != null ? weapon.parent.position : weapon.position;

            // Compute the direction for the ray cast
            // If the weapon is hold by a character, the direction will be from weapon holder (it's the weapon's parent) towards hitCollider.
            // Otherwise, if the weapon is not holded be someone (an arrow for example), the direction
            // will be from it weapon.position towards hitCollider
            Vector3 rayDirection = (hitCollider.bounds.center - start).normalized;

            // Compute the max distance for the box cast
            float maxDistance = Vector3.Distance(start, hitCollider.bounds.center);

            // Get the layer mask of the character who has been hit
            LayerMask characterHitLayerMask = LayerMask.GetMask(LayerMask.LayerToName(hitCollider.gameObject.layer));

            // Do the box cast, and get the nearest collider that has been hit
            // If the box cast doesn't hit anything, return a fake hit
            RaycastHit fakeHit = new RaycastHit();
            RaycastHit nearestHit = Physics.RaycastAll(start, rayDirection, maxDistance, characterHitLayerMask, QueryTriggerInteraction.Collide)
                                            .Where(hit => hit.collider.gameObject == hitCollider.gameObject)
                                            .OrderBy(hit => Vector3.Distance(hit.point, weapon.position))
                                            .DefaultIfEmpty(fakeHit)
                                            .FirstOrDefault();

            // If nearestHit is the fake hit and the hitCollider is not the one of the character who has the weapon
            // in the hand, perform the method recursively on its collider
            if (nearestHit.Equals(fakeHit) && hitCollider.transform.root.gameObject != hitCollider.gameObject)
            {
                Transform character = hitCollider.transform.root;

                nearestHit = FindCollisionPoint(character.GetComponent<Collider>());
            }

            return nearestHit;
        }

        private GameObject ChooseRandomBloodEffect()
        {
            BFX_BloodSettings bloodSettings = bloodEffectPrefabArray[UnityEngine.Random.Range(0, bloodEffectPrefabArray.Length)];

            return bloodSettings.gameObject;
        }
    }
}