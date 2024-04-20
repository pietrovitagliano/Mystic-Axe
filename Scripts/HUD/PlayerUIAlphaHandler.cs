// Author: Pietro Vitagliano

using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    public class PlayerUIAlphaHandler : UIAlphaHandler
    {
        private PlayerStatusHandler playerStatusHandler;
        private ManaHandler manaHandler;
        private CurrenciesHandler currenciesHandler;
        private PlayerConsumableHandler playerConsumableHandler;

        [Header("UI Show & Hide Components Settings")]
        [SerializeField] private float minEnemyDistanceToShowPlayerStats = 10f;
        
        protected override void Start()
        {
            base.Start();
            
            playerStatusHandler = character.GetComponent<PlayerStatusHandler>();
            manaHandler = character.GetComponent<ManaHandler>();
            currenciesHandler = character.GetComponent<CurrenciesHandler>();
            playerConsumableHandler = character.GetComponent<PlayerConsumableHandler>();

            manaHandler.OnManaRestoredEvent.AddListener(ShowCharacterUIOnEvent);
            manaHandler.OnManaConsumedEvent.AddListener(ShowCharacterUIOnEvent);
            currenciesHandler.OnGoldAmountChangedEvent.AddListener(ShowCharacterUIOnEvent);
            playerConsumableHandler.OnConsumableAmountChangedEvent.AddListener(ShowCharacterUIOnEvent);
            playerConsumableHandler.OnSelectNextConsumableEvent.AddListener(ShowCharacterUIOnEvent);
            playerConsumableHandler.OnDungeonKeyObtainedEvent.AddListener(ShowCharacterUIOnEvent);
        }

        protected override void InitializeCharacter()
        {
            character = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);
        }

        protected override void HandleUIAppearence()
        {
            bool showPlayerStatsCondition = playerStatusHandler.IsTargetingEnemy || playerStatusHandler.IsAttacking ||
                                            playerStatusHandler.IsCastingSkill || playerStatusHandler.IsAiming || playerStatusHandler.IsBlocking;


            if (showPlayerStatsCondition)
            {
                ShowCharacterUI();
            }
            else
            {
                Vector3 playerChest = Utils.GetCharacterHeightPosition(character.transform, Utils.UPPER_CHEST_HEIGHT);
                int enemyLayerMask = LayerMask.GetMask(Utils.ENEMY_LAYER_NAME);
                bool isPlayerNearToEnemies = Physics.OverlapSphere(playerChest, minEnemyDistanceToShowPlayerStats, enemyLayerMask, QueryTriggerInteraction.Ignore)
                                                    .Where(collider => collider.gameObject.GetComponent<Targetable>() != null).Count() > 0;

                if (isPlayerNearToEnemies)
                {
                    ShowCharacterUI();
                }
                else
                {
                    HideCharacterUI(delay: hideUIDelay);
                }
            }
        }
    }
}
