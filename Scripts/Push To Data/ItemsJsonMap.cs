// Author: Pietro Vitagliano

using System.Collections.Generic;
using UnityEngine;
using System;

namespace MysticAxe
{
    [Serializable]
    public class ItemsJsonMap
    {
        [SerializeField] private List<Item> items;

        public List<Item> Items { get => items; }

        public Item GetItemByName(string name)
        {
            foreach (Item item in items)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }

            return null;
        }

        [Serializable]
        public class Item
        {
            [SerializeField] private string name;
            [SerializeField] private string[] key_words;
            [SerializeField] private float effect;
            [SerializeField] private float effect_percentage_increase_per_level;

            public string Name { get => name; }
            public string[] KeyWords { get => key_words; }
            public float Effect { get => effect; }
            public float EffectPercentageIncreasePerLevel { get => effect_percentage_increase_per_level; }
        }
    }
}