namespace GameData.Resources.Data;

// 10-byte landing position used in DEF_TRAP and DEF_COMB encounter records.
//
// When an encounter fires, the player is snapped to:
//   worldX = currentTileX × 0xFA00 + FineX
//   worldY = currentTileY × 0xFA00 + FineY
//   rotation_z = RotationZ
//
// (0xFA00 = 64000 = the per-tile fine-coordinate range.)
//
// DEF_TRAP and DEF_COMB carry four of these (one per approach direction)
// selected by sub_stub184_48()'s return value:
//   1 / 3 / 5 / 6 / 7 (default) → LandingDir1
//   2                            → LandingDir2
//   4                            → LandingDir4
//   8                            → LandingDir8
//
// CONFIRMED 2026-08-19 against the jump table at 0x7430c: the switch has
// arms for 2, 4 and 8 only, and 1/3/5/6/7 all fall to the default, which
// is the same entry direction 1 uses. So five of the eight directions
// share one landing. Executable form + tests: EncounterAftermath.LandingFor.
//
// CONFIRMED 2026-08-25 what the four values MEAN, from the selector itself
// (worldmove_aabb_outcode_rotated, WORLDMOV.C:192 — EncounterAftermath.ApproachDirection):
//   1 → the party is past the hotspot box's max-Y edge
//   2 → short of its min-X edge
//   4 → short of its min-Y edge
//   8 → anything else: the +X side, and also inside the box
// So 1/4 are the two Y answers and 2/8 the two X answers. Which reads as north
// or east is a compass convention the selector does not establish, so the old
// "N=1, E=2, S=4, W=8" guess is right about the axes and unverified about the
// direction within them — and getting 2 and 8 backwards throws a fleeing party
// to the far side of the encounter.
//
// For DEF_COMB the direction is measured AFTER the fight, from where the
// party is standing then — so the landing depends on which side they
// finished on, not on how they arrived.
//
// DEF_TRAP additionally carries a fifth LandingPrimary used when the
// trap fires without going through the directional switch.
public class LandingPosition {
    public int FineX { get; set; }       // 4 bytes — added to tileX × 0xFA00
    public int FineY { get; set; }       // 4 bytes — added to tileY × 0xFA00
    public ushort RotationZ { get; set; } // 2 bytes — player camera rotation when encounter fires
}
