// Author: Pietro Vitagliano

using DungeonArchitect;
using DungeonArchitect.Builders.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MysticAxe.DungeonLevelsJsonMap;
using Random = UnityEngine.Random;

namespace MysticAxe
{
    public class DungeonGenerationHandler : DungeonEventListener
    {
        [Serializable]
        public class EnemySpawnablePerDungeonLevel
        {
            [SerializeField] private GameObject enemyPrefab;
            [SerializeField, Min(1)] private int minLevelToSpawn;

            public GameObject EnemyPrefab { get => enemyPrefab; }
            public int MinLevelToSpawn { get => minLevelToSpawn; }
        }

        private Dungeon dungeonScript;
        private DungeonBuilder dungeonBuilder;

        [Header("Dungeon Settings")]
        [Min(1)] private int dungeonIndex;
        [Min(1)] private int currentLevel;
        [Min(1)] private int maxLevel;
        [SerializeField, Range(0.1f, 2)] private float nextLevelFadeInDuration = 0.6f;

        [Header("Dungeon Treasure Chest Settings")]
        [SerializeField, Min(1)] private int treasureChestMinNumber = 6;
        [SerializeField, Min(1)] private int treasureChestMaxNumber = 20;
        [SerializeField, Range(0, 1)] private float treasureChestRoomPercentage = 0.65f;
        [SerializeField] private GameObject treasureChestPrefab;

        [Header("Dungeon Enemies Settings")]
        [SerializeField, Min(1)] private int enemyMinNumber = 10;
        [SerializeField, Min(1)] private int enemyMaxNumber = 30;
        [SerializeField, Range(0, 1)] private float enemyRoomPercentage = 0.5f;
        [SerializeField] private EnemySpawnablePerDungeonLevel[] enemyTypePerLevelArray;

        [Header("Essential GameObjects")]
        [SerializeField] private EssentialInstantiator essentialInstantiator;

        private NavMeshHandler navMeshHandler;

        public int DungeonIndex { get => dungeonIndex; }
        public int CurrentLevel { get => currentLevel; }

        // It's essential that this code is executed in the Awake,
        // since the dungeon index is used in the Start method of others scripts
        private void Awake()
        {
            navMeshHandler = GetComponent<NavMeshHandler>();
            InitializeDungeonVariables();
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == Utils.DUNGEON_SCENE)
            {
                GenerateNewLevel();
            }
        }

        public void GoToNextLevel()
        {
            currentLevel = Mathf.Clamp(currentLevel + 1, 1, maxLevel);

            string nextSceneName;
            if (currentLevel < maxLevel)
            {
                // The next one is another dungeon scene, woth the level updated
                nextSceneName = Utils.DUNGEON_SCENE;

                // Save the new level of the dungeon before going to the next level
                DataBetweenScenes.Instance.StoreData(DataBetweenScenes.DUNGEON_CURRENT_LEVEL_KEY, currentLevel);
            }
            else
            {
                // The next one is the boss fight scene
                nextSceneName = Utils.BOSS_FIGHT_SCENE;
            }

            // Load the next scene
            SceneLoader.Instance.AsyncChangeScene(nextSceneName, fadeInDuration: nextLevelFadeInDuration);
        }

        // Executed after that the dungeon has been completely built
        public override void OnPostDungeonBuild(Dungeon dungeon, DungeonModel model)
        {
            // Before spawning the enemies,
            // perform the NavMesh bake of the dungeon
            navMeshHandler.NavMeshBake();

            if (model is GridDungeonModel gridModel)
            {
                // Spawn chests and enemies
                SpawnOtherElements(gridModel);
            }

            // After chests and enemies have been spawned,
            // if the dungeon max level hasn't been reached yet,
            // assign the key to go to the next level
            if (currentLevel < maxLevel)
            {
                AssignDungeonKey();
            }

            // Instantiate essentials gameObjects, player included
            essentialInstantiator.InstantiateEssentials();

            // After the dungeon has been completely spawned,
            // and the player instantiated, move him to the dungeon entrance
            MovePlayerToDungeonEntrance();
        }

