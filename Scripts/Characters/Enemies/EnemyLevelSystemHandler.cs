// Author: Pietro Vitagliano

using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using static MysticAxe.EnemyLevelsByDungeonJsonMap;
using Random = UnityEngine.Random;

namespace MysticAxe
{
    public class EnemyLevelSystemHandler : LevelSystemHandler
    {
        [Header("Enemy Level Up Settings")]
        [SerializeField, Range(0, 1)] private float levelUpProbability = 0.25f;
        [SerializeField, Range(5, 20)] private float levelUpAttemptMinTime = 15;
        [SerializeField, Range(20, 60)] private float levelUpAttemptMaxTime = 40;

        private CharacterStatusHandler characterStatusHandler;
        private VisualEffect vfxAzazelStrength;
        private Coroutine levelUpCoroutineHandler;
        private bool hasLevelledUp = false;

        protected override void Start()
        {
            base.Start();

            InitializeEnemyLevel();

            vfxAzazelStrength = GetComponentsInChildren<VisualEffect>().ToList().Find(vfx => vfx.visualEffectAsset.name == Utils.AZAZEL_STRENGTH_VFX_NAME);
            vfxAzazelStrength.initialEventName = "";

            LevelUpEffectHandler.OnFlashEvent.AddListener(LevelUp);

            characterStatusHandler = GetComponent<CharacterStatusHandler>();
            characterStatusHandler.OnDeathEvent.AddListener(StopAzazelStrengthOnDeath);
        }

        protected override void Update()
        {
            HandleLevelUp();
            
            base.Update();
        }

        // Other enemies' level (such as the boss) could be handled manually from inspector.
        // This is why this method is virtual
        protected virtual void InitializeEnemyLevel()
        {
            // Get dungeon variables necessary to initialize the enemy level
            DungeonGenerationHandler dungeonGenerationHandler = FindObjectOfType<DungeonGenerationHandler>();

            // Get the EnemyLevelPerDungeon object that matches the dungeon name
            EnemyLevelsByDungeonJsonMap dungeonToEnemyLevelJsonMap = JsonDatabase.Instance.GetDataFromJson<EnemyLevelsByDungeonJsonMap>(Utils.ENEMY_LEVELS_BY_DUNGEON_JSON_NAME);
            EnemyLevelsByDungeonInfo enemyLevelPerDungeonInfo = dungeonToEnemyLevelJsonMap.Enemy_Levels_By_Dungeon[dungeonGenerationHandler.DungeonIndex];

            // Calculate the minimum and maximum weighted levels of the enemy,
            // taking into account the dungeon current level
            Tuple<int, int> enemyTupleWeightedLevels = dungeonGenerationHandler.ComputeWeightedMinMaxValue(enemyLevelPerDungeonInfo.Min_Level, enemyLevelPerDungeonInfo.Max_Level);

            // Randomly compute the enemy level
            int enemyLevel = Random.Range(enemyTupleWeightedLevels.Item1, enemyTupleWeightedLevels.Item2 + 1);

            // Clamp the level to the maximum level allowed by the game
            // and assign it to the enemy
            CurrentLevel = Mathf.Min(enemyLevel, MaxLevel);
        }

        private void HandleLevelUp()
        {
            // Check if the modifier has been activated on the current enemy
            bool azazelStrengthActive = characterStatusHandler.GetModifierByID(Utils.AZAZEL_STRENGTH_MODIFIER_ID) != null;

            // Check if the level up can be attempted
            if (!characterStatusHandler.IsDead && !hasLevelledUp && azazelStrengthActive && levelUpCoroutineHandler == null)
            {
                levelUpCoroutineHandler = StartCoroutine(HandleLevelUpCoroutine());
            }
        }

        private IEnumerator HandleLevelUpCoroutine()
        {
            if (Random.value <= levelUpProbability)
            {
                PlayLevelUpEffect();
            }
            else
            {
                float nextLevelUpAttemptTime = Random.Range(levelUpAttemptMinTime, levelUpAttemptMaxTime);
                yield return new WaitForSeconds(nextLevelUpAttemptTime);
            }

            levelUpCoroutineHandler = null;
        }

        private void StopAzazelStrengthOnDeath()
        {
            if (vfxAzazelStrength != null)
            {
                vfxAzazelStrength.Stop();
            }
        }

        private async void LevelUp()
        {
            // The enemy will level up, thus the flag is true
            hasLevelledUp = true;

            float loyaltyToAzazel = characterStatusHandler.GetFeature(Utils.LOYALTY_TO_AZAZEL_FEATURE_NAME).CurrentValue;
            int maxLevelIncrease = Mathf.CeilToInt(loyaltyToAzazel);
            int minLevelIncrease = Mathf.FloorToInt(loyaltyToAzazel * 0.35f);
            minLevelIncrease = Mathf.Max(minLevelIncrease, 1);

            // Compute the level increase
            int levelIncrease = Random.Range(minLevelIncrease, maxLevelIncrease + 1);

            // Wait for the delay after the flash 
            await Utils.AsyncWaitTimeScaled(LevelUpEffectHandler.FlashVanishingDelay);

            // Increase the level
            CurrentLevel = Mathf.Clamp(CurrentLevel + levelIncrease, 1, MaxLevel);

            // Play the azazel strength effect due to level up
            vfxAzazelStrength.Play();

            // Azazel strength gives hyper armor to the enemy
            characterStatusHandler.HasHyperArmor = true;
        }
    }
}