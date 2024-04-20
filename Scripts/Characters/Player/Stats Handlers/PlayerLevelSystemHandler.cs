// Author: Pietro Vitagliano

using System;
using UnityEngine;

namespace MysticAxe
{
    public class PlayerLevelSystemHandler : LevelSystemHandler
    {
        [Header("Player Experience Settings")]
        [SerializeField, Range(1, 200)] private int expToNextLevelFactor = 100;

        private int currentExp = 0;
        private int expToNextLevel;
        private PlayerStatusHandler playerStatusHandler;

        public int CurrentExp { get => currentExp; }
        public int ExpToNextLevel { get => expToNextLevel; }

        
        protected override void Start()
        {
            base.Start();

            // Player current experience is initialized using DataBetweenScenes
            // (The player has to start from level 1, but just for testing he starts from level 9) <- DELETE
            //CurrentLevel = (int)DataBetweenScenes.Instance.GetData(DataBetweenScenes.PLAYER_CURRENT_LEVEL_KEY, 1);
            CurrentLevel = (int)DataBetweenScenes.Instance.GetData(DataBetweenScenes.PLAYER_CURRENT_LEVEL_KEY, 9);

            // Clamp current level to max level
            CurrentLevel = Mathf.Min(CurrentLevel, MaxLevel);
            
            // Delete key, after that it has been used
            DataBetweenScenes.Instance.RemoveData(DataBetweenScenes.PLAYER_CURRENT_LEVEL_KEY);

            // Set the exp required for the next level,
            // based on the current level
            expToNextLevel = ComputeExpToNextLevel();

            // Player current experience is initialized using DataBetweenScenes
            currentExp = (int)DataBetweenScenes.Instance.GetData(DataBetweenScenes.PLAYER_CURRENT_EXP_KEY, 0);

            // Delete key, after that it has been used
            DataBetweenScenes.Instance.RemoveData(DataBetweenScenes.PLAYER_CURRENT_EXP_KEY);

            // Get the PlayerStatusHandler component
            playerStatusHandler = GetComponent<PlayerStatusHandler>();
        }

        public void AddExp(int exp)
        {
            if (!playerStatusHandler.IsDead && CurrentLevel < MaxLevel)
            {
                currentExp += exp;

                if (currentExp >= expToNextLevel)
                {
                    int expOverflow = currentExp - expToNextLevel;

                    LevelUp(expOverflow);
                }
            }
        }

        private void LevelUp(int expOverflow)
        {
            CurrentLevel++;
            OnLevelUpEvent.Invoke();
            PlayLevelUpEffect();

            if (CurrentLevel < MaxLevel)
            {
                expToNextLevel = ComputeExpToNextLevel();
                currentExp = 0;

                AddExp(expOverflow);
            }
            else
            {
                expToNextLevel = 0;
                currentExp = ComputeExpToNextLevel();
            }
        }

        private int ComputeExpToNextLevel()
        {
            int nextLevel = CurrentLevel < MaxLevel ? CurrentLevel + 1 : MaxLevel;

            // Compute exp required for next level in a way similar to the Dark Souls games
            return (int)(Mathf.Pow(nextLevel, 2) * expToNextLevelFactor);
        }
    }
}