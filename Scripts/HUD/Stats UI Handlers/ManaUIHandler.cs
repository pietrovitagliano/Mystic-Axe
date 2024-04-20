// Author: Pietro Vitagliano

using UnityEngine;
using UnityEngine.UI;

namespace MysticAxe
{
    public class ManaUIHandler : CharacterUIStat
    {        
        [Header("Mana Bar Settings")]
        [SerializeField, Range(0.1f, 5f)] private float manaBarFillingTime = 1.3f;
        [SerializeField] private Image manaBar;

        private ManaHandler manaHandler = null;


        protected override void InitializeUI()
        {
            InitializeManaHandler();
        }

        protected void Update()
        {
            UpdateManaBar();
        }

        private void InitializeManaHandler()
        {
            manaHandler = character.GetComponent<ManaHandler>();
        }

        private void UpdateManaBar()
        {
            float targetManaBar = (float)manaHandler.CurrentMana / manaHandler.MaxMana;
            manaBar.fillAmount = Utils.FloatInterpolation(manaBar.fillAmount, targetManaBar, Time.deltaTime / manaBarFillingTime);
        }
    }
}