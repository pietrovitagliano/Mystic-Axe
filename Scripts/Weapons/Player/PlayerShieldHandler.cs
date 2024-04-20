// Author: Pietro Vitagliano

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    public class PlayerShieldHandler : MonoBehaviour
    {
        [Header("Blocked Weapon Settings")]
        [SerializeField, Range(0.1f, 1)] private float playBlockSoundForSameWeaponTime = 0.4f;
        private List<GameObject> blockedWeapon = new List<GameObject>();
        private ParticleSystem sparklesParticleSystem;

        private PlayerStatusHandler playerStatusHandler;
        private List<Collider> shieldColliderList;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);
            playerStatusHandler = player.GetComponent<PlayerStatusHandler>();
            sparklesParticleSystem = GetComponentInChildren<ParticleSystem>();

            shieldColliderList = GetComponentsInChildren<Collider>().Where(collider => collider.isTrigger).ToList();
            shieldColliderList.ForEach(collider =>
            {
                collider.isTrigger = true;
                collider.enabled = false;
            });
        }

        private void Update()
        {
            HandleCollider();
        }
        
        private void OnTriggerEnter(Collider otherCollider)
        {
            int enemyWeaponLayerValue = LayerMask.NameToLayer(Utils.ENEMY_WEAPON_LAYER_NAME);

            if (otherCollider.gameObject.layer == enemyWeaponLayerValue)
            {
                Transform player = playerStatusHandler.transform;
                Transform enemy = otherCollider.transform.GetComponent<WeaponStatusHandler>().Character;

                if (Utils.IsShieldOnAttackLine(player, enemy))
                {
                    GameObject weapon = otherCollider.gameObject;
                    if (!blockedWeapon.Contains(weapon))
                    {
                        StartCoroutine(HandleBlockedWeaponCoroutine(weapon));

                        // Play sparkles effect
                        sparklesParticleSystem.Play();

                        // Play block sound
                        AudioManager.Instance.PlaySound(Utils.playerShieldHitAudioName, gameObject);
                    }
                }
            }
        }

        private void HandleCollider()
        {
            if (shieldColliderList[0].enabled != playerStatusHandler.IsBlocking)
            {
                shieldColliderList.ForEach(collider => collider.enabled = playerStatusHandler.IsBlocking);
            }
        }
        
        private IEnumerator HandleBlockedWeaponCoroutine(GameObject weapon)
        {
            blockedWeapon.Add(weapon);
            yield return new WaitForSeconds(playBlockSoundForSameWeaponTime);
            blockedWeapon.Remove(weapon);
        }
    }
}