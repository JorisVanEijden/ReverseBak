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

    /// <summary>
    /// Int32. When the party last rested (<c>dwLastActionTimeSnapshot</c>) — exhaustion is measured
    /// from here, so leaving it unwritten loses how tired everyone is.
    /// </summary>
    public const int TimeSnapshot = 10;

    // The pending-timer pool. Like the party records, the reader reaches these sequentially, so the
    // constants below are pinned by round-trip tests rather than trusted.
    public const int TimerPoolCount = 1458;   // Int16, how many of the 20 slots are live
    public const int TimerPool = 1460;        // 20 slots of {type u8, mode u8, key i16, time i32}

    /// <summary>
    /// Int16 — <c>wPalEventMask</c>, which overworld spell palette effects are running.
    /// </summary>
    /// <remarks>
    /// Immediately after the 160-byte timer pool, per <c>gstate.inc</c>: <c>nTimerEventPoolCount</c>
    /// (1458), <c>aTimerEventPool</c> (1460, 160 bytes), then this. Corroborated by its neighbours
    /// in the shipped saves — 1622/1624 are <c>nSpellMenuCasterSlot</c>/<c>nSpellMenuPreselect</c>,
    /// which read a real party slot and spell in the played saves and exactly the -1/-1 sentinel
    /// <c>SAVEGAME.C</c> writes on a new game.
    ///
    /// <para>The parser already reads this word as
    /// <c>SaveGameLightingStateData.ActiveSpellTimerFlags</c>; this constant is for the writer.</para>
    /// </remarks>
    public const int PaletteEventMask = 1620;
    public const int TimerStride = 8;
    public const int TimerSlots = 20;

    // The six party-member records and their affliction ranks, both inside StateData. The reader
    // reaches these sequentially rather than by offset, so SaveGameOffsetsTests pins every constant
    // below by writing at it and reading the value back out through SaveGameExtractor — if the
    // parse order ever shifts, those tests fail rather than the writer quietly corrupting a save.
    public const int PartyActorCount = 6;

    /// <summary>First party actor record; 95 bytes each, in character-id order.</summary>
    public const int PartyActors = 119;
    public const int PartyActorStride = 95;

    /// <summary>The three known-spell words within an actor record, right after the name pointer.
    /// Confirmed by <c>combat_actor_bitmap_set_bit</c>, which indexes them as
    /// <c>record + 2 + (spellId / 16) * 2</c>.</summary>
    public const int ActorKnownSpellsInRecord = 2;
    public const int ActorKnownSpellWords = 3;

    /// <summary>Attribute quintuples {Maximum, Current, CurrentEffective, Experience, Modifier}
    /// start here within an actor record, one per attribute in ActorAttribute order.</summary>
    public const int ActorAttributesInRecord = 8;
    public const int ActorAttributeStride = 5;
    public const int ActorAttributeCount = 16;

    /// <summary>First actor's seven affliction ranks; 7 bytes each, in character-id order.</summary>
    public const int ActorStatusEffects = 709;
    public const int ActorStatusEffectsStride = 7;
    public const int ActorStatusEffectCount = 7;

    // 100-byte slot header field offsets (within the header, which precedes the body on disk).
    // Name length + total size are the reader's (SaveGameHeader) — one source of truth.
    public const int HeaderName = 0;
    public const int HeaderNameLength = GameData.Resources.Data.SaveGameHeader.NameLength; // 90
    public const int HeaderChapter = 90;   // Int16
    public const int HeaderWorldX = 92;    // Int16 (FULLMAP pixel)
    public const int HeaderWorldY = 94;    // Int16
    public const int HeaderMapIcon = 96;   // Int16
    public const int HeaderVersion = 98;   // Int16
    public const int HeaderSize = GameData.Resources.Data.SaveGameHeader.Size;            // 100
}
