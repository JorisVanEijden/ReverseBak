namespace ResourceExtraction;

/// <summary>
/// Byte layout of a TEMP.GAM / SAVE##.GAM body and its 100-byte slot header, shared by the reader
/// (<see cref="Extractors.SaveGameExtractor"/>) and the writer (<see cref="SaveGameWriter"/>) so the
/// two can never drift. Offsets are into the body (little-endian); StateData starts at body offset 0.
/// Ground truth: SaveGameFromTempGam @0x41c00 (header) + ParseStateData (StateData field order).
/// </summary>
public static class SaveGameOffsets {
    // Body blocks.
    public const int StateDataSize = 2775;
    public const int WorldDataSize = 34320;
    public const int ActorDataSize = 164350;
    public const int CombatDataSize = 38060;
    public const int ZoneContainerDataSize = 95000;
    public const int BodySize =
        StateDataSize + WorldDataSize + ActorDataSize + CombatDataSize + ZoneContainerDataSize; // 334505

    // StateData scalar fields (byte offset within the body).
    public const int Chapter = 0;      // Int16
    public const int PartyGold = 2;    // Int32
    public const int GameTime = 6;     // Int32 (gameTimeIn2Seconds)
    public const int CurrentZone = 18; // byte
    public const int WorldX = 19;      // byte
    public const int WorldY = 20;      // byte
    public const int PositionX = 21;   // Int32
    public const int PositionY = 25;   // Int32
    public const int PositionZ = 29;   // Int32
    public const int Rotation = 33;    // Int16 (currentZRotation)

    // 100-byte slot header field offsets (within the header, which precedes the body on disk).
    public const int HeaderName = 0;
    public const int HeaderNameLength = 90;
    public const int HeaderChapter = 90;   // Int16
    public const int HeaderWorldX = 92;    // Int16 (FULLMAP pixel)
    public const int HeaderWorldY = 94;    // Int16
    public const int HeaderMapIcon = 96;   // Int16
    public const int HeaderVersion = 98;   // Int16
    public const int HeaderSize = 100;
}
