// Author: Pietro Vitagliano

using System;
using UnityEngine;
using UnityEngine.Events;

namespace MysticAxe
{
    public class CharacterHitHandler : MonoBehaviour
    {
        [Header("Hit Settings")]
        [SerializeField, Range(0.1f, 3f)] private float timeToPlayHitAnimationAgain = 0.5f;
        
        private CharacterStatusHandler characterStatusHandler;
        private CharacterAnimatorHandler characterAnimatorHandler;
        private bool canHitAnimationBePlayedAgain = true;
        private bool canResetHitAnimatorParams = false;


        private void Start()
        {
            characterAnimatorHandler = GetComponent<CharacterAnimatorHandler>();
            characterStatusHandler = GetComponent<CharacterStatusHandler>();
        }

        private void Update()
        {
            HandleResetHitAnimatorParams();
        }

        private void HandleResetHitAnimatorParams()
        {
            if (characterStatusHandler.IsHitAnimationOnGoing)
            {
                canResetHitAnimatorParams = true;
            }
            else
            {
                if (canResetHitAnimatorParams)
                {
                    canResetHitAnimatorParams = false;

                    characterAnimatorHandler.Animator.SetFloat(characterAnimatorHandler.HitDirectionXHash, 0);
                    characterAnimatorHandler.Animator.SetFloat(characterAnimatorHandler.HitDirectionYHash, 0);
                }
            }
        }

        public async void HitCharacter(Vector3 weaponPosition, bool heavyHit, bool ignoreHyperArmor = false)
        {            
            bool canHitAnimationBePlayed = canHitAnimationBePlayedAgain && !characterStatusHandler.IsHitAnimationOnGoing && !characterStatusHandler.IsInvulnerable &&
                                        !characterStatusHandler.WantsToCastSkill && !characterStatusHandler.IsCastingSkill &&
                                        !characterStatusHandler.HasHyperArmor || (characterStatusHandler.HasHyperArmor && ignoreHyperArmor);

            if (canHitAnimationBePlayed)
            {
                // Get the vector from the enemy to the player
                Vector3 enemyToWeaponDirection = (weaponPosition - transform.position).normalized;

                // Get the angle between the enemy's forward direction and the enemy-to-player direction
                float angle = Vector3.SignedAngle(enemyToWeaponDirection, transform.right, Vector3.up);

                // Calculate the cosine and sine values of the angle
                float hitDirectionX = Mathf.Cos(angle * Mathf.Deg2Rad);
                float hitDirectionY = Mathf.Sin(angle * Mathf.Deg2Rad);

                // Set hit direction
                characterAnimatorHandler.Animator.SetFloat(characterAnimatorHandler.HitDirectionXHash, hitDirectionX);
                characterAnimatorHandler.Animator.SetFloat(characterAnimatorHandler.HitDirectionYHash, hitDirectionY);

                // Trigger the right hit animation (heavy or lightly)
                int hitHash = heavyHit ? characterAnimatorHandler.HitHeavyHash : characterAnimatorHandler.HitLightlyHash;
                characterAnimatorHandler.Animator.SetTrigger(hitHash);

                canHitAnimationBePlayedAgain = false;
                await Utils.AsyncWaitTimeScaled(timeToPlayHitAnimationAgain);
                canHitAnimationBePlayedAgain = true;
            }
        }        
    }
}