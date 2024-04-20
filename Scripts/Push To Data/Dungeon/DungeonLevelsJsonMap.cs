// Author: Pietro Vitagliano

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    [Serializable]
    public class DungeonLevelsJsonMap
    {
        [SerializeField] private List<DungeonLevelsInfo> dungeons;

        public List<DungeonLevelsInfo> Dungeons { get => dungeons; }

        /// <summary>
        /// Retrieves the DungeonLevelsInfo object that matches the given dungeon name.
        /// The condition for matching is based on keyword comparisons, where the keywords of
        /// each string (dungeon.Name and dungeonName) are extracted by splitting them
        /// using spaces, underscores and dashes as separators. If all the keywords from one string are
        /// present in the other string, or vice versa, the condition is considered satisfied.
        /// </summary>
        /// <param name="dungeonName">The name of the dungeon to search for.</param>
        /// <returns>The DungeonLevelsInfo object that matches the given dungeon name, or null if not found.</returns>
        public DungeonLevelsInfo GetDungeonByName(string dungeonName)
        {
            string[] dungeonNameParamKeywords = dungeonName.Split(new char[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

            return dungeons.Find(dungeon =>
            {
                string currentDungeonName = dungeon.Name;
                string[] currentDungeonNameKeywords = currentDungeonName.Split(new char[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

                return dungeonNameParamKeywords.All(keyword => currentDungeonName.Contains(keyword)) ||
                       currentDungeonNameKeywords.All(keyword => dungeonName.Contains(keyword));
            });
        }

        [Serializable]
        public class DungeonLevelsInfo
        {
            [SerializeField] private string name;
            [SerializeField, Min(1)] private int number_of_levels;

            public string Name { get => name; }
            public int Number_Of_Levels { get => number_of_levels; }
        }
    }
}