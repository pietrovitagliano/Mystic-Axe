// Author: Pietro Vitagliano

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MysticAxe.Reward.DungeonToRarityLevelJsonMap;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MysticAxe
{
    [Serializable]
    public class Reward
    {
        [Min(0)] private int gold = 0;
        private HashSet<Consumable> consumableSet;
        private bool hasDungeonKey = false;
        private DungeonToRarityLevelInfo dungeonToRarityLevelInfo;

        [Serializable]
        public class DungeonToRarityLevelJsonMap
        {
            [SerializeField] private List<DungeonToRarityLevelInfo> rarityLevelProbability;

            public List<DungeonToRarityLevelInfo> RarityLevelProbability { get => rarityLevelProbability; }

            [Serializable]
            public class DungeonToRarityLevelInfo
            {
                // probabilityRarityLvl3 is not reported, since it is equal to 1 - (probabilityRarityLvl1 + probabilityRarityLvl2)
                [SerializeField, Range(0, 1)] private float probabilityRarityLvl1;
                [SerializeField, Range(0, 1)] private float probabilityRarityLvl2;

                public int GetRandomRarity()
                {
                    int rarity;
                    float randomValue = Random.Range(0f, 1f);
                    if (randomValue < probabilityRarityLvl1)
                    {
                        rarity = 1;
                    }
                    else if (randomValue < probabilityRarityLvl1 + probabilityRarityLvl2)
                    {
                        rarity = 2;
                    }
                    else
                    {
                        rarity = 3;
                    }

                    return rarity;
                }
            }
        }

        [Header("Difficulty Settings")]
        private const int maxDungeonDifficulty = 5;
        [SerializeField, Range(1, maxDungeonDifficulty)] private int dungeonDifficulty;
        [SerializeField] private bool dungeonBasedDifficulty = true;

        [Header("Empty Reward Settings")]
        [SerializeField, Range(0, 1)] private float notEmptyRewardProbability = 1f;

        [Header("Gold Settings")]
        [SerializeField, Range(0, 1)] private float probabilityToGetGold = 0.8f;
        [SerializeField, Range(1, 1000)] private int maxBaseGold = 200;
        [SerializeField, Range(0.1f, 1)] private float goldMinMultiplier = 0.85f;
        [SerializeField, Range(1, 3)] private float goldMaxMultiplier = 1.6f;

        [Header("Consumable Settings")]
        [SerializeField, Range(1, 10)] private int maxConsumableNumber = 4;
        [SerializeField, Range(0, 1)] private float oneConsumableAtLeastProbability = 0f;
        [SerializeField, Range(0, 1)] private float maxConsumableNumberProbability = 0.1f;
        [SerializeField, Range(1, 10)] private int minConsumableAmount = 1;
        [SerializeField, Range(1, 10)] private int maxConsumableAmount = 3;


        public bool HasDungeonKey { get => hasDungeonKey; set => hasDungeonKey = value; }

        
        public Reward InitializeReward()
        {
            bool notEmptyReward = Random.value <= notEmptyRewardProbability;
            if (notEmptyReward || hasDungeonKey)
            {
                InitializeRewardParams();

                if (notEmptyReward)
                {
                    gold = GenerateRandomGold();
                    consumableSet = GenerateRandomConsumables();
                }

                return this;
            }

            return null;
        }

        private void InitializeRewardParams()
        {
            // Get dungeon to rarity level json map
            DungeonToRarityLevelJsonMap dungeonToRarityLevelJsonMap = JsonDatabase.Instance.GetDataFromJson<DungeonToRarityLevelJsonMap>(Utils.DUNGEON_TO_RARITY_LEVEL_JSON_NAME);
            
            // If the difficulty has not to be handled manually
            if (dungeonBasedDifficulty)
            {
                // Get dungeon generation handler
                DungeonGenerationHandler dungeonGenerationHandler = Object.FindObjectOfType<DungeonGenerationHandler>();

                // Initialize dungeon difficulty
                dungeonDifficulty = dungeonGenerationHandler.DungeonIndex + 1;
            }

            // Get dungeonToRarityLevelInfo object in order to be able to generate random rarity level
            dungeonToRarityLevelInfo = dungeonToRarityLevelJsonMap.RarityLevelProbability[dungeonDifficulty - 1];
        }

        private int GenerateRandomGold()
        {
            if (Random.value <= probabilityToGetGold)
            {
                int minBaseGold = Mathf.CeilToInt(0.7f * maxBaseGold * dungeonDifficulty / maxDungeonDifficulty);
                minBaseGold = Mathf.Max(1, minBaseGold);

                int rewardLevel = dungeonToRarityLevelInfo.GetRandomRarity();
                int baseGold = Random.Range(minBaseGold, maxBaseGold + 1);
                float multiplierFactor = Random.Range(goldMinMultiplier, goldMaxMultiplier);
                
                return Mathf.CeilToInt(baseGold * rewardLevel * dungeonDifficulty * multiplierFactor);
            }

            return 0;
        }

        private HashSet<Consumable> GenerateRandomConsumables()
        {
            HashSet<Consumable> allDefinedConsumableSet = Consumable.GetAllDefinedConsumableSet();

            // Clamp maxConsumableNumber to a minimum equal to the number of defined consumables
            int maxConsumableNumber = Mathf.Min(this.maxConsumableNumber, allDefinedConsumableSet.Count);

            // Generate the number of consumables that need to be added to the reward
            int consumableNumber = Utils.GenerateRandomValue(maxValue: maxConsumableNumber, maxValueProbability: maxConsumableNumberProbability);

            // If consumableNumber is 0 and there is no gold in the reward or the probability condition is satisfied,
            // at least one consumable is guaranteed
            if (consumableNumber == 0 && (gold == 0 || Random.value <= oneConsumableAtLeastProbability))
            {
                consumableNumber = 1;
            }
            
            HashSet<Consumable> generatedConsumableSet = new HashSet<Consumable>();
            for (int i = 0; i < consumableNumber; i++)
            {
                // Compute the rarity of the consumable
                int consumableLevel = dungeonToRarityLevelInfo.GetRandomRarity();

                // Compute the amount of the consumable
                int amount = Random.Range(minConsumableAmount, maxConsumableAmount + 1);

                // Generate a random index to get a random consumable
                int randomIndex = Random.Range(0, allDefinedConsumableSet.Count);

                // Get the consumable at the random index and initialize it
                Consumable randomConsumable = allDefinedConsumableSet.ToList()[randomIndex];
                randomConsumable.InitializeConsumable(amount, consumableLevel);

                // Remove the consumable from the consumable set
                allDefinedConsumableSet.Remove(randomConsumable);

                // Add the consumable to the generated consumable set
                generatedConsumableSet.Add(randomConsumable);
            }

            return generatedConsumableSet;
        }
        
        public void GiveReward(GameObject player)
        {
            if (player.TryGetComponent(out PlayerConsumableHandler playerConsumableHandler))
            {
                if (consumableSet != null && consumableSet.Count > 0)
                {
                    playerConsumableHandler.AddConsumables(consumableSet.ToArray());
                    AudioManager.Instance.PlayMutuallyExclusiveSound(Utils.consumableCollectedAudioName, AudioManager.Instance.gameObject);
                }

                if (hasDungeonKey && !playerConsumableHandler.HasDungeonKey)
                {
                    playerConsumableHandler.AddDungeonKey();
                }
            }

            if (gold > 0 && player.TryGetComponent(out CurrenciesHandler currenciesHandler))
            {
                currenciesHandler.AddGold(gold);
            }
        }
    }
}
