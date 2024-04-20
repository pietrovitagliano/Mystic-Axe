// Author: Pietro Vitagliano

using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using static MysticAxe.DungeonGenerationHandler;

namespace MysticAxe
{
    public class EnemySpawnerHandler : MonoBehaviour
    {
        [Header("VFX Keys")]
        [SerializeField] private string vfxAlphaScaleName = "Alpha Scale";
        [SerializeField] private string vfxRadiusFactorName = "Radius Factor";
        
        [Header("Enemy Spawner Settings")]
        [SerializeField] private EnemySpawnablePerDungeonLevel[] enemyTypePerLevelArray;
        [SerializeField, Range(0.1f, 5)] private float enemySpawnDelay = 1.3f;
        [SerializeField, Range(0, 30)] private float maxDistanceToLookAtPlayer = 15f;

        [Header("Portal Settings")]
        [SerializeField, Range(0.1f, 5)] private float openPortalDuration = 0.9f;
        [SerializeField, Range(0.1f, 5)] private float closePortalDelay = 1.3f;
        [SerializeField, Range(0.1f, 5)] private float closePortalDuration = 0.9f;
        [SerializeField, Range(0.1f, 5)] private float destroyPortalDelay = 0.8f;

        [Header("Portal Sounds Settings")]
        [SerializeField] AudioSource openPortalAudioSource;
        [SerializeField] AudioSource closePortalAudioSource;
        [SerializeField, Range(0f, 5)] private float openPortalSoundDelay = 0.4f;
        [SerializeField, Range(0f, 5)] private float closePortalSoundDelay = 0f;

        private VisualEffect vfxPortal;
        private UnityEvent<GameObject> onEnemySpawnedEvent = new UnityEvent<GameObject>();

        public UnityEvent<GameObject> OnEnemySpawnedEvent { get => onEnemySpawnedEvent; }

        private void Start()
        {
            vfxPortal = GetComponentInChildren<VisualEffect>();

            // At the beginning, the portal is closed
            vfxPortal.SetFloat(vfxAlphaScaleName, 0);
            vfxPortal.SetFloat(vfxRadiusFactorName, 0);

            // Start the coroutine that handles the enemy spawning
            StartCoroutine(SpawnEnemyFromPortalCoroutine());
        }

        private IEnumerator SpawnEnemyFromPortalCoroutine()
        {
            // Open portal
            yield return HandlePortalOpeningCoroutine(open: true);

            // Wait a bit before spawning the enemy
            yield return new WaitForSeconds(enemySpawnDelay);

            // Spawn the enemy
            StartCoroutine(SpawnEnemyCoroutine());

            // Wait a bit before closing the portal
            yield return new WaitForSeconds(closePortalDelay);

            // Close portal
            yield return HandlePortalOpeningCoroutine(open: false);

            // Destroy the portal
            yield return DestroyPortalCoroutine(destroyPortalDelay);
        }

        private IEnumerator HandlePortalOpeningCoroutine(bool open)
        {
            float target = open ? 1 : 0;

            // Determine the portal action based on the "open" value
            if (open)
            {
                // Open the portal
                vfxPortal.Play();

                // Play open portal sound with a delay
                StartCoroutine(PlayPortalSound(openPortalAudioSource, openPortalSoundDelay));
            }
            else
            {
                // Close the portal
                vfxPortal.Stop();

                // Play close portal sound with a delay
                StartCoroutine(PlayPortalSound(closePortalAudioSource, closePortalSoundDelay));
            }

            // Perform the portal animation
            float timeElapsed = 0;
            float duration = open ? openPortalDuration : closePortalDuration;
            float startValue = open ? 0 : 1;
            while (timeElapsed < duration)
            {
                float alphaScale = Mathf.Lerp(startValue, target, Mathf.Clamp01(timeElapsed / duration));
                vfxPortal.SetFloat(vfxAlphaScaleName, alphaScale);
                vfxPortal.SetFloat(vfxRadiusFactorName, alphaScale);

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            vfxPortal.SetFloat(vfxAlphaScaleName, target);
        }

        private IEnumerator DestroyPortalCoroutine(float delay)
        {
            // Wait a bit before destroying the portal
            yield return new WaitForSeconds(delay);

            // Remove all the listeners from the event before destroying the portal
            onEnemySpawnedEvent.RemoveAllListeners();

            // Destroy the portal
            Destroy(gameObject);
        }

        private IEnumerator SpawnEnemyCoroutine()
        {
            // Get the player gameObject
            GameObject player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);

            // Compute enemy rotation when spawned
            // (if the player is near enough, the enemy will be spawned looking at him)
            Quaternion enemyRotation;
            if (player != null && Vector3.Distance(transform.position, player.transform.position) <= maxDistanceToLookAtPlayer)
            {
                Vector3 portalToPlayerDirection = (player.transform.position - transform.position).normalized;
                enemyRotation = Quaternion.LookRotation(portalToPlayerDirection, Vector3.up);
            }
            else
            {
                enemyRotation = Quaternion.identity;
            }

            // Get all the enemies who can be instantiated in the current dungeon level
            int dungeonCurrentLevel = FindObjectOfType<DungeonGenerationHandler>().CurrentLevel;
            GameObject[] enemyPrefabsToSpawnInLevel = enemyTypePerLevelArray.Where(enemyTypePerLevel => enemyTypePerLevel.MinLevelToSpawn <= dungeonCurrentLevel)
                                                            .Select(enemyTypePerLevel => enemyTypePerLevel.EnemyPrefab)
                                                            .ToArray();
            // Choose the enemy
            GameObject enemyPrefabToSpawn = enemyPrefabsToSpawnInLevel[Random.Range(0, enemyPrefabsToSpawnInLevel.Length)];

            // Spawn the enemy and invoke the event to notify enemy's instantiation
            GameObject enemy = Instantiate(enemyPrefabToSpawn, transform.position, enemyRotation);
            onEnemySpawnedEvent.Invoke(enemy);

            // Start enemy spawn animation
            EnemyAnimatorHandler enemyAnimatorHandler = enemy.GetComponent<EnemyAnimatorHandler>();
            enemyAnimatorHandler.Animator.SetTrigger(enemyAnimatorHandler.SpawnHash);
            
            /* The first frame the enemy is spawned, no one of its animation is playing.
             * Since the spawn animation is needed, the code below will turn enemy alpha to 0 for just one frame.*/

            // Find all SkinnedMeshRenderer and MeshRenderer components in the instantiated enemy
            SkinnedMeshRenderer[] skinnedRenderers = enemy.GetComponentsInChildren<SkinnedMeshRenderer>();
            MeshRenderer[] meshRenderers = enemy.GetComponentsInChildren<MeshRenderer>();

            // Combine the arrays of materials
            Material[] materials = skinnedRenderers.SelectMany(renderer => renderer.materials)
                                                  .Concat(meshRenderers.SelectMany(renderer => renderer.materials))
                                                  .ToArray();

            // Make all the materials transparent (thus make the enemy transparent)
            foreach (Material material in materials)
            {
                Color color = material.color;
                material.color = new Color(color.r, color.g, color.b, 0);
            }

            // Wait for one frame
            yield return null;

            // Make all the materials visible again
            foreach (Material material in materials)
            {
                Color color = material.color;
                material.color = new Color(color.r, color.g, color.b, 1);
            }
        }

        private IEnumerator PlayPortalSound(AudioSource audioSource, float delay)
        {
            yield return new WaitForSeconds(delay);
            audioSource.Play();
        }
    }
}