namespace GameData.Resources.Data;

public class SaveGameContainerEncounterData {
    public SaveGameContainerEncounterData(
        short globalDataKey1,
        short globalDataKey2,
        byte gdsNumber,
        byte gdsLetter,
        byte field6,
        byte x,
        byte y
    ) {
        GlobalDataKey1 = globalDataKey1;
        GlobalDataKey2 = globalDataKey2;
        GdsNumber = gdsNumber;
        GdsLetter = gdsLetter;
        Field6 = field6;
        X = x;
        Y = y;
    }

    public short GlobalDataKey1 { get; }
    public short GlobalDataKey2 { get; }
    public byte GdsNumber { get; }
    public byte GdsLetter { get; }
    public byte Field6 { get; }
    public byte X { get; }
    public byte Y { get; }

    public bool IsField6Set {
        get => Field6 != 0;
    }

    public string? GdsFilename {
        get {
            if (GdsNumber == 0 || GdsLetter == 0) {
                return null;
            }

            char letter = (char)('A' + GdsLetter - 1);
            return $"GDS{GdsNumber}{letter}.DAT";
        }
    }
}
