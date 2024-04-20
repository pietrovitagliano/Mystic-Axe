// Author: Pietro Vitagliano

using System.Linq;
using UnityEngine;

public sealed class PermanentUniqueObject : MonoBehaviour
{
    private void Awake()
    {
        int cloneNumber = FindObjectsOfType<GameObject>(includeInactive: true)
                                .Where(gameObjectFound => gameObjectFound == gameObject || gameObjectFound.CompareTag(gameObject.tag))
                                .Count();
        
        if (cloneNumber > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
