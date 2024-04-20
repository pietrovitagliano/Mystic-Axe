// Author: Pietro Vitagliano

using UnityEngine;
using UnityEngine.UI;

namespace MysticAxe
{
    public class PlayerLevelSystemUIHandler : LevelSystemUIHandler
    {
        [Header("UI Components")]
        [SerializeField] private Image expBar;

        [Header("EXP Bar Settings")]
        [SerializeField, Range(0.1f, 5f)] private float expBarFillingTime = 2f;

        protected override void InitializeUI()
        {
            base.InitializeUI();

            InitializeExpBar();
        }

        protected override void Update()
        {
            base.Update();
            
            UpdateExpBar();
        }

        protected override void UpdateLevelNumber()
        {
            if (levelNumberLabel.text.ToLower() != levelSystemHandler.CurrentLevel.ToString().ToLower())
            {
                levelNumberLabel.text = levelSystemHandler.CurrentLevel.ToString().ToUpper();
                expBar.fillAmount = 0;
            }
        }

        private void InitializeExpBar()
        {
            PlayerLevelSystemHandler playerLevelSystemHandler = (PlayerLevelSystemHandler)levelSystemHandler;
            
            // Set the current value of the exp bar
            expBar.fillAmount = (float)playerLevelSystemHandler.CurrentExp / playerLevelSystemHandler.ExpToNextLevel;
        }

        private void UpdateExpBar()
        {
            PlayerLevelSystemHandler playerLevelSystemHandler = (PlayerLevelSystemHandler)levelSystemHandler;

            float fillTarget = (float)playerLevelSystemHandler.CurrentExp / playerLevelSystemHandler.ExpToNextLevel;

            expBar.fillAmount = Utils.FloatInterpolation(expBar.fillAmount, fillTarget, Time.deltaTime / expBarFillingTime);
        }
    }
}