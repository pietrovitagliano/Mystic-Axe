// Author: Pietro Vitagliano

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MysticAxe
{
    public class ConsumableUIHandler : MonoBehaviour
    {
        [Header("Consumable UI Components")]
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject itemInfoParent;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemAmountText;
        private PlayerConsumableHandler playerConsumableHandler;
        
        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);
            playerConsumableHandler = player.GetComponent<PlayerConsumableHandler>();
        }

        private void Update()
        {
            HandleUI();
        }

        private void HandleUI()
        {
            List<Consumable> consumableList = playerConsumableHandler.ConsumableList;
            if (consumableList.Count > 0)
            {
                if (playerConsumableHandler.ConsumableSelectedIndex >= 0 && playerConsumableHandler.ConsumableSelectedIndex < consumableList.Count)
                {
                    Consumable selectedConsumable = consumableList[playerConsumableHandler.ConsumableSelectedIndex];
                    
                    Sprite icon = selectedConsumable.Icon;
                    if (iconImage.sprite == null || iconImage.sprite != icon)
                    {
                        iconImage.color = Color.white;
                        iconImage.sprite = icon;
                    }

                    if (selectedConsumable.Amount > 0)
                    {
                        itemInfoParent.SetActive(true);
                        itemNameText.text = selectedConsumable.Prefab.name;
                        itemAmountText.text = selectedConsumable.Amount.ToString();
                    }
                }
            }
            else
            {
                iconImage.color = Color.clear;
                iconImage.sprite = null;

                itemNameText.text = "";
                itemAmountText.text = "";
                itemInfoParent.SetActive(false);
            }
        }
    }
}