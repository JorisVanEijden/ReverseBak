namespace GameData.Resources.Data;

// The 339-byte encounter actor-placement block shared by DEF_COMB (at
// payload offset 0x3A) and DEF_TRAP (at payload offset 0x44). Identical
// layout in both. Mirrors IDA's `encounterEnemyTable`.
//
// When an encounter is initialised, placeEncounterActors (ovr188 @0x75359)
// SCOPYs this whole block into the runtime buffer `p5times339bytes` and walks
// the slot array to position each enemy in the world. Seven consumers read
// the buffer; between them they touch CreatureNumber, MovementPattern,
// PrimarySpawn*, and AltSpawn*. The remaining bytes (each slot's Field2E/2F
// and the block Trailer) are verified dead — no reader in the binary.
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
    public byte Field2E { get; set; }                   // 0x2E — DEAD (no readers; unstructured editor-side bytes)
    public sbyte Field2F { get; set; }                  // 0x2F — DEAD (no readers; unstructured editor-side bytes)
}
