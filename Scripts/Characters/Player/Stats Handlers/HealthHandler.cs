// Author: Pietro Vitagliano

using UnityEngine;
using UnityEngine.Events;

namespace MysticAxe
{
    public class HealthHandler : MonoBehaviour
    {
        private LevelSystemHandler levelSystemHandler;

        [Header("Health Settings")]
        [SerializeField, Range(1, 9999)] private int baseHealth = 200;
        [SerializeField, Range(1f, 10f)] private float multiplicativeFactorPerLevel = 2;

        // At the beginning, current and max health must be > 0,
        // otherwise the character could be considered dead
        [Min(0)] private int currentHealth = 1;
        [Min(1)] private int maxHealth = 1;
        private int healthPerLevelIncrease;

        private readonly UnityEvent onHealthRestoredEvent = new UnityEvent();
        private readonly UnityEvent onHealthLostEvent = new UnityEvent();


        public int CurrentHealth { get => currentHealth; }
        public int MaxHealth { get => maxHealth; }
        public UnityEvent OnHealthRestoredEvent { get => onHealthRestoredEvent; }
        public UnityEvent OnHealthLostEvent { get => onHealthLostEvent; }

        
        private void Start()
        {
            levelSystemHandler = GetComponent<LevelSystemHandler>();

            // Set the health required for the next level
            // For each level health is equally increased.
            healthPerLevelIncrease = (int)Mathf.Ceil(multiplicativeFactorPerLevel * baseHealth / (levelSystemHandler.MaxLevel - 1));

            UpdateMaxHealth();

            // Player current health is initialized using DataBetweenScenes
            if (levelSystemHandler is PlayerLevelSystemHandler)
            {
                currentHealth = (int)DataBetweenScenes.Instance.GetData(DataBetweenScenes.PLAYER_CURRENT_HEALTH_KEY, maxHealth);

                // Clamp current health to max health
                currentHealth = Mathf.Min(currentHealth, maxHealth);

                // Delete key, after that it has been used
                DataBetweenScenes.Instance.RemoveData(DataBetweenScenes.PLAYER_CURRENT_HEALTH_KEY);
            }
            else
            {
                currentHealth = maxHealth;
            }

            levelSystemHandler.OnLevelUpEvent.AddListener(FillHealthWhenLevelUp);
        }

        private void Update()
        {
            UpdateMaxHealth();
            HandleListenersWhenDead();
        }

        private void UpdateMaxHealth()
        {
            maxHealth = baseHealth + ((levelSystemHandler.CurrentLevel - 1) * healthPerLevelIncrease);
        }

        private void FillHealthWhenLevelUp()
        {
            UpdateMaxHealth();
            currentHealth = maxHealth;
        }

        public void RestoreHealth(float amount)
        {
            currentHealth = Mathf.Clamp(currentHealth + (int)amount, 0, maxHealth);
            onHealthRestoredEvent.Invoke();
        }
        
        public void ApplyDamage(float amount)
        {
            if (currentHealth > 0)
            {
                currentHealth = Mathf.Clamp(currentHealth - (int)amount, 0, maxHealth);
                onHealthLostEvent.Invoke();
            }
        }

        public bool IsDead()
        {
            return currentHealth <= 0;
        }

        private void HandleListenersWhenDead()
        {
            if (IsDead())
            {
                onHealthRestoredEvent.RemoveAllListeners();
                onHealthLostEvent.RemoveAllListeners();
            }
        }
    }
}