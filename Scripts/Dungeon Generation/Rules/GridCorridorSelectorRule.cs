// Author: Pietro Vitagliano

using DungeonArchitect;
using UnityEngine;

/// <summary>
/// Select a grid corridor
/// </summary>
namespace MysticAxe
{
    public class GridCorridorSelectorRule : SelectorRule
    {
        public override bool CanSelect(PropSocket socket, Matrix4x4 propTransform, DungeonModel model, System.Random random)
        {
            return Utils.IsGridCorridorCell(model, socket);
        }
    }
}
