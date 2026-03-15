namespace GameData.Resources.Data;

using System;

public class SaveGameExplorationStateData {
    public SaveGameExplorationStateData(
        SaveGameExplorationStateEntryData[] entries
    ) {
        Entries = entries ?? Array.Empty<SaveGameExplorationStateEntryData>();
    }

    public SaveGameExplorationStateEntryData[] Entries { get; }
}
