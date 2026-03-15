namespace GameData.Resources.Data;

using System;

public class SaveGameZoneContainerEntryData {
    public SaveGameZoneContainerEntryData(
        short zoneNumber,
        int tempGamFileOffset,
        SaveGameContainerData[] containers
    ) {
        ZoneNumber = zoneNumber;
        TempGamFileOffset = tempGamFileOffset;
        Containers = containers ?? Array.Empty<SaveGameContainerData>();
    }

    public short ZoneNumber { get; }
    public int TempGamFileOffset { get; }
    public SaveGameContainerData[] Containers { get; }
}
