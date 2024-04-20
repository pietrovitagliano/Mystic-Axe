// Author: Pietro Vitagliano

using System.Collections.Generic;
using UnityEngine;

namespace MysticAxe
{
    public class CleaverStatusHandler : WeaponStatusHandler
    {
        private CharacterStatusHandler characterStatusHandler;
        
        public override Vector3 GetWeaponForwardDirection()
        {
            return -1 * transform.right;
        }

        protected override float ComputeDamageApplied()
        {
            return characterStatusHandler.GetFeature(Utils.DAMAGE_FEATURE_NAME).CurrentValue;
        }

        protected override List<Feature> InitializeFeatures()
        {
            return JsonDatabase.Instance.GetDataFromJson<FeaturesJsonMap>(Utils.CLEAVER_FEATURES_JSON_NAME).Features;
        }

        protected override void InitializeWeapon()
        {
            characterStatusHandler = Character.GetComponent<CharacterStatusHandler>();

            // The collider becomes true only during the attack animations,
            // otherwise it is false, in order to prevent to damage continually the player
            // (if the OnTriggerEnter() is called)
            WeaponCollider.enabled = false;
            WeaponCollider.isTrigger = true;
        }
        
        protected override void OnTriggerEnter(Collider otherCollider)
        {
            int playerLayerMask = LayerMask.NameToLayer(Utils.PLAYER_LAYER_NAME);
            
            if (otherCollider.gameObject.layer == playerLayerMask)
            {
                Transform character = otherCollider.transform.root;
                CharacterStatusHandler characterHitStatusHandler = character.GetComponent<CharacterStatusHandler>();

                if (!characterHitStatusHandler.IsDead)
                {
                    bool shieldUp = false;
                    if (characterHitStatusHandler is PlayerStatusHandler playerStatusHandler)
                    {
                        shieldUp = playerStatusHandler.IsBlocking && Utils.IsShieldOnAttackLine(playerStatusHandler.transform, Character);
                    }

                    if (shieldUp)
                    {
                        ApplyHitForce(character);
                    }
                    else
                    {
                        if (!HasCharacterBeenAlreadyHit(character.gameObject) && characterHitStatusHandler.IsHitableCollider(otherCollider))
                        {
                            StartCoroutine(MarkCharacterAsHitCoroutine(character.gameObject));

                            // Apply damage and play hit animation
                            ApplyDamage(characterHitStatusHandler);
                            HitCharacter(characterHitStatusHandler, weaponPosition: transform.position);
                            WeaponBloodSpawner.MakeCharacterBleed(characterHitStatusHandler, otherCollider);

                            if (characterHitStatusHandler.IsDead)
                            {
                                // Play death hit sound
                                AudioManager.Instance.PlayMutuallyExclusiveSound(Utils.deathHitAudioName, gameObject);
                            }
                            else
                            {
                                // Play hit sound
                                AudioManager.Instance.PlaySound(Utils.playerAxeEnemyLightHitAudioName, gameObject);
                            }
                        }
                    }
                }
            }
        }
        
        public override void PlayMeleeLightWhoosh()
        {
            AudioManager.Instance.PlayMutuallyExclusiveSound(Utils.cleaverMeleeWhooshesAudioName, gameObject);
        }

        public override void PlayMeleeHeavyWhoosh() { }

        public override void StopMeleeLightWhoosh()
        {
            AudioManager.Instance.StopSound(Utils.cleaverMeleeWhooshesAudioName, gameObject);
        }

        public override void StopMeleeHeavyWhoosh() { }

        protected override void HandleClearCharacterAlreadyHitSet()
        {
            if (EnemyAlreadyHitSet.Count > 0 && !characterStatusHandler.IsAttacking)
            {
                ClearCharacterAlreadyHitSet();
            }
        }
    }
}