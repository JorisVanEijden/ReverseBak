namespace GameData.Resources.Data;

// One DEF_COMB.DAT payload entry. Layout mirrors IDA's `def_comb`
// struct (size 399). The original IDA struct models the four
// directional landings as `field_<X>:int + gap<X+2>[8]`; the C# model
// collapses each into a `LandingPosition` (10 bytes: FineX:i32 +
// FineY:i32 + RotationZ:u16).
//
// Three consumers in ovr187:
//   sub_ovr187_3C5 (multi-trigger Comb/Trap dispatcher) — reads
//     DialogId1 and shows it.
//   sub_ovr187_648 (pre-fire) — Scouting check; if detected, shows
//     DialogId2.
//   sub_ovr187_74D (main fire) — Stealth check; if not avoided,
//     snaps player to a directional landing, shows DialogId1,
//     starts the encounter via sub_stub168_2F.
//
// Plus DEF_COMB.Field3A is read by ovr188:sub_ovr188_1E9 (combat
// initialization), purpose TBD.
public class DefCombEntry {
    public ushort Field0 { get; set; }                      // 0   — verified unread 2026-06-20 (all 3 consumers traced: combTrigger_phase1 0x73f98, combTrigger_phase2 0x7409d, sub_ovr187_3C5 0x73d15); editor metadata
    public uint EncounterNumber { get; set; }               // 2   — index into combat encounter table
    public uint DialogId1 { get; set; }                     // 6   — multi-trigger dispatcher AND main-fire dialog
    public uint DialogId2 { get; set; }                     // A   — pre-fire dialog (sub_ovr187_648 → dialog_Show)
    public byte GapE { get; set; }                          // E   — no readers (dead)
    public ushort GlobalKey { get; set; }                   // F   — IDA-named int; no readers (dead)
    public byte Gap11 { get; set; }                         // 11  — no readers (dead)
    public LandingPosition LandingDir1 { get; set; } = new(); // 12 — default landing (dir 1/3/5/6/7)
    public LandingPosition LandingDir2 { get; set; } = new(); // 1C — landing for dir 2
    public LandingPosition LandingDir4 { get; set; } = new(); // 26 — landing for dir 4
    public LandingPosition LandingDir8 { get; set; } = new(); // 30 — landing for dir 8
    public byte Field3A { get; set; }                       // 3A  — read by ovr188:sub_ovr188_1E9 (combat init); purpose TBD
    public ushort MonsterNumber { get; set; }               // 3B  — assigned to creatureType global on fire
    public byte[] Gap3D { get; set; } = new byte[336];      // 3D  — 336 bytes opaque; no individual-field readers in trigger path
    public ushort Field18D { get; set; }                    // 18D — bit 0 gates the stealth/scouting detection path
}
