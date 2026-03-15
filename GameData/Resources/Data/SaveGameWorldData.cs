namespace GameData.Resources.Data;

using System;
using System.Collections.Generic;

public class SaveGameWorldData {
    public SaveGameWorldData(
        SaveGameExplorationStateEntryData[] explorationStateEntries,
        SaveGameZoneDataData[] zoneDataEntries,
        int[] chapterGoldAmounts,
        Dictionary<int, List<short>> enemyPartyActorSlots,
        Dictionary<int, int> combatEncounterTimestamps,
        Dictionary<int, int> enemyTrapRespawnTimestamps,
        SaveGameWorldEncounterItemData[][] worldEncounterItemData
    ) {
        ExplorationStateEntries = explorationStateEntries;
        ZoneDataEntries = zoneDataEntries ?? Array.Empty<SaveGameZoneDataData>();
        ChapterGoldAmounts = chapterGoldAmounts ?? Array.Empty<int>();
        EnemyPartyActorSlots = enemyPartyActorSlots ?? new Dictionary<int, List<short>>();
        CombatEncounterTimestamps = combatEncounterTimestamps ?? new Dictionary<int, int>();
        EnemyTrapRespawnTimestamps = enemyTrapRespawnTimestamps ?? new Dictionary<int, int>();
        WorldEncounterItemData = worldEncounterItemData ?? Array.Empty<SaveGameWorldEncounterItemData[]>();
    }

    public SaveGameExplorationStateEntryData[] ExplorationStateEntries { get; }
    public SaveGameZoneDataData[] ZoneDataEntries { get; }
    public int[] ChapterGoldAmounts { get; }
    public Dictionary<int, List<short>> EnemyPartyActorSlots { get; }
    public Dictionary<int, int> CombatEncounterTimestamps { get; }
    public Dictionary<int, int> EnemyTrapRespawnTimestamps { get; }
    public SaveGameWorldEncounterItemData[][] WorldEncounterItemData { get; }
}
