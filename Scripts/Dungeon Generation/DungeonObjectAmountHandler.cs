// Author: Pietro Vitagliano

using DungeonArchitect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MysticAxe
{
    public class DungeonObjectAmountHandler : DungeonEventListener
    {
        [SerializeField] private List<DungeonObjectAmountInfo> dungeonObjectAmountInfoList;

        public List<DungeonObjectAmountInfo> DungeonObjectAmountInfoList { get => dungeonObjectAmountInfoList; }

        [Serializable]
        public class DungeonObjectAmountInfo
        {
            [SerializeField] private string keyName = "";
            [SerializeField, Min(0)] private int maxItemInDungeon = 0;
            [SerializeField, Min(0)] private int maxItemInRooms = 0;
            [SerializeField, Min(0)] private int maxItemInCorridors = 0;
            [SerializeField, Range(0, 1)] private float spawnInRoomProbability = 0.5f;
            [SerializeField, Range(0, 1)] private float spawnInCorridorProbability = 0.5f;
            private int itemsSpawnedInRoom = 0;
            private int itemsSpawnedInCorridors = 0;

            public string KeyName { get => keyName; }
            public int MaxItemInDungeon { get => maxItemInDungeon; }
            public int MaxItemInRooms { get => maxItemInRooms; }
            public int MaxItemInCorridors { get => maxItemInCorridors; }
            public float SpawnInRoomProbability { get => spawnInRoomProbability; }
            public float SpawnInCorridorProbability { get => spawnInCorridorProbability; }
            public int ItemsSpawnedInRoom { get => itemsSpawnedInRoom; set => itemsSpawnedInRoom = value; }
            public int ItemsSpawnedInCorridors { get => itemsSpawnedInCorridors; set => itemsSpawnedInCorridors = value; }
        }

        public override void OnDungeonMarkersEmitted(Dungeon dungeon, DungeonModel model, LevelMarkerList markers)
        {
            // Reset the amount for each MaxItemAmountInfo,
            // in order to make the next rebuild works correctly
            dungeonObjectAmountInfoList.ForEach(info => 
            {
                info.ItemsSpawnedInRoom = 0;
                info.ItemsSpawnedInCorridors = 0;
            });
        }
    }
}
