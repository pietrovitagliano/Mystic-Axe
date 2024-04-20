// Author: Pietro Vitagliano

using UnityEngine;
using UnityEngine.Events;

namespace MysticAxe
{
    [RequireComponent(typeof(LevelUpEffectHandler))]
    public abstract class LevelSystemHandler : MonoBehaviour
    {
        [Header("Level Settings")]
        [SerializeField, Range(1, 100)] private int currentLevel = 1;
        [SerializeField, Range(1, 100)] private int maxLevel = 20;
        [SerializeField, Range(1, 100)] private float percentageDamagePerLevelIncrease = 8f;

        [Min(1)] private float damageMultiplier = 1f;

        private LevelUpEffectHandler levelUpEffectHandler;

        private readonly UnityEvent onLevelUpEvent = new UnityEvent();
        
        
        public int CurrentLevel { get => currentLevel; protected set => currentLevel = value; }
        public int MaxLevel { get => maxLevel; }
        public float DamageMultiplier { get => damageMultiplier; }
        public UnityEvent OnLevelUpEvent { get => onLevelUpEvent; }
        protected LevelUpEffectHandler LevelUpEffectHandler { get => levelUpEffectHandler; }

        protected virtual void Start()
        {
            levelUpEffectHandler = GetComponent<LevelUpEffectHandler>();
        }

        protected virtual void Update()
        {
            UpdateDamageMultiplier();
        }
        
        protected void PlayLevelUpEffect()
        {
            levelUpEffectHandler.PlayEffect();
        }

        private void UpdateDamageMultiplier()
        {
            damageMultiplier = 1 + (currentLevel - 1) * percentageDamagePerLevelIncrease * 0.01f;
        }
    }
}