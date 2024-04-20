// Author: Pietro Vitagliano

using DungeonArchitect;
using DungeonArchitect.Builders.Grid;
using DungeonArchitect.Utils;
using UnityEngine;

namespace MysticAxe
{
    public abstract class DungeonConstrainedPlaceableSelectorRule : ItemAmountSelectorRule
    {
        // These index identify a submatrix on which the cells has to be unkown,
        // in order to have enough space to place the exit.
        // They are initialized in the InitializeIndexes method
        private int emptyRowsMinIndex;
        private int emptyRowsMaxIndex;
        private int emptyColumnsMinIndex;
        private int emptyColumnsMaxIndex;

        protected int EmptyRowsMinIndex { get => emptyRowsMinIndex; set => emptyRowsMinIndex = value; }
        protected int EmptyRowsMaxIndex { get => emptyRowsMaxIndex; set => emptyRowsMaxIndex = value; }
        protected int EmptyColumnsMinIndex { get => emptyColumnsMinIndex; set => emptyColumnsMinIndex = value; }
        protected int EmptyColumnsMaxIndex { get => emptyColumnsMaxIndex; set => emptyColumnsMaxIndex = value; }

        protected abstract void InitializeIndexes();

        public override bool CanSelect(PropSocket socket, Matrix4x4 propTransform, DungeonModel model, System.Random random)
        {
            // Check if there is space for the dungeon exit
            if (model is GridDungeonModel)
            {
                InitializeIndexes();
                
                // Get the dungeon model
                GridDungeonModel gridModel = model as GridDungeonModel;

                // Get the grid size
                Vector3 gridSize = gridModel.Config.GridCellSize;

                // Calculate the world position of the cell
                Vector3 position = Matrix.GetTranslation(ref propTransform);

                // Calculate the coordinates of the cell on the grid
                int x = Mathf.FloorToInt(position.x / gridSize.x);
                int z = Mathf.FloorToInt(position.z / gridSize.z);

                // Get info for the cell at coordinates x and z
                GridCellInfo cellInfo = gridModel.GetGridCellLookup(x, z);

                // The exit must be in the void
                if (cellInfo.CellType == CellType.Unknown)
                {
                    // Get the rotation of the cell
                    Quaternion rotation = Matrix.GetRotation(ref propTransform);
                    for (int i = emptyRowsMinIndex; i <= emptyRowsMaxIndex; i++)
                    {
                        for (int j = emptyColumnsMinIndex; j <= emptyColumnsMaxIndex; j++)
                        {
                            float offsetX = j * gridSize.x;
                            float offsetZ = i * gridSize.z;

                            // Apply the rotation only if y angle is not 0
                            if (rotation.eulerAngles.y != 0)
                            {
                                float angleInRad = rotation.eulerAngles.y * Mathf.Deg2Rad;
                                float rotatedOffsetX = offsetX * Mathf.Cos(angleInRad) - offsetZ * Mathf.Sin(angleInRad);
                                float rotatedOffsetZ = offsetX * Mathf.Sin(angleInRad) + offsetZ * Mathf.Cos(angleInRad);
                                offsetX = rotatedOffsetX;
                                offsetZ = rotatedOffsetZ;
                            }

                            Vector3 offset = new Vector3(offsetX, 0, offsetZ);
                            Vector3 cellPosition = position + offset;
                            Debug.DrawRay(cellPosition, Vector3.up * 10, Color.red, 15);

                            IntVector gridPosition = MathUtils.WorldToGrid(cellPosition, gridModel.Config.GridCellSize);
                            cellInfo = gridModel.GetGridCellLookup(gridPosition.x, gridPosition.z);

                            // If the number of the next empty cell (CellType.Unknown)
                            // is not enough, return false
                            if (cellInfo.CellType != CellType.Unknown)
                            {
                                return false;
                            }
                        }
                    }

                    Debug.DrawRay(position, 10 * Vector3.up, Color.blue, 100f);

                    // If the number of the next empty cell (CellType.Unknown) is enough
                    // (due to the fact that the for cycle has not been interrupted by "return false"),
                    // proceed with the base implementation of the CanSelect method
                    return base.CanSelect(socket, propTransform, model, random);
                }
            }

            return false;
        }
    }
}
