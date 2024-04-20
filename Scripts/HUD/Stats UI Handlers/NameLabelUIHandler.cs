// Author: Pietro Vitagliano

using TMPro;
using UnityEngine;

namespace MysticAxe
{
    public class NameLabelUIHandler : CharacterUIStat
    {
        [Header("Character Name")]
        [SerializeField] TMP_Text characterNameLabel;

        protected override void InitializeUI() { }

        private void Update()
        {
            UpdateLabel();
        }
        
        private void UpdateLabel()
        {
            string characterName = character.name.Replace("(Clone)", "").Trim();
            if (characterNameLabel.name.ToLower() != characterName.ToLower())
            {
                characterNameLabel.text = characterName.ToUpper();
            }
        }        
    }
}