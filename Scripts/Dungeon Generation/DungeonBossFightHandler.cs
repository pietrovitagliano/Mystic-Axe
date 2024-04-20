// Author: Pietro Vitagliano

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    public class DungeonBossFightHandler : MonoBehaviour
    {
        private Transform player;

        [Header("Boss Room Collider Settings")]
        [SerializeField] private BoxCollider bossRoomCollider;
        [SerializeField] private Collider bossRoomEntranceCollider;
        private bool isBossFightOnGoing = false;
        private bool isBossFightEnded = false;

        [Header("Boss Fight Settings")]
        [SerializeField] private GameObject enemySpawnerPortalPrefab;
        [SerializeField] private int[] enemyNumberPerWave;
        [SerializeField, Range(0.1f, 5)] float enemySpawnDelay = 1.2f;
        [SerializeField, Range(0.1f, 10)] float waveDelay = 4f;
        [SerializeField, Range(0.1f, 5)] float bossFightEndDelay = 0.7f;
        [SerializeField, Range(0.1f, 60)] float showCreditsDelay = 12f;
        private readonly List<GameObject> enemiesAndPortals = new List<GameObject>();

        [Header("Credits")]
        private CanvasCreditsHandler canvasCreditsHandler;

        public bool IsBossFightOnGoing { get => isBossFightOnGoing; }


        private void Start()
        {
            player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG).transform;
            canvasCreditsHandler = FindObjectOfType<CanvasCreditsHandler>(includeInactive: true);

            // The bossRoomCollider is a trigger collider,
            // that is used to detect when the player enters the boss room
            bossRoomCollider.isTrigger = true;
            bossRoomCollider.enabled = true;

            // The bossRoomEntranceCollider is a non-trigger collider,
            // that is used to prevent the player from leaving the boss room.
            // At the beginning, it is disabled and it will be enabled when the boss fight starts.
            bossRoomEntranceCollider.isTrigger = false;
            bossRoomEntranceCollider.enabled = false;
        }

        private void Update()
        {
            HandleNullElementsInList();
        }

        private void OnTriggerEnter(Collider other)
        {
            // If the boss fight hasn't started yet and the bossRoomCollider (exactly this collider, not others) intersects with a player's collider
            if (!isBossFightOnGoing && !isBossFightEnded && other.bounds.Intersects(bossRoomCollider.bounds) && player.GetComponentsInChildren<Collider>().Contains(other))
            {
                isBossFightOnGoing = true;
                bossRoomEntranceCollider.enabled = true;

                // Start the boss fight
                StartCoroutine(HandleBossFightCoroutine());
            }
        }

        private IEnumerator HandleBossFightCoroutine()
        {            
            // Start boss fight, taking into account the waves of enemies
            for (int i = 0; i < enemyNumberPerWave.Length; i++)
            {
                yield return StartWaveCoroutine(i);

                while (enemiesAndPortals.Count > 0)
                {
                    yield return null;
                }
            }

            // The boss fight is ended
            isBossFightEnded = true;
            isBossFightOnGoing = false;
            
            // Wait a little delay
            yield return new WaitForSeconds(bossFightEndDelay);

            // Show the credits after the boss fight
            yield return ShowCreditsCoroutine();
        }

        private IEnumerator StartWaveCoroutine(int index)
        {
            // Wait a bit before starting the wave of enemies
            yield return new WaitForSeconds(waveDelay);

            for (int i = 0; i < enemyNumberPerWave[index]; i++)
            {
                // Compute portal position
                Vector3 portalPosition = GetRandomPositionInBossRoom();
                
                // Spawn the portal
                GameObject portal = Instantiate(enemySpawnerPortalPrefab, portalPosition, Quaternion.identity);

                // Add the portal to the list of enemies and portals
                enemiesAndPortals.Add(portal);

                // Add a listener in order to know when the enemy is spawned
                EnemySpawnerHandler enemySpawnerHandler = portal.GetComponent<EnemySpawnerHandler>();
                enemySpawnerHandler.OnEnemySpawnedEvent.AddListener(OnEnemySpawned);

                // Wait a bit before spawning the next enemy
                yield return new WaitForSeconds(enemySpawnDelay);
            }
        }
        
        private Vector3 GetRandomPositionInBossRoom()
        {            
            // Compute the raycast start point as a random point inside the boss room collider
            Vector3 start = bossRoomCollider.transform.position + bossRoomCollider.center;
            start.x += Random.Range(-0.4f * bossRoomCollider.size.x, 0.4f * bossRoomCollider.size.x);
            start.z += Random.Range(-0.4f * bossRoomCollider.size.z, 0.4f * bossRoomCollider.size.z);

            // Compute the layer mask for the raycast
            LayerMask groundLayerMask = LayerMask.GetMask(Utils.GROUND_LAYER_NAME);

            // Cast a ray from the start point to the bottom of the boss room collider
            // and return the nearest hit point with the Ground tag
            return Physics.RaycastAll(start, Vector3.down, float.MaxValue, groundLayerMask, QueryTriggerInteraction.Ignore)
                                        .Where(hit => hit.collider.CompareTag(Utils.GROUND_TAG))
                                        .OrderBy(hit => hit.distance)
                                        .FirstOrDefault()
                                        .point;
        }

        private void HandleNullElementsInList()
        {
            // After the boss fight starts, remove every null element from the list
            if (isBossFightOnGoing)
            {
                enemiesAndPortals.RemoveAll(item => item == null);
            }
        }

        private void OnEnemySpawned(GameObject enemy)
        {
            // Add the enemy to the list of enemies and portals
            enemiesAndPortals.Add(enemy);

            // The enemy knows player position
            EnemyStatusHandler enemyStatusHandler = enemy.GetComponent<EnemyStatusHandler>();
            enemyStatusHandler.NotifyPlayerPresence();
        }

        private IEnumerator ShowCreditsCoroutine(){
            // Turn off all OSTs after the boss fight
            AudioManager.Instance.SetOSTsPlayable(playable: false);

            // Wait a bit before returning to the main menu
            yield return new WaitForSeconds(showCreditsDelay);
            
            // Disable all inputs
            InputMapHandler.Instance.SetInputEnabled(enabled: false);

            // Show the credits
            canvasCreditsHandler.ShowCredits();

            // Wait until the credits are ended
            while (!canvasCreditsHandler.CreditsEnded)
            {
                yield return null;
            }

            // Clear all the data between scenes before returning to the main menu
            DataBetweenScenes.Instance.ClearData();

            // Return to the main menu without saving the player status, because the game has been completed
            SceneLoader.Instance.AsyncChangeScene(Utils.MAIN_MENU_SCENE, savePlayerStatus: false);
        }
    }
}