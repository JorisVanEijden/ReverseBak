namespace GameData.Resources.Data;

public class SaveGameContainerEncounterData {
    public SaveGameContainerEncounterData(
        int globalDataKey1,
        int globalDataKey2,
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

    /// <summary>
    /// The global whose value gates this encounter — a key for <c>GetGlobalValue</c> @0x42250.
    /// </summary>
    /// <remarks>
    /// <b>Unsigned, and it must be: the key space runs past 32767.</b> <c>GetGlobalValue</c>
    /// compares the key with <c>jb</c>/<c>jnb</c> — unsigned branches — and its top band is the
    /// 56000+ range backed by <c>global_flags2[]</c>. Reading the field as a signed 16-bit turns
    /// key 56012 into -9524, which no band then matches.
    ///
    /// <para>This is not hypothetical: the shipped OBJFIXED.DAT has one such record and the save
    /// games have more (56012 and 56315 both appear). Held as <c>int</c> rather than
    /// <c>ushort</c> so the value reads as the number the engine uses.</para>
    /// </remarks>
    public int GlobalDataKey1 { get; }

    /// <inheritdoc cref="GlobalDataKey1"/>
    public int GlobalDataKey2 { get; }
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
