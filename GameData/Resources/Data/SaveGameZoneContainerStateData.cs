namespace GameData.Resources.Data;

using System;

public class SaveGameZoneContainerStateData {
    public SaveGameZoneContainerStateData(SaveGameZoneContainerEntryData[] zones) {
        Zones = zones ?? Array.Empty<SaveGameZoneContainerEntryData>();
    }

    public SaveGameZoneContainerEntryData[] Zones { get; }
}
