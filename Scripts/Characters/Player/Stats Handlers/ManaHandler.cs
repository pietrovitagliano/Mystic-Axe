// Author: Pietro Vitagliano

using UnityEngine;
using UnityEngine.Events;

namespace MysticAxe
{
    public class ManaHandler : MonoBehaviour
    {
        private LevelSystemHandler levelSystemHandler;

        [Header("Mana Settings")]
        [SerializeField, Range(1, 9999)] private int baseMana = 100;
        [SerializeField, Range(1f, 10f)] private float multiplicativeFactorPerLevel = 2;
        [SerializeField] private bool isManaInfinite = false;
        
        [Min(0)] private int currentMana = 1;
        [Min(1)] private int maxMana = 1;
        private int manaPerLevelIncrease;

        private UnityEvent onManaRestoredEvent = new UnityEvent();
        private UnityEvent onManaConsumedEvent = new UnityEvent();

        public int CurrentMana { get => currentMana; }
        public int MaxMana { get => maxMana; }
        public UnityEvent OnManaRestoredEvent { get => onManaRestoredEvent; }
        public UnityEvent OnManaConsumedEvent { get => onManaConsumedEvent; }

        private void Start()
        {
            levelSystemHandler = GetComponent<LevelSystemHandler>();
            levelSystemHandler.OnLevelUpEvent.AddListener(FillMana);

            // Set the mana required for the next level
            // For each level mana is equally increased.
            manaPerLevelIncrease = (int)Mathf.Ceil(multiplicativeFactorPerLevel * baseMana / (levelSystemHandler.MaxLevel - 1));

            UpdateMaxMana();

            // Player current mana is initialized using DataBetweenScenes
            if (levelSystemHandler is PlayerLevelSystemHandler)
            {
                currentMana = (int)DataBetweenScenes.Instance.GetData(DataBetweenScenes.PLAYER_CURRENT_MANA_KEY, maxMana);

                // Clamp current mana to max mana
                currentMana = Mathf.Min(currentMana, maxMana);

                // Delete key, after that it has been used
                DataBetweenScenes.Instance.RemoveData(DataBetweenScenes.PLAYER_CURRENT_MANA_KEY);
            }
            else
            {
                currentMana = maxMana;

                // Enemies have infinite mana
                isManaInfinite = true;
            }
        }

        private void Update()
        {
            if (isManaInfinite)
            {
                FillMana();
            }
            else
            {
                UpdateMaxMana();
            }
        }

        private void UpdateMaxMana()
        {
            maxMana = baseMana + ((levelSystemHandler.CurrentLevel - 1) * manaPerLevelIncrease);
        }

        private void FillMana()
        {
            UpdateMaxMana();
            currentMana = maxMana;
        }
        
        public void RestoreMana(float amount)
        {
            currentMana = Mathf.Clamp(currentMana + (int)amount, 0, maxMana);
            onManaRestoredEvent.Invoke();
        }
        
        public void ConsumeMana(float amount)
        {
            if (currentMana > 0)
            {
                currentMana = Mathf.Clamp(currentMana - (int)amount, 0, maxMana);
                onManaConsumedEvent.Invoke();
            }
        }

        public bool HasEnoughMana(float amount)
        {
            return isManaInfinite || currentMana >= amount;
        }
    }
}