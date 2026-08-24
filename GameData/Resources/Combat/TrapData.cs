namespace GameData.Resources.Combat;

/// <summary>
/// Engine-independent view of TRAPS.DAT — the per-combat-encounter grid layout table:
/// the starting positions of actors, cannons, the exit, and crystal/diamond traps on the
/// tactical combat grid.
///
/// On-disk layout: a flat array of <see cref="EncounterCount"/> fixed 62-byte records, one
/// per combat encounter number, seeked directly as <c>encounterNumber * 62</c>. Each record:
///   +0x00 u16            count        — number of active elements (read signed; ≤ 0 → none)
///   +0x02 element[15]     slots        — 15 fixed 4-byte slots {i16 type, u8 gridX, u8 gridY}
/// (2 + 15×4 = 62). Only the first <c>count</c> slots are read by the engine; the rest are
/// stale editor data and are dropped here.
///
/// Reversed from <c>Load_traps.dat</c> (seg033 @ 0x2e2ce), which seeks to
/// <c>encounterNumber_dseg_50F4 * 0x3E</c>, reads the count, then loops reading 4-byte
/// elements and dispatches each onto the combat grid (<c>apply_to_combat_grid_cell</c>,
/// <c>place_actor_on_grid</c>). See <c>docs/FileFormats/TRAPS.DAT.md</c>.
///
/// Grid coordinates are tactical-grid cells (small integers), not world units — left
/// unscaled. There is a matching <c>Write_traps_dat</c> (0x2f3ad), i.e. the original
/// combat editor could re-author this file.
/// </summary>
public class TrapData : IResource {
    /// <summary>Number of fixed 62-byte encounter records in the file (47616 / 62).</summary>
    public const int EncounterCount = 768;

    /// <summary>Element slots stored per record (only the first <c>count</c> are active).</summary>
    public const int SlotsPerEncounter = 15;

