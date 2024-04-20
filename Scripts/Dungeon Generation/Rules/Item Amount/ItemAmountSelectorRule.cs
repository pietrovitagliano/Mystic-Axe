// Author: Pietro Vitagliano

using DungeonArchitect;
using UnityEngine;
using static MysticAxe.DungeonObjectAmountHandler;

namespace MysticAxe
{
    public abstract class ItemAmountSelectorRule : SelectorRule
    {
        private DungeonObjectAmountInfo dungeonObjectAmountInfo;
        private string keyName;
        
        protected DungeonObjectAmountInfo DungeonObjectAmountInfo { get => dungeonObjectAmountInfo; }
        protected string KeyName { get => keyName; set => keyName = value; }

        protected abstract void InitializeKeyName();

        public override bool CanSelect(PropSocket socket, Matrix4x4 propTransform, DungeonModel model, System.Random random)
        {
            InitializeKeyName();

            if (dungeonObjectAmountInfo == null) 
            {
                DungeonObjectAmountHandler dungeonObjectAmountHandler = model.gameObject.GetComponent<DungeonObjectAmountHandler>();
                dungeonObjectAmountInfo = dungeonObjectAmountHandler.DungeonObjectAmountInfoList.Find(maxItemAmountInfo => maxItemAmountInfo.KeyName == keyName);
            }

            bool isRoomCell = Utils.IsGridRoomCell(model, socket);
            float spawnProbability = isRoomCell ? dungeonObjectAmountInfo.SpawnInRoomProbability : dungeonObjectAmountInfo.SpawnInCorridorProbability;
            bool isRandomlyChoosen = random.value() <= spawnProbability;
            
            if (dungeonObjectAmountInfo != null && isRandomlyChoosen)
            {
                int totalItemSpawned = dungeonObjectAmountInfo.ItemsSpawnedInRoom + dungeonObjectAmountInfo.ItemsSpawnedInCorridors;
                if (isRoomCell)
                {
                    if (dungeonObjectAmountInfo.ItemsSpawnedInRoom < dungeonObjectAmountInfo.MaxItemInRooms && totalItemSpawned < dungeonObjectAmountInfo.MaxItemInDungeon)
                    {
                        dungeonObjectAmountInfo.ItemsSpawnedInRoom++;

                        return true;
                    }
                }
                else
                {
                    if (dungeonObjectAmountInfo.ItemsSpawnedInCorridors < dungeonObjectAmountInfo.MaxItemInCorridors && totalItemSpawned < dungeonObjectAmountInfo.MaxItemInDungeon)
                    {
                        dungeonObjectAmountInfo.ItemsSpawnedInCorridors++;

                        return true;
                    }
                }
            }

            return false;
        }
    }
}