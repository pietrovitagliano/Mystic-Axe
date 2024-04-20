// Author: Pietro Vitagliano

using DungeonArchitect;
using UnityEngine;

/// <summary>
/// Select even elements on the grid with a 50% chance
/// </summary>
public class AlternateWith50ChanceSelectorRule : SelectorRule
{
    public override bool CanSelect(PropSocket socket, Matrix4x4 propTransform, DungeonModel model, System.Random random)
    {
        bool isEven = (socket.gridPosition.x + socket.gridPosition.z) % 2 == 0;

        return isEven && random.Next(0, 2) == 0;
    }
}
