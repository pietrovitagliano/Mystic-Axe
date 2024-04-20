// Author: Pietro Vitagliano

using System;
using System.Collections.Generic;
using UnityEngine;


namespace MysticAxe
{
    public class ConsumableDatabase : Singleton<ConsumableDatabase>
    {
        [Serializable]
        public class ConsumableData
        {
            [SerializeField] private GameObject consumablePrefab;
            [SerializeField] private int consumableLevel;
            [SerializeField] private Sprite consumableIcon;

            public GameObject ConsumablePrefab { get => consumablePrefab; }
            public int ConsumableLevel { get => consumableLevel; }
            public Sprite ConsumableIcon { get => consumableIcon; }
            public string ConsumableName { get => consumablePrefab.name; }
        }

        [SerializeField] private List<ConsumableData> consumableList;

        public List<ConsumableData> ConsumableList { get => consumableList; }
    }
}