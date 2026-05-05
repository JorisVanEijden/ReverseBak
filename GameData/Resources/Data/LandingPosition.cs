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
// The directional values look like cardinal-direction bit flags
// (N=1, E=2, S=4, W=8) but the exact mapping is not yet confirmed.
//
// DEF_TRAP additionally carries a fifth LandingPrimary used when the
// trap fires without going through the directional switch.
public class LandingPosition {
    public int FineX { get; set; }       // 4 bytes — added to tileX × 0xFA00
    public int FineY { get; set; }       // 4 bytes — added to tileY × 0xFA00
    public ushort RotationZ { get; set; } // 2 bytes — player camera rotation when encounter fires
}
