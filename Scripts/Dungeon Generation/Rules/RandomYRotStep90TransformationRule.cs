// Author: Pietro Vitagliano

using DungeonArchitect;
using UnityEngine;

namespace MysticAxe
{
    public class RandomYRotStep90TransformationRule : TransformationRule
    {
        public override void GetTransform(PropSocket socket, DungeonModel model, Matrix4x4 propTransform, System.Random random, out Vector3 outPosition, out Quaternion outRotation, out Vector3 outScale)
        {
            base.GetTransform(socket, model, propTransform, random, out outPosition, out outRotation, out outScale);
            
            float angle = random.Range(0, 4) * 90;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            outRotation *= rotation;
        }
    }
}