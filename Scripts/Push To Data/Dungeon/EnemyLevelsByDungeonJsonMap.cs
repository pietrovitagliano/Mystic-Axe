// Author: Pietro Vitagliano

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MysticAxe
{
    [Serializable]
    public class EnemyLevelsByDungeonJsonMap
    {
        [SerializeField] private List<EnemyLevelsByDungeonInfo> enemy_levels_by_dungeon;

        public List<EnemyLevelsByDungeonInfo> Enemy_Levels_By_Dungeon { get => enemy_levels_by_dungeon; }

        [Serializable]
        public class EnemyLevelsByDungeonInfo
        {
            [SerializeField, Min(1)] private int min_level;

            [SerializeField, Min(1)] private int max_level;

            public int Min_Level { get => min_level; }
            public int Max_Level { get => max_level; }
        }
    }
}