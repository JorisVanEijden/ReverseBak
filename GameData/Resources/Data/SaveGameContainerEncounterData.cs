namespace GameData.Resources.Data;

public class SaveGameContainerEncounterData {
    public SaveGameContainerEncounterData(
        short globalDataKey1,
        short globalDataKey2,
        byte gdsNumber,
        byte gdsLetter,
        byte firesTrapEncounter,
        byte x,
        byte y
    ) {
        GlobalDataKey1 = globalDataKey1;
        GlobalDataKey2 = globalDataKey2;
        GdsNumber = gdsNumber;
        GdsLetter = gdsLetter;
        FiresTrapEncounter = firesTrapEncounter;
        X = x;
        Y = y;
    }

    public short GlobalDataKey1 { get; }
    public short GlobalDataKey2 { get; }
    public byte GdsNumber { get; }
    public byte GdsLetter { get; }

    // 0x06. When nonzero, this location has a positioned trap/ambush: handle_Building (0x76b39),
    // handle_Tunnel, handle_Grave require the player on the exact tile, then fire the tile's
    // DEF_TRAP encounter at (X, Y) via sub_stub187_34(def_trap_dat, X, Y) before the GDS/dialog
    // flow. 0 = plain GDS-scene/dialog location. (IDA: containerData_encounter.firesTrapEncounter.)
    public byte FiresTrapEncounter { get; }
    public byte X { get; }
    public byte Y { get; }

    public bool IsFiresTrapEncounterSet {
        get => FiresTrapEncounter != 0;
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
