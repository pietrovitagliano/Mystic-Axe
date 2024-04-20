// Author: Pietro Vitagliano

using System.Collections;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    public class LightningStrikeHandler : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float lightningDelay = 0.02f;
        [SerializeField] private float lightningLightLifetime = 0.52f;

        private Light lightningLight;
        private GameObject weapon;

        private void Start()
        {
            InitializeLightningParams();

            // Spawn the lightning
            GetComponent<ParticleSystem>().Play();

            // Handle collider and light of the lightning
            StartCoroutine(HandleLightningCoroutine());
        }

        private void InitializeLightningParams()
        {
            lightningLight = GetComponentInChildren<Light>();

            // At the beginning, the lightning is not visible (the particle system works in this way),
            // thus light is disabled
            lightningLight.gameObject.SetActive(false);
        }

        private IEnumerator HandleLightningCoroutine()
        {
            // Wait for the delay to spawn the lightning
            yield return new WaitForSeconds(lightningDelay);

            // The lightning is spawned here (because the particle system has a delay equals to lightningLightSpawnTime)
            // Thus light is enabled here
            lightningLight.gameObject.SetActive(true);

            // Play the impact sound
            AudioManager.Instance.PlaySoundOneShot(Utils.lightningStrikeImpactAudioName, gameObject);

            // Wait for the end of lightning's lifetime
            yield return new WaitForSeconds(lightningLightLifetime - lightningDelay);

            // Now the lightning is not visible, thus light is disabled
            lightningLight.gameObject.SetActive(false);
        }

        public float GetMaxLifetime()
        {
            return GetComponentsInChildren<ParticleSystem>().Max(particle => particle.main.startLifetime.constantMax);
        }

        public void InitializeCastingWeapon(GameObject weapon)
        {
            this.weapon = weapon;
        }

        private void OnParticleCollision(GameObject collidedGameObject)
        {
            int enemyLayer = LayerMask.NameToLayer(Utils.ENEMY_LAYER_NAME);

            if (collidedGameObject.layer == enemyLayer)
            {
                CharacterStatusHandler characterHitStatusHandler = collidedGameObject.GetComponent<CharacterStatusHandler>();
                if (!characterHitStatusHandler.IsDead && !characterHitStatusHandler.IsInvulnerable)
                {
                    // Compute damage
                    WeaponStatusHandler weaponStatusHandler = weapon.GetComponent<WeaponStatusHandler>();
                    Transform weaponOwner = weaponStatusHandler.Character;
                    float damage = weaponOwner.GetComponent<CharacterStatusHandler>().GetFeature(Utils.DAMAGE_FEATURE_NAME).CurrentValue;

                    // Apply damage
                    characterHitStatusHandler.ApplyDamage(damage, weaponOwner.gameObject);
                    
                    // Play hit animation
                    CharacterHitHandler characterHitHandler = collidedGameObject.GetComponent<CharacterHitHandler>();
                    characterHitHandler.HitCharacter(weaponPosition: transform.position, heavyHit: characterHitStatusHandler.IsDoingHeavyAttack, ignoreHyperArmor: true);
                }
            }
        }
    }
}