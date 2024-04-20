// Author: Pietro Vitagliano

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MysticAxe
{
    public abstract class ConsumableHandler : MonoBehaviour
    {
        private List<Consumable> consumableList = new List<Consumable>();
        
        private Transform character;
        private Transform consumableHolderHand;
        private Transform weaponHolder;
        private GameObject instantiatedConsumable = null;

        private UnityEvent onConsumableAmountChangedEvent = new UnityEvent();

        public List<Consumable> ConsumableList { get => consumableList; protected set => consumableList = value; }
        public Transform Character { get => character; }
        public Transform HolderHand { get => consumableHolderHand; }
        protected Transform WeaponHolder { get => weaponHolder; }
        public UnityEvent OnConsumableAmountChangedEvent { get => onConsumableAmountChangedEvent; }
        protected GameObject InstantiatedConsumable { get => instantiatedConsumable; set => instantiatedConsumable = value; }

        protected virtual void Start()
        {
            character = transform.root;
            consumableHolderHand = Utils.FindGameObjectInTransformWithTag(character, Utils.POTION_HOLDER_TAG).transform;
            weaponHolder = Utils.FindGameObjectInTransformWithTag(character, Utils.WEAPON_HOLDER_TAG).transform;
        }

        protected virtual void Update()
        {
            HandleConsume();
            HandleConsumableRemoval();
        }

        protected abstract void HandleConsume();

        protected abstract void IstantiateConsumable(Consumable consumable);

        protected abstract void OnConsumableUsed();

        protected abstract void OnDestroyConsumable();

        public void AddConsumables(Consumable[] consumables)
        {
            foreach (Consumable consumable in consumables)
            {
                AddConsumable(consumable);
            }
        }

        private void AddConsumable(Consumable consumable)
        {
            Consumable consumableInList = consumableList.Find(consumableElement => consumableElement.Equals(consumable));

            if (consumableInList != null)
            {
                int previousAmount = consumableInList.Amount;
                
                // Merge consumables (sum the amount of one to the other)
                consumableInList.Merge(consumable);

                // If the amount of consumables has changed, invoke the event
                if (consumableInList.Amount != previousAmount)
                {
                    onConsumableAmountChangedEvent.Invoke();
                }
            }
            else
            {
                // Add consumable to list
                consumableList.Add(consumable);
                onConsumableAmountChangedEvent.Invoke();
            }
        }
        
        public void HandleConsumableRemoval()
        {
            consumableList.RemoveAll(consumable => consumable.Amount <= 0);
        }

        protected void ChangeWeaponMeshesVisibility(bool isVisible)
        {
            foreach (MeshRenderer mesh in WeaponHolder.GetComponentsInChildren<MeshRenderer>())
            {
                mesh.enabled = isVisible;
            }
        }
    }
}