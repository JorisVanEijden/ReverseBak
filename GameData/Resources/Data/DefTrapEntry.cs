namespace GameData.Resources.Data;

// One DEF_TRAP.DAT payload entry. Layout mirrors IDA's `def_trap`
// struct (size 409). The original IDA struct represents the four
// directional landing positions as `field_<X> + gap<X+1>[9]` pairs;
// the C# model collapses each into a `LandingPosition` (10 bytes:
// FineX:i32 + FineY:i32 + RotationZ:u16) reflecting the actual usage.
//
// Two consumers in ovr187:
//   sub_ovr187_C6F (pre-fire) — Scouting check; if detected, shows
//     DialogId2 and updates Scouting attribute.
//   sub_ovr187_D74 (main fire) — Stealth check; if not avoided,
//     snaps player to one of the landing positions, shows DialogId1,
//     starts the encounter via sub_stub168_2F.
//
// See docs/FileFormats/DEF_DAT family.md for the full pipeline writeup.
public class DefTrapEntry {
    public ushort Gap0 { get; set; }                        // 0   — IDA-marked gap; no readers (dead)
    public uint EncounterNumber { get; set; }               // 2   — index into combat encounter table
    public uint DialogId1 { get; set; }                     // 6   — main-fire dialog (sub_ovr187_D74 → dialog_Show)
    public uint DialogId2 { get; set; }                     // A   — pre-fire dialog (sub_ovr187_C6F → dialog_Show)
    public uint GapE { get; set; }                          // E   — IDA-marked gap; no readers (dead)
    public LandingPosition LandingDir1 { get; set; } = new(); // 12 — default landing (dir 1/3/5/6/7)
    public LandingPosition LandingDir2 { get; set; } = new(); // 1C — landing for dir 2
    public LandingPosition LandingDir4 { get; set; } = new(); // 26 — landing for dir 4
    public LandingPosition LandingDir8 { get; set; } = new(); // 30 — landing for dir 8
    public LandingPosition LandingPrimary { get; set; } = new(); // 3A (= coordinates_64k + field_42) — landing for the non-directional fire path
    public DefTrapStruct339 Struct339 { get; set; } = new(); // 44 — 339 bytes; only Field1 (creatureType) is read
    public ushort Field197 { get; set; }                    // 197 — bit 0 gates the stealth/scouting detection path
}

// Mirrors IDA's `unknownStruct339`. Mostly opaque; only Field1 is read
// (assigned to global `creatureType` when the trap fires).
public class DefTrapStruct339 {
    public byte Gap0 { get; set; }                          // 0   — no readers (dead)
    public ushort Field1 { get; set; }                      // 1   — creatureType global
    public byte[] Gap3 { get; set; } = new byte[335];       // 3   — opaque; no individual-field readers
    public byte Field152 { get; set; }                      // 152 — no readers (dead)
}
