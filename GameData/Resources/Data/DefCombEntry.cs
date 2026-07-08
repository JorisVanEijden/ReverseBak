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
// EnemySetup (offset 0x3A) is the 339-byte encounter actor-placement
// block — see EncounterActorSetup. placeEncounterActors (ovr188 @0x75359)
// SCOPYs it into p5times339bytes and the combat engine positions/animates
// each enemy from it. (IDA models the same bytes as field_3A:slotCount +
// the leading creatureNumber + a 336-byte tail.)
// See docs/FileFormats/DEF_DAT family.md.
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
    public EncounterActorSetup EnemySetup { get; set; } = new(); // 3A — 339-byte actor-placement block (slotCount + 7×EnemySlot + trailer)
    // 0x18D (bit 0 of a u16): the encounter is Stealth-avoidable / Scouting-detectable — the same
    // gate as DEF_TRAP's Avoidable (+0x197). When true the party rolls Stealth to evade (and
    // Scouting to detect); when false it fires unconditionally (only Dragon's Breath can still
    // stealth past it). combTrigger_phase2 @0x7409d. Higher bits are always 0 in shipping data.
    public bool Avoidable { get; set; }
}
