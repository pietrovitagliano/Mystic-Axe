// Author: Pietro Vitagliano

using System.Linq;
using UnityEngine;

namespace MysticAxe
{
    public abstract class CharacterUIStat : MonoBehaviour
    {
        protected GameObject character = null;
        

        protected virtual void Start()
        {
            GameObject canvasPlayerStats = GameObject.FindGameObjectWithTag(Utils.PLAYER_STATS_TAG);

            // This script is attatched to a gameobject, child of player UI stats canvas
            if (canvasPlayerStats.GetComponentsInChildren<Transform>().Contains(transform))
            {
                character = GameObject.FindGameObjectWithTag(Utils.PLAYER_TAG);
            }
            else
            {
                GameObject canvasBossStats = GameObject.FindGameObjectWithTag(Utils.BOSS_ENEMY_STATS_TAG);

                // This script is attatched to a gameobject, child of boss enemy UI stats canvas
                if (canvasBossStats != null && canvasBossStats.GetComponentsInChildren<Transform>().Contains(transform))
                {
                    character = GameObject.FindGameObjectWithTag(Utils.BOSS_TAG);

                }
                // This script is attatched to a gameobject, child of a base enemy UI stats canvas
                else
                {
                    character = transform.root.gameObject;
                }
            }

            InitializeUI();
        }

        protected abstract void InitializeUI();
    }
}