// Author: Pietro Vitagliano

using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace MysticAxe
{
    public class GoldCounterUIUpdater : MonoBehaviour
    {
        [Header("Numeric Counter")]
        [SerializeField] private TMP_Text numericText;

        [Header("Counter Update Settings")]
        [SerializeField, Range(0.1f, 5)] private float updateDuration = 1.5f;
        private const int minUpdateFrequency = 60;
        private int updateFrequency;

        private float refreshTime;

        private CurrenciesHandler currenciesHandler;
        private Coroutine countingCoroutine;

        
        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);
            currenciesHandler = player.GetComponent<CurrenciesHandler>();
            currenciesHandler.OnGoldAmountChangedEvent.AddListener(UpdateCounter);

            // Initialize the UI with the current gold amount
            numericText.text = currenciesHandler.GoldAmount.ToString();

            // Initialize the counter update frequency and the refresh time
            float fps = 1f / Time.deltaTime;
            updateFrequency = Mathf.Max(minUpdateFrequency, Mathf.CeilToInt(fps));
            refreshTime = 1f / updateFrequency;
        }

        private void UpdateCounter()
        {            
            if (countingCoroutine != null)
            {
                StopCoroutine(countingCoroutine);
            }

            countingCoroutine = StartCoroutine(UpdateCounterCoroutine(newValue: currenciesHandler.GoldAmount));
        }

        private IEnumerator UpdateCounterCoroutine(int newValue)
        {
            int currentValue = Int32.Parse(numericText.text);

            // This operation needs a cast to convert it (a float) to an integer.
            // Anyway, the simple (int) cast is not enough since step amount has always to be greater than 0.
            // That's why the converstion is made by Mathf.CeilToInt, which round up the float passed as param.
            // Indeed, if the result of this operation is 0.3, for example, without CeilToInt, the loop will never end,
            // because the step amount would be 0.
            int stepAmount = Mathf.CeilToInt(Mathf.Abs(newValue - currentValue) * refreshTime / updateDuration);

            // Play Coins Sound
            AudioManager.Instance.PlaySound(Utils.coinsAudioName, AudioManager.Instance.gameObject);
            
            // Update Counter
            while (currentValue != newValue)
            {
                currentValue = (int)Utils.FloatInterpolation(currentValue, newValue, stepAmount);

                numericText.text = currentValue.ToString();

                yield return new WaitForSeconds(refreshTime);
            }
        }
    }
}
