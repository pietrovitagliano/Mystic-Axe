// Author: Pietro Vitagliano

using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace MysticAxe
{
    public class ThunderBlessingHandler : SkillHandler
    {
        [Header("Damage Settings")]
        [SerializeField, Range(0.1f, 2)] private float damagePercentageIncreasePerLevel = 0.3f;

        [Header("VFX Keys")]
        [SerializeField] private string electricityBurstMaxLifeTimeName = "Max Lifetime";
        private VisualEffect electricityBurst;

        private ParticleSystem thunderBlessingParticle;

        public override void CastSkill()
        {
            // Get thunder blessing modifier
            ModifiersJsonMap modifiersJsonMap = JsonDatabase.Instance.GetDataFromJson<ModifiersJsonMap>(Utils.MODIFIERS_JSON_NAME);
            Modifier thunderBlessingModifier = modifiersJsonMap.GetModifierByID(Utils.THUNDER_BLESSING_MODIFIER_ID);

            // Update thunder blessing factor with skill level
            float baseDamage = thunderBlessingModifier.Factor;
            thunderBlessingModifier.Factor += baseDamage * damagePercentageIncreasePerLevel * (SkillLevel - 1);

            // Apply modifier
            WeaponStatusHandler weaponStatusHandler = Weapon.GetComponent<WeaponStatusHandler>();
            weaponStatusHandler.AddModifier(thunderBlessingModifier);

            // Play thunder blessing graphics effects
            thunderBlessingParticle = GetComponent<ParticleSystem>();
            thunderBlessingParticle.Play();
            electricityBurst.Play();

            // Play thunder blessing sound effect
            AudioManager.Instance.PlaySound(Utils.thunderBlessingExplosionAudioName, Weapon);

            // Handle game object destrucition asyncronously
            StartCoroutine(HandleAsyncDestructionCoroutine());
        }

        protected override void InitializeSkill()
        {
            transform.position = Weapon.transform.position;

            electricityBurst = GetComponentsInChildren<VisualEffect>()
                                .ToList()
                                .Find(vfx => vfx.visualEffectAsset.name == Utils.ELECTRICITY_BURST_VFX_NAME);

            electricityBurst.initialEventName = "";
        }
        
        private IEnumerator HandleAsyncDestructionCoroutine()
        {
            // Get electricity burst lifetime duration
            float electricityBurstLifeTime = electricityBurst.GetFloat(electricityBurstMaxLifeTimeName);

            // Wait for the electricity burst to finish
            yield return new WaitForSeconds(electricityBurstLifeTime);

            // Wait for the thunder blessing particle system to finish
            while (thunderBlessingParticle.isPlaying)
            {
                yield return null;
            }

            // Destroy the game object
            Destroy(gameObject);
        }        
    }
}