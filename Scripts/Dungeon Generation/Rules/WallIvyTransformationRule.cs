// Author: Pietro Vitagliano

using DungeonArchitect;
using DungeonArchitect.Builders.Grid;
using UnityEngine;

namespace MysticAxe
{
    public class WallIvyTransformationRule : TransformationRule
    {
        public override void GetTransform(PropSocket socket, DungeonModel model, Matrix4x4 propTransform, System.Random random, out Vector3 outPosition, out Quaternion outRotation, out Vector3 outScale)
        {
            base.GetTransform(socket, model, propTransform, random, out outPosition, out outRotation, out outScale);

            // Randomize position along X-Axis, if the dungeon model is a grid model
            if (model is GridDungeonModel gridModel)
            {
                // Get the size of the cell
                Vector3 cellSize = gridModel.Config.GridCellSize;

                outPosition.x += random.Range(-0.5f * cellSize.x, 0.5f * cellSize.x);
            }

            // Randomize rotation along Z-Axis
            Quaternion rotation = Quaternion.Euler(0, 0, random.value() * 360);
            outRotation *= rotation;

            // Randomize scale
            float scale = random.Range(0.8f, 1.2f);
            outScale *= scale;
        }
    }
}