        /// <summary>
        /// Generate a new level of the dungeon
        /// </summary>
        public void GenerateNewLevel()
        {
            // Generate a random dungeon level
            dungeonScript.RandomizeSeed();
            dungeonScript.Build();
        }

        private void MovePlayerToDungeonEntrance()
        {
            // Get player's transform and character controller
            Transform player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG).transform;
            CharacterController characterController = player.GetComponent<CharacterController>();

            // Get the spawn position
            Transform playerSpawnPosition = GameObject.FindGameObjectWithTag(Utils.PLAYER_SPAWN_POSITION_TAG).transform;

            // Compute player's rotation after the spawn
            Quaternion lookForwardRotation = Quaternion.LookRotation(playerSpawnPosition.forward, Vector3.up);

            // Disable the character controller to be able to move the player 
            characterController.enabled = false;

            // Move and rotate the player
            player.SetPositionAndRotation(playerSpawnPosition.position, lookForwardRotation);

            // Enable the character controller again
            characterController.enabled = true;
        }

        /// <summary>
        /// Randomly assign, to an enemy or to a treasure chest,
        /// the key to proceed to the next level of the dungeon
        /// </summary>
        private void AssignDungeonKey()
        {
            Reward[] rewards = FindObjectsOfType<EnemyStatusHandler>()
                                .Where(enemyStatusHandler => enemyStatusHandler.Reward != null)
                                .Select(enemyStatusHandler => enemyStatusHandler.Reward)
                                .Concat(FindObjectsOfType<ChestInteraction>()
                                            .Where(chestInteraction => chestInteraction.Reward != null)
                                            .Select(chestInteraction => chestInteraction.Reward))
                                .ToArray();

            int randomIndex = Random.Range(0, rewards.Length);

            rewards[randomIndex].HasDungeonKey = true;
        }

        private void InitializeDungeonVariables()
        {
            // Get the dungeon component
            dungeonScript = GetComponent<Dungeon>();

            // Get the dungeon builder component and set the dungeon creation not asyncrounus
            dungeonBuilder = GetComponent<DungeonBuilder>();
            dungeonBuilder.asyncBuild = false;

            // Get the dungeon name from the Dungeon object
            string dungeonThemeName = dungeonScript.dungeonThemes[0].name;

            // Get the number of levels for the dungeon
            DungeonLevelsJsonMap dungeonLevelsJsonMap = JsonDatabase.Instance.GetDataFromJson<DungeonLevelsJsonMap>(Utils.DUNGEON_LEVELS_JSON_NAME);
            DungeonLevelsInfo dungeonLevelsInfo = dungeonLevelsJsonMap.GetDungeonByName(dungeonThemeName);

            dungeonIndex = dungeonLevelsJsonMap.Dungeons.FindIndex(dungeonInfo => dungeonInfo.Name == dungeonLevelsInfo.Name);
            maxLevel = dungeonLevelsInfo.Number_Of_Levels;

            // Dungeon current level is initialized using DataBetweenScenes
            currentLevel = (int)DataBetweenScenes.Instance.GetData(DataBetweenScenes.DUNGEON_CURRENT_LEVEL_KEY, 1);

            // Clamp dungeon currentLevel to dungeon maxLevel
            currentLevel = Mathf.Min(currentLevel, maxLevel);

            // Delete key, after that it has been used
            DataBetweenScenes.Instance.RemoveData(DataBetweenScenes.DUNGEON_CURRENT_LEVEL_KEY);
        }

        private void SpawnOtherElements(GridDungeonModel gridModel)
        {
            // Compute the dungeon's valid cells, on which other gameObjects can be spawned
            Cell[] validCells = gridModel.Cells.Where(cell =>
                                                {
                                                    bool containsStair = gridModel.ContainsStairAtLocation(cell.Center.x, cell.Center.z);

                                                    return !containsStair && cell.CellType != CellType.Unknown;
                                                })
                                                .ToArray();

            // Get the positions of all the GameObjects with the Ground tag
            Vector3[] groundPositionArray = GameObject.FindGameObjectsWithTag(Utils.GROUND_TAG)
                                                    .Select(gameObject => gameObject.transform.position)
                                                    .ToArray();

            // Get the grid size of the dungeon model
            Vector3 gridSize = gridModel.Config.GridCellSize;

            // Get the list of enemy positions in rooms
            List<Vector3> groundRoomPositions = groundPositionArray.Where(groundPosition => validCells.Where(cell => cell.CellType == CellType.Room)
                                                                                                        .Any(cell => IsPositionInsideCell(groundPosition, cell, gridSize)))
                                                                    .ToList();

            // Get the list of enemy positions in corridors
            List<Vector3> groundCorridorPositions = groundPositionArray.Where(groundPosition => validCells.Where(cell => cell.CellType != CellType.Room)
                                                                                                            .Any(cell => IsPositionInsideCell(groundPosition, cell, gridSize)))
                                                                        .ToList();

            // Compute the number of chests to spawn in rooms and in corridors
            Tuple<int, int> numberToSpawn = ComputeNumberToSpawnInRoomsAndCorridors(treasureChestMinNumber, treasureChestMaxNumber, treasureChestRoomPercentage);

            // Spawn chests in rooms
            SpawnChests(numberToSpawn.Item1, groundRoomPositions, gridSize);

            // Spawn chests in corridors
            SpawnChests(numberToSpawn.Item2, groundCorridorPositions, gridSize);

            // Compute the number of enemies to spawn in rooms and in corridors
            numberToSpawn = ComputeNumberToSpawnInRoomsAndCorridors(enemyMinNumber, enemyMaxNumber, enemyRoomPercentage);

            // Spawn enemies in rooms
            SpawnEnemies(numberToSpawn.Item1, groundRoomPositions, gridSize);

            // Spawn enemies in corridors
            SpawnEnemies(numberToSpawn.Item2, groundCorridorPositions, gridSize);
        }

        /// <summary>
        /// Compute the number of elements to spawn in rooms and in corridors
        /// </summary>
        /// <param name="minNumber">The min number of elements to spawn</param>
        /// <param name="maxNumber">The min number of elements to spawn</param>
        /// <param name="roomPercentage">The percentage of elements to spawn in rooms only</param>
        /// <returns>A tuple with 2 values: the number of elements to spawn in rooms and corridors</returns>
        private Tuple<int, int> ComputeNumberToSpawnInRoomsAndCorridors(int minNumber, int maxNumber, float roomPercentage)
        {
            Tuple<int, int> tupleWeightedCount = ComputeWeightedMinMaxValue(minNumber, maxNumber);
            int numberToSpawnByDungeonLevel = Random.Range(tupleWeightedCount.Item1, tupleWeightedCount.Item2 + 1);

            int numberToSpawnInRooms = Mathf.FloorToInt(roomPercentage * numberToSpawnByDungeonLevel);
            numberToSpawnInRooms = Mathf.Clamp(numberToSpawnInRooms, 0, numberToSpawnByDungeonLevel);
            
            int numberToSpawnInCorridors = numberToSpawnByDungeonLevel - numberToSpawnInRooms;

            return new Tuple<int, int>(numberToSpawnInRooms, numberToSpawnInCorridors);
        }

        /// <summary>
        /// Compute a weighted min and max value, taking into account the current dungeon level
        /// </summary>
        /// <param name="minValue">The min value that has to be weighted</param>
        /// <param name="maxValue">The max value that has to be weighted</param>
        /// <returns>A tuple with 2 values: the weithed min and max values</returns>
        public Tuple<int, int> ComputeWeightedMinMaxValue(int minValue, int maxValue)
        {
            // Compute a weight taking into account the dungeon current level
            float weight = (float)(currentLevel - 1) / (this.maxLevel - 1);

            // Calculate the min and max weighted value taking into account the weight
            int weightedMinValue = minValue + Mathf.FloorToInt(weight * (maxValue - minValue));
            int weightedMaxValue = maxValue - Mathf.FloorToInt((1 - weight) * (maxValue - (minValue + 1)));

            return new Tuple<int, int>(weightedMinValue, weightedMaxValue);
        }

        private bool IsPositionInsideCell(Vector3 position, Cell cell, Vector3 gridSize)
        {
            Bounds worldBound = cell.GetWorldBounds(gridSize);

            float upperX = worldBound.center.x + worldBound.extents.x;
            float lowerX = worldBound.center.x - worldBound.extents.x;
            float upperZ = worldBound.center.z + worldBound.extents.z;
            float lowerZ = worldBound.center.z - worldBound.extents.z;

            return position.x >= lowerX && position.x <= upperX &&
                    position.z >= lowerZ && position.z <= upperZ;
        }

        private void SpawnChests(int chestToSpawn, List<Vector3> positions, Vector3 gridSize)
        {
            for (int i = 0; i < chestToSpawn && positions.Count > 0; i++)
            {
                // Get a random index for the ground positions list
                int randomSpawnPositionIndex = Random.Range(0, positions.Count);
                
                // Get a random ground position (it's the ground center)
                Vector3 groundCenterPosition = positions[randomSpawnPositionIndex];

                // Compute the rotation of the chest (as a multiple of 90 degrees)
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90, 0);

                // Remove the ground position from the list, since it has been used
                positions.RemoveAt(randomSpawnPositionIndex);

                // Instantiate the chest
                GameObject treasureChest = Instantiate(treasureChestPrefab, groundCenterPosition, rotation);

                // Randomize chest local position
                // The half bound multiplier is 0.4 rather than 0.5,
                // in order to avoid moving the chest to the edge of the ground marker
                // (before the dungeon is created, the ground is a marker with a size equal to gridSize) 
                float halfBoundMultiplier = 0.4f;
                float newX = treasureChest.transform.localPosition.x + Random.Range(-halfBoundMultiplier * gridSize.x, halfBoundMultiplier * gridSize.x);
                float newZ = treasureChest.transform.localPosition.z + Random.Range(-halfBoundMultiplier * gridSize.z, halfBoundMultiplier * gridSize.z);
                treasureChest.transform.localPosition = new Vector3(newX, treasureChest.transform.localPosition.y, newZ);

                // Fix occasional interpenetrasions
                FixGameObjectInterpenetrations(treasureChest, gridSize);

                // Fix the room chests rotation, if they are facing a near wall
                FixChestOrientation(treasureChest, gridSize);
            }
        }

        private void FixChestOrientation(GameObject treasureChest, Vector3 gridSize)
        {
            // Check if the chest is facing a wall.
            // If so, turn it by 180 degrees.
            Vector3 start = treasureChest.transform.position + Vector3.up;

            // Get the max distance for the raycast (mean of half the grid size X and Z)
            float maxDistance = (0.5f * gridSize.x + 0.5f * gridSize.z) * 0.5f;

            // Get the ground layer mask
            LayerMask groundLayerMask = LayerMask.GetMask(Utils.GROUND_LAYER_NAME);

            // If the chest has a near wall to its left or right, turn it by 90 degrees
            if (Physics.Raycast(start, treasureChest.transform.right, maxDistance, groundLayerMask, QueryTriggerInteraction.Ignore) ||
                Physics.Raycast(start, -treasureChest.transform.right, maxDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                treasureChest.transform.rotation *= Quaternion.Euler(0, 90, 0);
            }

            // If the chest is facing a near wall, turn it by 180 degrees
            if (Physics.Raycast(start, treasureChest.transform.forward, maxDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                treasureChest.transform.rotation *= Quaternion.Euler(0, 180, 0);
            }
        }

        private void SpawnEnemies(int enemyToSpawn, List<Vector3> positions, Vector3 gridSize)
        {
            // Get the enemy prefab that can be spawned in the current level
            GameObject[] enemyPrefabToSpawnInLevel = enemyTypePerLevelArray.Where(enemyTypePerLevel => enemyTypePerLevel.MinLevelToSpawn <= currentLevel)
                                                                        .Select(enemyTypePerLevel => enemyTypePerLevel.EnemyPrefab)
                                                                        .ToArray();

            for (int i = 0; i < enemyToSpawn && positions.Count > 0; i++)
            {
                // Get a random index for the enemy prefab array
                int randomEnemyPrefabIndex = Random.Range(0, enemyPrefabToSpawnInLevel.Length);

                // Get a random enemy among the ones that can be spawned in the current level
                GameObject enemyPrefab = enemyPrefabToSpawnInLevel[randomEnemyPrefabIndex];

                // Get a random index for the ground positions list
                int randomSpawnPositionIndex = Random.Range(0, positions.Count);

                // Get a random ground position (it's the ground center)
                Vector3 groundCenterPosition = positions[randomSpawnPositionIndex];

                // Compute the rotation of the enemy
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                // Remove the ground position from the list, since it has been used
                positions.RemoveAt(randomSpawnPositionIndex);

                // Instantiate the enemy
                GameObject enemy = Instantiate(enemyPrefab, groundCenterPosition, rotation);

                // Randomize enemy local position
                // The half bound multiplier is 0.4 rather than 0.5,
                // in order to avoid moving the enemy to the edge of the ground marker
                // (before the dungeon is created, the ground is a marker with a size equal to gridSize) 
                float halfBoundMultiplier = 0.4f;
                float newX = enemy.transform.localPosition.x + Random.Range(-halfBoundMultiplier * gridSize.x, halfBoundMultiplier * gridSize.x);
                float newZ = enemy.transform.localPosition.z + Random.Range(-halfBoundMultiplier * gridSize.z, halfBoundMultiplier * gridSize.z);
                enemy.transform.localPosition = new Vector3(newX, enemy.transform.localPosition.y, newZ);

                // Fix occasional interpenetrasions
                FixGameObjectInterpenetrations(enemy, gridSize);
            }
        }

        private void FixGameObjectInterpenetrations(GameObject gameObject, Vector3 gridSize)
        {
            Collider[] gameObjectColliders = gameObject.GetComponentsInChildren<Collider>()
                                                .Where(collider => collider.enabled && !collider.isTrigger)
                                                .ToArray();

            // Get the layer mask
            LayerMask layerMask = LayerMask.GetMask(Utils.GROUND_LAYER_NAME, Utils.OBSTACLES_LAYER_NAME);

            // Comput the sphere radius for the OverlapSphere (mean of half the grid size X and Z)
            float sphereRadius = (0.5f * gridSize.x + 0.5f * gridSize.z) * 0.5f;

            // Get all the physical colliders (the not trigger ones) that are inside the a cell of size grid size,
            // making exception for the ones of the ground gameObjects
            Collider[] interpenetratedColliders = Physics.OverlapSphere(gameObject.transform.position, sphereRadius, layerMask, QueryTriggerInteraction.Ignore)
                                                        .Where(collider => !collider.gameObject.CompareTag(Utils.GROUND_TAG) && !gameObjectColliders.Contains(collider))
                                                        .ToArray();
            
            foreach (Collider collider in gameObjectColliders)
            {
                foreach (Collider otherCollider in interpenetratedColliders)
                {
                    if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                                                    otherCollider, otherCollider.transform.position, otherCollider.transform.rotation,
                                                    out Vector3 direction, out float distance))
                    {
                        gameObject.transform.position += direction * distance;
                    }
                }
            }
        }
    }
}