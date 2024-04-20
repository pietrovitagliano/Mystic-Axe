// Author: Pietro Vitagliano

using TMPro;
using UnityEngine;

namespace MysticAxe
{
    public class LevelSystemUIHandler : CharacterUIStat
    {
        [Header("UI Components")]
        [SerializeField] protected TMP_Text levelNumberLabel;

        protected LevelSystemHandler levelSystemHandler;

        protected override void InitializeUI()
        {
            InitializeLevelSystemHandler();
        }
        
        protected virtual void Update()
        {
            UpdateLevelNumber();
        }

        private void InitializeLevelSystemHandler()
        {
            levelSystemHandler = character.GetComponent<LevelSystemHandler>();
        }

        protected virtual void UpdateLevelNumber()
        {
            if (levelNumberLabel.text.ToLower() != levelSystemHandler.CurrentLevel.ToString().ToLower())
            {
                levelNumberLabel.text = levelSystemHandler.CurrentLevel.ToString().ToUpper();
            }
        }
    }
}