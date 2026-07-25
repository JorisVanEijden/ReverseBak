namespace GameData.Resources.Data;

// The 339-byte encounter actor-placement block shared by DEF_COMB (at
// payload offset 0x3A) and DEF_TRAP (at payload offset 0x44). Identical
// layout in both. Mirrors IDA's `encounterEnemyTable`.
//
// When an encounter is initialised, placeEncounterActors (ovr188 @0x75359)
// SCOPYs this whole block into the runtime buffer `p5times339bytes` and walks
// the slot array to position each enemy in the world. Exactly seven functions
// reference the buffer (complete, non-truncated xref set); reading all seven at
// the instruction level, the only slot bytes any consumer touches are
// CreatureNumber (0x00), MovementPattern (0x02), PrimarySpawn* (0x04/0x08/0x0C)
// and AltSpawn* (0x0E..0x2D) — the highest byte read is AltSpawnY[3] ending at
// 0x2D. Each slot's AuthoringWord (0x2E) and the block Trailer (0x151) are
// therefore verified dead: no reader in the binary consumes them.
//
// See docs/FileFormats/DEF_DAT family.md ("Encounter actor placement").
public class EncounterActorSetup {
    public byte SlotCount { get; set; }                 // 0     — populated EnemySlot count (0..7)
    public EnemySlot[] Slots { get; set; }              // 1     — 7 × 48-byte slots
        = new EnemySlot[7];
    public ushort Trailer { get; set; }                 // 0x151 — 2 bytes; DEAD (no readers; editor-side/uninitialised)
}

// One 48-byte enemy placement record inside EncounterActorSetup. Mirrors
// IDA's `encounterEnemy` struct.
public class EnemySlot {
    public ushort CreatureNumber { get; set; }          // 0x00 — `mnames` enum. Slot 0's value is assigned to the `creatureType` global when the encounter fires.

    /// <summary>De-indexed <see cref="CreatureNumber"/>: <c>base:mnames:&lt;CreatureNumber&gt;</c>, the
    /// creature this slot spawns. Resolves to a <c>CreatureName.Key</c>. See
    /// docs/re-notes/reference-inventory.md #15.</summary>
    public string CreatureKey { get; set; } = "";
    public ushort MovementPattern { get; set; }         // 0x02 — Roaming-enemy patrol selector, read by updateRoamingEncounterActors (ovr188 @0x7600b). Domain {0..4}:
                                                        //          0 = stationary
                                                        //          1 = ping-pong between AltSpawn[0..1] (180° about-face on reach)
                                                        //          2 = 4-waypoint circuit, turn −90° on reach
                                                        //          3 = 4-waypoint circuit, turn +90° on reach
                                                        //          4 = auto-travel / road-follow pathing (TryAutoTravelStep)
    public int PrimarySpawnX { get; set; }              // 0x04 — tile-relative X of the main spawn (world = tileX × 0xFA00 + this)
    public int PrimarySpawnY { get; set; }              // 0x08 — tile-relative Y of the main spawn
    public short PrimaryRotationZ { get; set; }         // 0x0C — heading at the main spawn
    public int[] AltSpawnX { get; set; }                // 0x0E — i32[4] tile-relative X of 4 alternate spawns / patrol waypoints
        = new int[4];
    public int[] AltSpawnY { get; set; }                // 0x1E — i32[4] tile-relative Y of 4 alternate spawns / patrol waypoints
        = new int[4];
    public ushort AuthoringWord { get; set; }           // 0x2E — DEAD to the runtime (no reader among the 7 p5times339bytes
                                                        //          consumers, verified at the instruction level). On disk the
                                                        //          two bytes behave as one 16-bit LE value: 0xFFFF sentinel
                                                        //          (once per record on avg), recurring clusters (0x4B00, 0x2147),
                                                        //          frequently identical across a record's populated slots. So it
                                                        //          is *structured* authoring data, not uninitialised filler —
                                                        //          but its authoring purpose is undetermined (no correlation with
                                                        //          MovementPattern/CreatureNumber, and no runtime consumer to anchor it).
}
