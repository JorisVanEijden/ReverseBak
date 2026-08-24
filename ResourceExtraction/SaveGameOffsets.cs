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

    /// <summary>Combat records — one per actor, so this is the actor table's count.</summary>
    public const int CombatSlotCount = CombatDataSize / 22;

    /// <summary>Where the combat block starts in a save BODY (not in the file — mind the header).</summary>
    public const int CombatDataOffset = StateDataSize + WorldDataSize + ActorDataSize;
    public const int ZoneContainerDataSize = 95000;
    public const int BodySize =
        StateDataSize + WorldDataSize + ActorDataSize + CombatDataSize + ZoneContainerDataSize; // 334505

    // StateData scalar fields (byte offset within the body).
    public const int Chapter = 0;      // Int16
    public const int PartyGold = 2;    // Int32
    public const int GameTime = 6;     // Int32 (gameTimeIn2Seconds)
    // ---- Section 0 head, cross-checked against canassa bak/INCLUDE/gstate.inc (2026-08-23) ----
    // The .inc gives the block as a flat ordered list, which turns a probed offset into its
    // neighbours. Every offset below that we already had matches it exactly. Two results worth
    // keeping:
    //
    //   21..28  canassa zoneDefaultCameraPos (8 bytes)  = our PositionX + PositionY. The player
    //           position IS the zone's camera position, which is why the names differ.
    //   29..32  canassa rsvd_1d (UNIDENTIFIED there)    = our PositionZ. We are ahead of canassa
    //           here; do not "correct" our model to match its reserved block.
    //   33      canassa wZoneDefaultCameraHeading       = our Rotation. Exact match.
    //
    // 14/15/17 are the three bytes the reader already models on SaveGameSection0. canassa calls
    // them bCombatExitRequest / nWorldLoopExitRequest / nPrevZoneId, but its first two names
    // describe the world loop's reaction rather than the state: 14 is set to 1 only when EVERY
    // active party member's condition[6] is set (STAT.C) and to 2 when the arena kills the last
    // of them (CACTOR.C), and 15 == 1 means the loop exits to the next chapter (GMAIN.C mode 5).
    // Our reader's names are the accurate ones, so they win.
    public const int PartyDeathState = 14;          // byte
    public const int ChapterTransitionPending = 15; // byte
    // 16 is canassa rsvd_10, a genuine reserved byte — leave it to passthrough.
    public const int PreviousZone = 17;             // byte — the zone the party came FROM

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
    /// <summary>How many characters are in the active party — <c>partySize</c>.</summary>
    /// <remarks>
    /// Offsets computed from <c>canassa/bak/INCLUDE/gstate.inc</c>, whose fields sum to
    /// <see cref="StateDataSize"/> (2775 = 0xad7) and whose <c>characters</c> array lands on the
    /// already-known <see cref="PartyActors"/> = 119 — two independent checks that the arithmetic is
    /// right, which matters because a wrong offset here still parses.
    /// </remarks>
    public const int ActivePartySize = 0x2b1;

    /// <summary>The active party's character indices — <c>activeParty</c>, three bytes.</summary>
    public const int ActivePartyMembers = 0x2b2;

    /// <summary>Slots the active-party array holds.</summary>
    public const int ActivePartySlots = 3;

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
    /// <remarks>
    /// <b>0x2cc, not 0x2c5.</b> It read 709 until 2026-08-24, seven bytes early, because the reader
    /// modelled <c>aSkillTrainRate</c> as two int16 and a pad rather than the six int16 the struct
    /// declares. Character N therefore carried character N-1's ranks and character 0 carried seven
    /// bytes of the preceding array. The writer used the same constant, so every round trip agreed
    /// with itself — see TASK-203.
    ///
    /// <para>Confirmed three ways: <c>gstate.inc</c> sums to <see cref="StateDataSize"/>; its other
    /// offsets land on <see cref="PartyActors"/>, <see cref="TimerPoolCount"/> and
    /// <see cref="PaletteEventMask"/>; and the engine's rest gate indexes
    /// <c>aSkillTrainRate + N + charSlot * 7</c> with a 1-BASED slot, which puts character 0 here.</para>
    /// </remarks>
    public const int ActorStatusEffects = 0x2cc;
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
