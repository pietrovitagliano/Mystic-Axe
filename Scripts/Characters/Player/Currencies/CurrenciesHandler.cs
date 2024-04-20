// Author: Pietro Vitagliano

using UnityEngine;
using UnityEngine.Events;

namespace MysticAxe
{
    public class CurrenciesHandler : MonoBehaviour
    {
        private int goldAmount = 0;
        [SerializeField, Range(9999, 99999)] private int maxGoldAmount = 99999;

        private readonly UnityEvent onGoldAmountChangedEvent = new UnityEvent();

        public int GoldAmount { get => goldAmount; }
        public UnityEvent OnGoldAmountChangedEvent { get => onGoldAmountChangedEvent; }

        private void Start()
        {            
            // Player gold amount is initialized using DataBetweenScenes
            goldAmount = (int)DataBetweenScenes.Instance.GetData(DataBetweenScenes.PLAYER_GOLD_KEY, goldAmount);

            // Clamp gold amount to max gold amount
            goldAmount = Mathf.Min(goldAmount, maxGoldAmount);
            
            // Delete key, after that it has been used
            DataBetweenScenes.Instance.RemoveData(DataBetweenScenes.PLAYER_GOLD_KEY);
        }

        public bool HasEnoughMoney(int amount)
        {
            return goldAmount >= amount;
        }

        public void AddGold(int amount)
        {
            int previousAmount = goldAmount;
            goldAmount = Mathf.Clamp(goldAmount + amount, 0, maxGoldAmount);

            if (goldAmount != previousAmount)
            {
                onGoldAmountChangedEvent.Invoke();
            }
        }

        public void RemoveGold(int amount)
        {
            AddGold(-amount);
        }
    }
}