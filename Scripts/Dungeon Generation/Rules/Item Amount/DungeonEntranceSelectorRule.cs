// Author: Pietro Vitagliano

namespace MysticAxe
{
    public class DungeonEntranceSelectorRule : DungeonConstrainedPlaceableSelectorRule
    {
        protected override void InitializeKeyName()
        {
            KeyName = "Dungeon Entrance";
        }

        protected override void InitializeIndexes()
        {
            EmptyRowsMinIndex = 0;
            EmptyRowsMaxIndex = 2;
            EmptyColumnsMinIndex = -2;
            EmptyColumnsMaxIndex = 1;
        }
    }
}