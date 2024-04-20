// Author: Pietro Vitagliano

namespace MysticAxe
{
    public class DungeonExitSelectorRule : DungeonConstrainedPlaceableSelectorRule
    {
        protected override void InitializeKeyName()
        {
            KeyName = "Dungeon Exit";
        }

        protected override void InitializeIndexes()
        {
            EmptyRowsMinIndex = 0;
            EmptyRowsMaxIndex = 3;
            EmptyColumnsMinIndex = -2;
            EmptyColumnsMaxIndex = 1;
        }
    }
}