    public TrapData(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>One entry per encounter number (index = the encounter's record index).</summary>
    public List<TrapEncounter> Encounters { get; set; } = new();

    /// <summary>
    /// Whether the party may attempt to retreat from an encounter — see
    /// <see cref="TrapElementType.RetreatLock"/>.
    /// </summary>
    /// <remarks>
    /// <b>An encounter this file knows nothing about still allows escape.</b> The flag is raised
    /// before the record is read and only a <see cref="TrapElementType.RetreatLock"/> element lowers
    /// it, so "no record" and "an ordinary record" answer the same — which is what makes true the
    /// safe answer for a missing table rather than a guess.
    ///
    /// <para>A static predicate rather than a property on <see cref="TrapEncounter"/> deliberately:
    /// that type is serialized into <c>generated/TRAPS.json</c>, and a computed property there would
    /// change the committed output.</para>
    /// </remarks>
    public bool AllowsRetreat(int encounterNumber) {
        TrapEncounter record = Encounters?.Find(e => e.Index == encounterNumber);
        if (record?.Elements == null) {
            return true;
        }
        return !record.Elements.Exists(e => e.Type == (int)TrapElementType.RetreatLock);
    }
}

/// <summary>One combat encounter's grid layout (a 62-byte record).</summary>
public class TrapEncounter {
    /// <summary>Encounter number = this record's index in the file.</summary>
    public int Index { get; set; }

    /// <summary>Stable content-graph key of this encounter: <c>base:traps:&lt;Index&gt;</c>. The
    /// de-indexed target that DEF combat/trap records reference via their EncounterNumber. See
    /// docs/re-notes/reference-inventory.md #14.</summary>
    public string Key { get; set; } = "";

    /// <summary>Raw 16-bit element count from the record header. The engine compares it
    /// signed, so values ≤ 0 (e.g. the stray 0xFFEE) yield no active elements; the few
    /// records with a count &gt; 15 are unused/garbage slots. <see cref="Elements"/> holds
    /// the clamped active set.</summary>
    public int RawCount { get; set; }

    /// <summary>The active elements (first <c>clamp(RawCount, 0, 15)</c> slots).</summary>
    public List<TrapElement> Elements { get; set; } = new();
}

/// <summary>One grid placement within an encounter (4 bytes on disk).</summary>
public class TrapElement {
    /// <summary>+0x00 (i16). Raw element type — see <see cref="TrapElementType"/> for the
    /// decoded values. Unmapped values (e.g. 195) are preserved verbatim.</summary>
    public int Type { get; set; }

    /// <summary>+0x02 (u8). Tactical-grid X cell.</summary>
    public int GridX { get; set; }

    /// <summary>+0x03 (u8). Tactical-grid Y cell.</summary>
    public int GridY { get; set; }
}

/// <summary>
/// Decoded TRAPS.DAT element types, from the dispatch in <c>Load_traps.dat</c>.
/// Actors and cannons are stored negative on disk; the loader takes the absolute value
/// before dispatch. Actor placement uses combat-actor slot <c>|type| - 15</c>
/// (so -15 → slot 0, -16 → slot 1, -17 → slot 2). Crystals/diamonds are stored positive.
/// </summary>
public enum TrapElementType {
    /// <summary>0 — empty / no-op element (read but not placed).</summary>
    Empty = 0,

    /// <summary>7 — red crystal trap.</summary>
    RedCrystal = 7,

    /// <summary>8 — green crystal trap.</summary>
    GreenCrystal = 8,

    /// <summary>9 — solid diamond trap (blocks movement).</summary>
    DiamondSolid = 9,

    /// <summary>10 — pass-through diamond trap.</summary>
    DiamondPassthrough = 10,

    /// <summary>-6 — combat exit cell.</summary>
    Exit = -6,

    /// <summary>-10 — cannon facing west (|type| = 10).</summary>
    CannonWest = -10,

    /// <summary>-11 — cannon facing east.</summary>
    CannonEast = -11,

    /// <summary>-12 — cannon facing north.</summary>
    CannonNorth = -12,

    /// <summary>-13 — cannon facing south.</summary>
    CannonSouth = -13,

    /// <summary>-15 — actor placement, combat slot 0 (|type| - 15).</summary>
    ActorSlot0 = -15,

    /// <summary>-16 — actor placement, combat slot 1.</summary>
    ActorSlot1 = -16,

    /// <summary>-17 — actor placement, combat slot 2.</summary>
    ActorSlot2 = -17,

    /// <summary>
    /// -18 — <b>this encounter cannot be retreated from</b>. Places nothing on the grid.
    /// </summary>
    /// <remarks>
    /// <b>The only element type that is a rule rather than a placement</b>, which is why its
    /// coordinates are meaningless (four of the five records that carry it store (0,0), and the
    /// fifth stores junk). Every other type either sets a tile effect or positions an actor; this one
    /// takes a branch that does neither.
    ///
    /// <para><b>The flag it clears defaults to SET, so this record is an opt-OUT.</b> The loader
    /// raises the flag unconditionally as it opens the file and only this record lowers it, so 763
    /// of the 768 encounters allow a retreat and five forbid it — reading the flag as "trap data was
    /// loaded" (the shape of the original's variable name) inverts the default and would forbid
    /// escape everywhere.</para>
    ///
    /// <para>The five are 259, 347, 348, 349 and 463 — all trap-puzzle rooms, each also carrying an
    /// <see cref="Exit"/> tile and the three actor placements. <b>But the exit is not the rule</b>:
    /// 35 encounters carry an <see cref="Exit"/> and only these five carry the lock, so a port that
    /// derived "no retreat" from the presence of an exit would lock thirty fights the game lets you
    /// leave. The two markers are independent.</para>
    ///
    /// <para>Consumed by <c>CombatCommands.EscapeRollPasses</c>.</para>
    /// </remarks>
    RetreatLock = -18,
}
