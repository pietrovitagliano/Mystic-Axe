// Author: Pietro Vitagliano

using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace MysticAxe
{
    public class HealthPotion : Consumable
    {
        public override void Consume(Transform character, GameObject instancedConsumable)
        {
            VisualEffect vfxParticles = character.GetComponentsInChildren<VisualEffect>().
                            Where(visualEffect => visualEffect.visualEffectAsset.name == Utils.HEALTH_POTION_VFX_NAME)
                            .FirstOrDefault();

            vfxParticles.initialEventName = "";
            
            Amount--;

            // Compute the amount of health to restore, based on potion level
            float healthRestored = effect * (1 + effectPercentageIncreasePerLevel * (Level - 1));

            CharacterStatusHandler characterStatusHandler = character.GetComponent<CharacterStatusHandler>();
            characterStatusHandler.RestoreHealth(healthRestored);

            vfxParticles.Play();

            // Play the potion sound (drinking sound)
            instancedConsumable.GetComponentInChildren<AudioSource>().Play();

            // Play heal effect audio
            AudioManager.Instance.PlaySound(Utils.healEffectAudioName, character.gameObject, delay: 0.5f);
        }

        protected override void InitializeDataFromJson()
        {
            keyWordsToFindPrefab = itemsJsonMap.GetItemByName(Utils.healthPotionItemName).KeyWords;
            effect = itemsJsonMap.GetItemByName(Utils.healthPotionItemName).Effect;
            effectPercentageIncreasePerLevel = itemsJsonMap.GetItemByName(Utils.healthPotionItemName).EffectPercentageIncreasePerLevel;
        }
    }
}