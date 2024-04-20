// Author: Pietro Vitagliano

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static MysticAxe.ConsumableDatabase;

namespace MysticAxe
{
    public abstract class Consumable : IEqualityComparer<Consumable>
    {
        [Range(0, maxConsumables)] private int amount;
        [Range(1, 999)] protected const int maxConsumables = 20;
        [Range(1, 3)] private int level;
        protected float effect;
        protected float effectPercentageIncreasePerLevel;
        
        protected string[] keyWordsToFindPrefab;
        protected ItemsJsonMap itemsJsonMap;
        
        private GameObject prefab;
        private Sprite icon;

        public int Amount { get => amount; set => amount = value; }
        public int Level { get => level; set => level = value; }
        public GameObject Prefab { get => prefab; protected set => prefab = value; }
        public Sprite Icon { get => icon; set => icon = value; }
        

        public void Merge(Consumable consumable)
        {
            if (Equals(consumable))
            {
                amount = Mathf.Clamp(amount + consumable.amount, 0, maxConsumables);
            }
        }

        public void InitializeConsumable(int amount, int level)
        {
            this.amount = amount;
            this.level = level;

            itemsJsonMap = JsonDatabase.Instance.GetDataFromJson<ItemsJsonMap>(Utils.ITEMS_JSON_NAME);

            InitializeDataFromJson();
            InitializeIconAndPrefab();
        }

        protected abstract void InitializeDataFromJson();

        public abstract void Consume(Transform character, GameObject instancedConsumable);
        
        private void InitializeIconAndPrefab()
        {
            // Look for a consumable data with all the key words in its name and with the same level of the current one 
            ConsumableData consumableData = ConsumableDatabase.Instance.ConsumableList.Find(consumableData => level == consumableData.ConsumableLevel && 
                                                                                                                keyWordsToFindPrefab.All(keyword => consumableData.ConsumableName.ToLower().Contains(keyword.ToLower())));
            Prefab = consumableData.ConsumablePrefab;
            icon = consumableData.ConsumableIcon;
        }
        
        public static HashSet<Consumable> GetAllDefinedConsumableSet()
        {
            HashSet<Consumable> concreteConsumableSet = new HashSet<Consumable>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(Consumable)))
                    {
                        Consumable concreteInstance = (Consumable)Activator.CreateInstance(type);
                        concreteConsumableSet.Add(concreteInstance);
                    }
                }
            }

            return concreteConsumableSet;
        }

        public bool Equals(Consumable other)
        {
            return GetType() == other.GetType() && level == other.level;
        }

        // This method is necessary to implement IEqualityComparer<Consumable>
        public bool Equals(Consumable consumableA, Consumable consumableB)
        {
            return consumableA.Equals(consumableB);
        }

        // This method is necessary to implement IEqualityComparer<Consumable>
        public int GetHashCode(Consumable consumable)
        {
            // Compute hashcode based on level and name
            return consumable.level.GetHashCode() ^ consumable.GetType().GetHashCode();
        }
    }
}
