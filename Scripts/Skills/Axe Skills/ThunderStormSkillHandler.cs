// Author: Pietro Vitagliano

using System.Collections;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    public class ThunderStormSkillHandler : SkillHandler
    {
        private PlayerCombatHandler playerCombatHandler;

        [Header("Damage Settings")]
        [SerializeField, Range(0.1f, 2)] private float damagePercentageIncreasePerLevel = 0.35f;

        [Header("Thunder Storm Settings")]
        [SerializeField, Min(0.1f)] private float lightIntensityFadeOnTime = 0.4f;
        [SerializeField, Min(0.1f)] private float lightIntensityFadeOffTime = 0.2f;
        [SerializeField, Range(0.01f, 0.5f)] private float stormSoundFadeOutDuration = 0.15f;
        private ParticleSystem lightningRing;

        private float stormCastingTime;
        private float stormCastingAnimationDuration;


        [Header("Lightning Strike Prefab")]
        [SerializeField] private ParticleSystem lightningStrike;
        
        [Header("Lightning Strike Settings")]
        [SerializeField, Min(0.1f)] private float lightningStrikeDelay = 0.17f;
        [SerializeField, Range(1, 4)] private int lightningSeriesSpawnedForLevel = 2;
        [SerializeField, Range(0.1f, 20)] private float lightningMaxHeightFromOrigin = 6;

        private Light stormLight;
        
        private float stormDuration, stormLifeDuration, maxLightningLifetime;
        private float thunderstormTimeElapsed = 0;
        private float lightningTimeElapsed = 0;
        private float stormLightInitialIntensity;

        private Modifier thunderstormElectricDamage;
        private Modifier physicalDamage0;

        
        void Update()
        {
            HandleLight();
            HandleDestruction();

            thunderstormTimeElapsed += Time.deltaTime;
            lightningTimeElapsed += Time.deltaTime;
        }

        public override void CastSkill()
        {
            ParticleSystem lightningRingParticleSystem = transform.GetComponentsInChildren<ParticleSystem>()
                                                                    .ToList()
                                                                    .Find(particleSystem => particleSystem.tag == Utils.LIGHTNING_RING_TAG);

            // Get thunderstorm modifiers
            ModifiersJsonMap modifiersJsonMap = JsonDatabase.Instance.GetDataFromJson<ModifiersJsonMap>(Utils.MODIFIERS_JSON_NAME);
            thunderstormElectricDamage = modifiersJsonMap.GetModifierByID(Utils.THUNDERSTORM_ELECTRIC_DAMAGE_MODIFIER_ID);
            physicalDamage0 = modifiersJsonMap.GetModifierByID(Utils.PHYSICAL_DAMAGE_0_MODIFIER_ID);

            // Update thunder blessing factor with skill level
            float baseDamage = thunderstormElectricDamage.Factor;
            thunderstormElectricDamage.Factor += baseDamage * damagePercentageIncreasePerLevel * (SkillLevel - 1);

            // Apply thunderstorm modifiers
            WeaponStatusHandler weaponStatusHandler = Weapon.GetComponent<WeaponStatusHandler>();
            weaponStatusHandler.AddModifier(physicalDamage0);
            weaponStatusHandler.AddModifier(thunderstormElectricDamage);

            StartCoroutine(CastThunderstormCoroutine(lightningRingParticleSystem));
            StartCoroutine(HandleSoundStopCoroutine());
            StartCoroutine(HandleCastingAnimationEndCoroutine());
        }

        protected override void InitializeSkill()
        {
            transform.position = ComputeSpawnPosition();

            lightningRing = transform.GetComponentsInChildren<ParticleSystem>()
                                    .ToList()
                                    .Find(particleSystem => particleSystem.CompareTag(Utils.LIGHTNING_RING_TAG));

            stormLight = GetComponentInChildren<Light>();
            stormLightInitialIntensity = stormLight.intensity;
            stormLight.intensity = 0;

            // Compute the time needed for the storm to be cast
            stormCastingTime = lightningRing.main.startDelay.constant + 0.1f;

            // stormDuration and stormLifeDuration are different:
            // the former is the duration of the storm effect (lighnings casting),
            // the latter is the duration of the storm gameobject
            stormDuration = lightningRing.main.duration;

            // Find the biggest startLifetime value among all the children particle systems
            stormLifeDuration = 0;
            foreach (ParticleSystem particle in gameObject.GetComponentsInChildren<ParticleSystem>())
            {
                float duration = particle.main.duration;
                float startLifetime = particle.main.startLifetime.constant;
                float particleDuration = Mathf.Max(duration, startLifetime);
                if (particleDuration > stormLifeDuration)
                {
                    stormLifeDuration = particleDuration;
                }
            }

            // Compute the time that the casting animation has to last
            stormCastingAnimationDuration = stormDuration + stormCastingTime;

            // Compute max lifetime for each lightning
            maxLightningLifetime = lightningStrike.GetComponent<LightningStrikeHandler>().GetMaxLifetime();

            // Initialize playerCombatHandler
            playerCombatHandler = Character.GetComponent<PlayerCombatHandler>();
        }

        private Vector3 ComputeSpawnPosition()
        {
            Vector3 thunderstormSpawnPosition = Weapon.transform.position;
            thunderstormSpawnPosition.y = Character.transform.position.y;

            return thunderstormSpawnPosition;
        }

        private void HandleLight()
        {
            if (thunderstormTimeElapsed < stormLifeDuration - stormCastingTime)
            {
                stormLight.intensity = Mathf.Lerp(stormLight.intensity, stormLightInitialIntensity, Time.deltaTime / lightIntensityFadeOnTime);
            }
            else
            {
                stormLight.intensity = Mathf.Lerp(stormLight.intensity, 0, Time.deltaTime / lightIntensityFadeOffTime);
            }
        }
        
        private IEnumerator HandleCastingAnimationEndCoroutine()
        {
            yield return new WaitForSeconds(stormCastingAnimationDuration);

            // Remove thunderstorm modifiers
            WeaponStatusHandler weaponStatusHandler = Weapon.GetComponent<WeaponStatusHandler>();
            weaponStatusHandler.RemoveModifier(thunderstormElectricDamage);
            weaponStatusHandler.RemoveModifier(physicalDamage0);

            playerCombatHandler.StartSpecialAttackEndAnimation();
        }

        private IEnumerator HandleSoundStopCoroutine()
        {
            float duration = stormDuration - stormSoundFadeOutDuration;
            yield return new WaitForSeconds(duration);

            float fadeOutDuration = stormDuration - thunderstormTimeElapsed;
            AudioManager.Instance.StopSoundWithFadeOut(Utils.thunderstormLoopAudioName, Weapon, fadeOutDuration);
        }

        private void HandleDestruction()
        {
            if (thunderstormTimeElapsed >= stormLifeDuration &&
                lightningTimeElapsed >= maxLightningLifetime)
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator CastThunderstormCoroutine(ParticleSystem lightningRing)
        {
            transform.GetComponent<ParticleSystem>().Play();
            AudioManager.Instance.PlaySoundWithFadeIn(Utils.thunderstormLoopAudioName, Weapon);

            yield return new WaitForSeconds(stormCastingTime);

            for (int i = 0; i < SkillLevel; i++)
            {
                for (int j = 0; j < lightningSeriesSpawnedForLevel; j++)
                {
                    float durationLeft = stormDuration - thunderstormTimeElapsed;
                    StartCoroutine(CastLightningSerieCoroutine(durationLeft, lightningRing));

                    yield return new WaitForSeconds(lightningStrikeDelay);
                }
            }
        }

        private IEnumerator CastLightningSerieCoroutine(float stormDuration, ParticleSystem lightningRing)
        {
            float timeElapsed = 0;
            float delayTimeElapsed = 0;

            while (timeElapsed < stormDuration)
            {
                if (delayTimeElapsed >= lightningStrikeDelay)
                {
                    CastLightningStrike(lightningRing.transform);

                    lightningTimeElapsed = 0;
                    delayTimeElapsed = 0;
                }
                else
                {
                    delayTimeElapsed += Time.deltaTime;
                }

                timeElapsed += Time.deltaTime;
                yield return null;
            }
        }

                
        private void CastLightningStrike(Transform parent)
        {
            GameObject castedLightning = Instantiate(lightningStrike.gameObject, parent);
            
            LightningStrikeHandler lightningStrikeHandler = castedLightning.GetComponent<LightningStrikeHandler>();
            lightningStrikeHandler.InitializeCastingWeapon(Weapon);
            
            Utils.ResetTransformLocalPositionAndRotation(castedLightning.transform);

            // Compute and add local x and z offset to the lightning strike position,
            // starting from the center of the ring
            Vector3 lightingLocalPosition = ComputeLightningLocalPositionInsideRing(lightningRing);
            castedLightning.transform.position += lightingLocalPosition;

            // Compute the y position of the lightning strike in world space
            castedLightning.transform.position = new Vector3(castedLightning.transform.position.x,
                                                            LookDownForGroundYInWorldSpace(castedLightning.transform.position),
                                                            castedLightning.transform.position.z);
        }

        // Compute the local position of the lightning strike considering the center of the lightning ring.
        // The computation also takes into account player position, so that the lightning will never be spawned in player's position
        private Vector3 ComputeLightningLocalPositionInsideRing(ParticleSystem lightningRing)
        {
            int playerLayer = LayerMask.GetMask(Utils.PLAYER_LAYER_NAME);
            float offset = 100f;

            Vector3 start = lightningStrike.transform.position + offset * Vector3.up;
            float radius = lightningRing.main.startSize.constantMax;

            float x = Random.Range(-radius, radius);
            float z = Random.Range(-radius, radius);

            // Check if the lightning spawn position is where the player is standing.
            // If so, move the spawn position a just a bit, since the lightning should have appeared near to the player.
            Transform player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG).transform;
            float nearToPlayerDistance = 1.5f;
            while (Physics.Raycast(start, Vector3.down, float.MaxValue, playerLayer, QueryTriggerInteraction.Ignore))
            {
                x = Random.Range(-nearToPlayerDistance * player.localScale.x, nearToPlayerDistance * player.localScale.x);
                z = Random.Range(-nearToPlayerDistance * player.localScale.z, nearToPlayerDistance * player.localScale.z);
            }

            return new Vector3(x, 0, z);
        }

        private float LookDownForGroundYInWorldSpace(Vector3 positionInsideRing)
        {
            // Initialize ground and offset for raycast
            LayerMask groundLayerMask = LayerMask.GetMask(Utils.GROUND_LAYER_NAME);

            Vector3 start = positionInsideRing + lightningMaxHeightFromOrigin * Vector3.up;
            RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down, 2 * lightningMaxHeightFromOrigin, groundLayerMask, QueryTriggerInteraction.Ignore)
                                        .Where(hit => hit.distance <= lightningMaxHeightFromOrigin)
                                        .ToArray();
            if (hits.Length > 0)
            {
                Vector3 groundHit = hits.OrderBy(hit => hit.distance)
                                        .FirstOrDefault()
                                        .point;

                return groundHit.y;
            }

            return positionInsideRing.y;
        }

    }
}