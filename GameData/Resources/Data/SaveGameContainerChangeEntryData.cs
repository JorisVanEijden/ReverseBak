namespace GameData.Resources.Data;

using System;

public class SaveGameExplorationStateEntryData {
    public SaveGameExplorationStateEntryData(
        byte zoneNumber,
        byte xCoordinate,
        byte yCoordinate,
        byte[] exploredTileBitfield
    ) {
        ZoneNumber = zoneNumber;
        XCoordinate = xCoordinate;
        YCoordinate = yCoordinate;
        ExploredTileBitfield = exploredTileBitfield ?? Array.Empty<byte>();
    }

    public byte ZoneNumber { get; }
    public byte XCoordinate { get; }
    public byte YCoordinate { get; }
    public byte[] ExploredTileBitfield { get; }
}
