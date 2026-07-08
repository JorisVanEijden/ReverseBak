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
    public EncounterActorSetup EnemySetup { get; set; } = new(); // 44 — 339-byte actor-placement block (slotCount + 7×EnemySlot + trailer); identical layout to DEF_COMB's
    // 0x197 (bit 0 of a u16): the trap is Stealth-avoidable / Scouting-detectable. When true the
    // party rolls Stealth to evade the trap (and Scouting to detect it in the pre-fire phase);
    // when false the trap fires unconditionally — only an active Dragon's Breath spell can still
    // stealth past it. Gate in trapTrigger_phase2 @0x746c4 / trapTrigger_phase1 @0x745bf; the same
    // bit-0 toggle is DEF_COMB's field_18D. Higher bits are always 0 in shipping data.
    public bool Avoidable { get; set; }
}
