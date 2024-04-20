// Author: Pietro Vitagliano

using DungeonArchitect;
using DungeonArchitect.Builders.Grid;
using UnityEngine;

namespace MysticAxe
{
    public class RandomizeTransformTransformationRule : TransformationRule
    {
        public override void GetTransform(PropSocket socket, DungeonModel model, Matrix4x4 propTransform, System.Random random, out Vector3 outPosition, out Quaternion outRotation, out Vector3 outScale)
        {
            base.GetTransform(socket, model, propTransform, random, out outPosition, out outRotation, out outScale);
            
            // Randomize position if the dungeon model is a grid model
            if (model is GridDungeonModel gridModel)
            {
                // Get the size of the cell
                Vector3 cellSize = gridModel.Config.GridCellSize;

                // Add a random offset to the position
                // The half bound multiplier is 0.4 rather than 0.5,
                // in order to avoid moving the socket to the edge of the cell
                float halfBoundMultiplier = 0.4f;
                outPosition.x += random.Range(-halfBoundMultiplier * cellSize.x, halfBoundMultiplier * cellSize.x);
                outPosition.z += random.Range(-halfBoundMultiplier * cellSize.z, halfBoundMultiplier * cellSize.z);
            }

            // Randomize rotation along Y Axis
            Quaternion rotation = Quaternion.Euler(0, random.value() * 360, 0);
            outRotation *= rotation;

            // Randomize scale
            float scale = random.Range(0.8f, 1.2f);
            outScale *= scale;
        }
    }
}