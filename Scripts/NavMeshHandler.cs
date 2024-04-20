// Author: Pietro Vitagliano

using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace MysticAxe
{
    [RequireComponent(typeof(NavMeshSurface))]
    public class NavMeshHandler : MonoBehaviour
    {
        private NavMeshSurface navMeshSurface;

        // It's necessary to use the Awake() method rather than the Start(),
        // since DungeonGenerationHandler generate the dungeon in the awake method
        // and it requires NavMeshHandler for the NavMesh bake
        protected void Awake()
        {
            navMeshSurface = GetComponent<NavMeshSurface>();

            // The geometry must be the one with PhysicsColliders, otherwise unity will throw some errors,
            // that could be fixed disabling the option "static batching", but that will increase the draw calls,
            // that means worse performance.
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        }
        
        public void NavMeshBake()
        {
            // Remove any existing NavMesh data
            navMeshSurface.RemoveData();

            // Mark all the gameObjects that belong to the Obstacles layer, as Not Walkable
            ModifyNavMeshArea(layerName: Utils.OBSTACLES_LAYER_NAME, newAreaName: Utils.NAVMESH_NOT_WALKABLE_AREA_NAME);

            // Create new NavMesh data performing the bake
            navMeshSurface.BuildNavMesh();
        }

        // Change the navmesh area of all the gameObjects in the layer with the given name
        public void ModifyNavMeshArea(string layerName, string newAreaName)
        {
            int layer = LayerMask.NameToLayer(layerName);

            foreach (GameObject obstacleGameObject in FindObjectsOfType<GameObject>(includeInactive: true)
                                                        .Where(gameObject => gameObject.layer == layer))
            {
                if (!obstacleGameObject.TryGetComponent(out NavMeshModifier navMeshModifier))
                {
                    navMeshModifier = obstacleGameObject.AddComponent<NavMeshModifier>();
                }

                navMeshModifier.overrideArea = true;
                navMeshModifier.area = NavMesh.GetAreaFromName(newAreaName);
            }
        }
    }
}