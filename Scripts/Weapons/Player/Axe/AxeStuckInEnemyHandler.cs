// Author: Pietro Vitagliano

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MysticAxe
{
    public class AxeStuckInEnemyHandler : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float collisionDisabledTime = 0.3f;
        [SerializeField, Min(0.1f)] private float timeThreshold = 0.3f;
        [SerializeField, Min(0.1f)] private float speedThreshold = 1;
        private int enemyLayerValue;
        private float startCollisionTime;
        private bool canCollisionsBeDisabled;
        private float axeSpeed;

        private AxeStatusHandler axeStatusHandler;
        
        void Start()
        {
            axeStatusHandler = GetComponent<AxeStatusHandler>();
            enemyLayerValue = LayerMask.NameToLayer(Utils.ENEMY_LAYER_NAME);
        }

        void Update()
        {
            canCollisionsBeDisabled = !axeStatusHandler.IsAxeOnBody() && !axeStatusHandler.IsReturning;
            axeSpeed = GetComponent<Rigidbody>().velocity.magnitude;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (canCollisionsBeDisabled && axeSpeed < speedThreshold && collision.gameObject.layer == enemyLayerValue)
            {
                // Store the moment of the collision
                startCollisionTime = Time.time;
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (axeSpeed < speedThreshold)
            {
                if (canCollisionsBeDisabled && collision.gameObject.layer == enemyLayerValue)
                {
                    // Check if the collision with the enemy has persisted for longer than the threshold time
                    if (Time.time - startCollisionTime >= timeThreshold)
                    {
                        // Start the coroutine to ignore collisions with enemies
                        StartCoroutine(IgnoreEnemyCollisions(collisionDisabledTime));
                    }
                }
            }
            else
            {
                // If the axe is moving fast enough, reset the startCollisionTime
                startCollisionTime = Time.time; 
            }
        }

        private IEnumerator IgnoreEnemyCollisions(float duration)
        {
            // Ignore collisions with enemies for the specified duration
            Physics.IgnoreLayerCollision(gameObject.layer, enemyLayerValue, true);

            // Wait for the specified duration
            yield return new WaitForSeconds(duration);

            // Enable again collisions with enemies
            Physics.IgnoreLayerCollision(gameObject.layer, enemyLayerValue, false);
        }
    }
}
