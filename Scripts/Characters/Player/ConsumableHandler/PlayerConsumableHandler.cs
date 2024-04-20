// Author: Pietro Vitagliano

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MysticAxe
{
    [RequireComponent(typeof(PlayerAnimatorHandler))]
    [RequireComponent(typeof(PlayerStatusHandler))]
    public class PlayerConsumableHandler : ConsumableHandler
    {
        private int consumableSelectedIndex = 0;
        private bool hasDungeonKey = false;
        private PlayerAnimatorHandler playerAnimatorHandler;
        private PlayerStatusHandler playerStatusHandler;
        private InputHandler inputHandler;
        
        private readonly UnityEvent onSelectNextConsumableEvent = new UnityEvent();
        private readonly UnityEvent onDungeonKeyObtainedEvent = new UnityEvent();

        public int ConsumableSelectedIndex { get => consumableSelectedIndex; }
        public bool HasDungeonKey { get => hasDungeonKey; }
        public UnityEvent OnDungeonKeyObtainedEvent => onDungeonKeyObtainedEvent;
        public UnityEvent OnSelectNextConsumableEvent => onSelectNextConsumableEvent;


        protected override void Start()
        {
            base.Start();
            
            playerAnimatorHandler = GetComponent<PlayerAnimatorHandler>();
            playerStatusHandler = GetComponent<PlayerStatusHandler>();
            inputHandler = InputHandler.Instance;

            // Player consumable list and index are initialized using DataBetweenScenes
            ConsumableList = (List<Consumable>)DataBetweenScenes.Instance.GetData(DataBetweenScenes.PLAYER_CONSUMABLE_LIST_KEY, new List<Consumable>());
            consumableSelectedIndex = (int)DataBetweenScenes.Instance.GetData(DataBetweenScenes.PLAYER_CONSUMABLE_INDEX_KEY, 0);
            
            // Delete keys, after that they have been used
            DataBetweenScenes.Instance.RemoveData(DataBetweenScenes.PLAYER_CONSUMABLE_LIST_KEY);
            DataBetweenScenes.Instance.RemoveData(DataBetweenScenes.PLAYER_CONSUMABLE_INDEX_KEY);
        }

        protected override void Update()
        {
            base.Update();
            HandleSelectedConsumableIndex();
        }

        private void HandleSelectedConsumableIndex()
        {
            // Handle the update of the index when the selected consumable is removed and it is the last of the list
            if (ConsumableList.Count > 0 && consumableSelectedIndex == ConsumableList.Count)
            {
                consumableSelectedIndex = ConsumableList.Count - 1;
            }

            if (ConsumableList.Count <= 1)
            {
                consumableSelectedIndex = 0;
            }
            else if (playerStatusHandler.WantsToChangeConsumable)
            {
                inputHandler.ChangeConsumablePressed = false;
                consumableSelectedIndex = consumableSelectedIndex < ConsumableList.Count - 1 ? consumableSelectedIndex + 1 : 0;

                AudioManager.Instance.PlaySound(Utils.UINavigationAudioName, AudioManager.Instance.gameObject);
                onSelectNextConsumableEvent.Invoke();
            }
        }

        protected override void HandleConsume()
        {
            if (playerStatusHandler.WantsToUseConsumable && ConsumableList.Count > 0)
            {
                inputHandler.UseConsumablePressed = false;

                if (ConsumableList[consumableSelectedIndex].Amount > 0)
                {
                    playerStatusHandler.IsUsingConsumable = true;
                    playerAnimatorHandler.Animator.SetTrigger(playerAnimatorHandler.DrinkPotionHash);

                    if (HolderHand.parent.gameObject == WeaponHolder.parent.gameObject)
                    {
                        ChangeWeaponMeshesVisibility(false);

                        if (WeaponHolder.GetComponentInChildren<WeaponStatusHandler>() != null)
                        {
                            WeaponHolder.GetComponentInChildren<AxeGraphicsEffectsHandler>().SetThunderBlessingVFXAlpha(alpha: 0);
                        }
                    }

                    IstantiateConsumable(ConsumableList[consumableSelectedIndex]);
                }
            }
        }

        protected override void IstantiateConsumable(Consumable consumable)
        {
            InstantiatedConsumable = Instantiate(consumable.Prefab, HolderHand);
            Utils.ResetTransformLocalPositionAndRotation(InstantiatedConsumable.transform);
        }

        protected override void OnConsumableUsed()
        {
            ConsumableList[consumableSelectedIndex].Consume(transform.root, InstantiatedConsumable);
        }

        protected override void OnDestroyConsumable()
        {
            if (InstantiatedConsumable != null)
            {
                Destroy(InstantiatedConsumable);
                playerStatusHandler.IsUsingConsumable = false;

                if (HolderHand.parent.gameObject == WeaponHolder.parent.gameObject)
                {
                    ChangeWeaponMeshesVisibility(true);

                    if (WeaponHolder.GetComponentInChildren<WeaponStatusHandler>() != null)
                    {
                        WeaponHolder.GetComponentInChildren<AxeGraphicsEffectsHandler>().SetThunderBlessingVFXAlpha(alpha: 1);
                    }
                }
            }
        }

        public void AddDungeonKey()
        {
            // Turn on the flag to indicate that the player has the dungeon key
            hasDungeonKey = true;

            // Invoke the event
            onDungeonKeyObtainedEvent.Invoke();

            // Play key obtained sound
            AudioManager.Instance.PlaySound(Utils.keyObtainedAudioName, AudioManager.Instance.gameObject);
        }

        public void UseDungeonKey()
        {
            // Turn off the flag to indicate that
            // the player hasn't the dungeon key anymore
            hasDungeonKey = false;
        }
    }
}