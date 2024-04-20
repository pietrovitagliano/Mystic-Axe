// Author: Pietro Vitagliano

using UnityEngine;
using UnityEngine.UI;

namespace MysticAxe
{
    public class DungeonKeyUIUpdater : MonoBehaviour
    {
        [Header("Dungeon Key UI Components")]
        [SerializeField] private Image iconImage;
        private Color iconColor;
        private PlayerConsumableHandler playerConsumableHandler;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);
            playerConsumableHandler = player.GetComponent<PlayerConsumableHandler>();

            iconColor = iconImage.color;
        }

        private void Update()
        {
            HandleUI();
        }

        private void HandleUI()
        {
            if (playerConsumableHandler.HasDungeonKey)
            {
                iconImage.color = iconColor;
            }
            else
            {
                iconImage.color = Color.clear;
            }
        }
    }
}