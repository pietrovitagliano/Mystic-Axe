// Author: Pietro Vitagliano

using DungeonArchitect;
using UnityEngine;

/// <summary>
/// Select a grid room
/// </summary>
namespace MysticAxe
{
    public class GridRoomSelectorRule : SelectorRule
    {
        public override bool CanSelect(PropSocket socket, Matrix4x4 propTransform, DungeonModel model, System.Random random)
        {
            return Utils.IsGridRoomCell(model, socket);
        }
    }
}
