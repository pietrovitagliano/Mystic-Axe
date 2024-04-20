// Author: Pietro Vitagliano

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class EssentialInstantiator
{
    [SerializeField] private GameObject[] essentialPrefabs;
    
    public List<GameObject> InstantiateEssentials()
    {
        List<GameObject> instantiatedEssentials = new List<GameObject>();

        foreach (GameObject essentialPrefab in essentialPrefabs)
        {
            if (GameObject.FindGameObjectWithTag(essentialPrefab.tag) == null)
            {
                GameObject instantiatedEssential = Object.Instantiate(essentialPrefab);
                instantiatedEssentials.Add(instantiatedEssential);
            }
        }

        return instantiatedEssentials;
    }
}
