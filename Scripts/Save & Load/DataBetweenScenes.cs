// Author: Pietro Vitagliano

using System.Collections.Generic;

namespace MysticAxe
{
    public class DataBetweenScenes : Singleton<DataBetweenScenes>
    {
        // This is the data that will be passed between scenes
        // It is a dictionary of string and object and is similar to PlayerPrefs,
        // but it is not saved to the disk and it works with objects too
        private readonly Dictionary<string, object> data = new Dictionary<string, object>();

        #region Data keys
        public const string PLAYER_CURRENT_HEALTH_KEY = "player_current_health";
        public const string PLAYER_CURRENT_MANA_KEY = "player_current_mana";
        public const string PLAYER_CURRENT_LEVEL_KEY = "player_current_level";
        public const string PLAYER_CURRENT_EXP_KEY = "player_current_exp";
        public const string PLAYER_CONSUMABLE_LIST_KEY = "player_consumable_list";
        public const string PLAYER_CONSUMABLE_INDEX_KEY = "consumable_index";
        public const string PLAYER_GOLD_KEY = "player_gold";
        public const string DUNGEON_CURRENT_LEVEL_KEY = "dungeon_current_level";
        #endregion

        public object GetData(string key, object defaultValue)
        {
            return data.ContainsKey(key) ? data[key] : defaultValue;
        }

        public void StoreData(string key, object value)
        {
            if (data.ContainsKey(key))
            {
                data[key] = value;
            }
            else
            {
                data.Add(key, value);
            }
        }

        public void RemoveData(string key)
        {
            if (data.ContainsKey(key))
            {
                data.Remove(key);
            }
        }

        public void ClearData()
        {
            data.Clear();
        }
    }
}