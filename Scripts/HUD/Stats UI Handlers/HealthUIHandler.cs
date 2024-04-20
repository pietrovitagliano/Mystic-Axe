// Author: Pietro Vitagliano

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MysticAxe
{
    public class HealthUIHandler : CharacterUIStat
    {
        [Header("Health Bar Settings")]
        [SerializeField, Range(0.1f, 5f)] private float healthBarFillingTime = 2f;
        [SerializeField, Range(0.1f, 5f)] private float timeToWaitBeforeMovingRedBar = 1.2f;
        [SerializeField] private Image greenHealthBar;
        [SerializeField] private Image redHealthBar;

        private float timeElapsedFromDamage = 0;
        private Coroutine timeElapsedHandlerCoroutine;

        private HealthHandler healthHandler = null;


        protected override void InitializeUI()
        {
            InitializeHealthHandler();
            timeElapsedFromDamage = timeToWaitBeforeMovingRedBar;
        }

        protected void Update()
        {
            UpdateHealthBar();
        }

        private void InitializeHealthHandler()
        {
            healthHandler = character.GetComponent<HealthHandler>();
            healthHandler.OnHealthLostEvent.AddListener(OnDamageTaken);
        }

        private void UpdateHealthBar()
        {
            float greenBarAmount = greenHealthBar.fillAmount;
            float redBarAmount = redHealthBar.fillAmount;

            // Health bar is decreasing only after hit.
            // This doesn't work for other scenarios,
            // for example when changing target with lower health.
            // This task is handled by UpdateUI().
            if (greenBarAmount < redBarAmount)
            {
                if (timeElapsedFromDamage >= timeToWaitBeforeMovingRedBar)
                {
                    UpdateRedBar();
                }
            }
            else
            {
                float targetGreenBarAmount = (float)healthHandler.CurrentHealth / healthHandler.MaxHealth;
                    
                // Health bar is filling
                if (greenBarAmount <= targetGreenBarAmount)
                {
                    UpdateGreenBar();
                    UpdateRedBar();
                }
            }
        }

        private void UpdateRedBar()
        {
            float greenBarAmount = greenHealthBar.fillAmount;
            float redBarAmount = redHealthBar.fillAmount;

            redHealthBar.fillAmount = Utils.FloatInterpolation(redBarAmount, greenBarAmount, Time.deltaTime / healthBarFillingTime);
        }

        private void UpdateGreenBar()
        {
            float targetGreenBarAmount = (float)healthHandler.CurrentHealth / healthHandler.MaxHealth;
            greenHealthBar.fillAmount = Utils.FloatInterpolation(greenHealthBar.fillAmount, targetGreenBarAmount, Time.deltaTime / healthBarFillingTime);
        }

        private void OnDamageTaken()
        {
            greenHealthBar.fillAmount = (float)healthHandler.CurrentHealth / healthHandler.MaxHealth;

            if (timeElapsedHandlerCoroutine != null)
            {
                StopCoroutine(timeElapsedHandlerCoroutine);
            }

            timeElapsedHandlerCoroutine = StartCoroutine(HandleTimeElapsedFromDamageCoroutine());
        }

        private IEnumerator HandleTimeElapsedFromDamageCoroutine()
        {
            timeElapsedFromDamage = 0;
            while (timeElapsedFromDamage < timeToWaitBeforeMovingRedBar)
            {
                timeElapsedFromDamage += Time.deltaTime;
                yield return null;
            }

            timeElapsedHandlerCoroutine = null;
        }
    }
}