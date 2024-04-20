// Author: Pietro Vitagliano

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TextAsset = UnityEngine.TextAsset;

namespace MysticAxe
{
    public class JsonDatabase : Singleton<JsonDatabase>
    {
        [SerializeField] private List<TextAsset> jsonList;
        private Dictionary<string, TextAsset> jsonDict;

        protected override void Awake()
        {
            base.Awake();

            // Convert the list into a dictionary
            jsonDict = jsonList.ToDictionary(textAsset => textAsset.name, textAsset => textAsset);

            // Deallocate the list
            jsonList.Clear();
            jsonList = null;
        }

        public T GetDataFromJson<T>(string jsonName)
        {
            // Remove extension from json name, since the text asset name has no extension
            jsonName = jsonName.Replace(Path.GetExtension(jsonName), "");

            if (jsonDict.TryGetValue(jsonName, out TextAsset jsonFile))
            {
                return JsonUtility.FromJson<T>(jsonFile.text);
            }
            else
            {
                throw new IOException($"JSON file {jsonName} not found");
            }
        }
    }
}