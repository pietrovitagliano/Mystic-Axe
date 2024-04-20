// Author: Pietro Vitagliano

using UnityEngine;

namespace MysticAxe
{
    public class OcclusionCullingController : MonoBehaviour
    {
        [SerializeField] private float collisionRadius = 0.45f;
        private LayerMask wallLayerMask;
        private Camera camera;

        private void Start()
        {
            wallLayerMask = LayerMask.GetMask(Utils.GROUND_LAYER_NAME, Utils.OBSTACLES_LAYER_NAME);

            camera = Camera.main;
            camera.useOcclusionCulling = true;
        }

        void Update()
        {
            Collider[] colliders = Physics.OverlapSphere(camera.transform.position, collisionRadius, wallLayerMask);
            camera.useOcclusionCulling = colliders.Length == 0;
        }
    }
}