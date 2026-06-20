namespace GameData.Resources.Data;

// One DEF_TOWN.DAT payload entry. Layout mirrors IDA's `def_town` struct
// (size 21) — identical to def_bkgr, and handled identically (townTrigger_phase1
// 0x744f6 shows DialogId; townTrigger_phase2 0x7455b does the approach-walk +
// GDS_RunScene). See DefBkgrEntry for the full handler walkthrough.
// Reversed 2026-06-20.
public class DefTownEntry {
    public ushort Gap0 { get; set; }            // offset 0  — IDA-marked gap, 2 bytes (unread)

    /// <summary>+0x02. GDS scene number — runs <c>GDS&lt;n&gt;A.DAT</c> via
    /// GDS_RunScene(number=this, letter=1). For DEF_TOWN this is 1..12 (one per town gate).
    /// (Was Field2.)</summary>
    public ushort GdsSceneNumber { get; set; }

    public ushort Gap4 { get; set; }            // offset 4  — IDA-marked gap, 2 bytes (unread)
    public uint DialogId { get; set; }          // offset 6  — shown on entry by townTrigger_phase1 (0x744f6) via dialog_Show(DialogId, 0)
    public uint GapA { get; set; }              // offset A  — consistently DialogId+1 in shipped data; not read by either handler (editor sibling ref)

    /// <summary>+0x0E. Approach-walk destination as a packed sub-tile offset
    /// (low byte = X, high byte = Y); auto-traveled to before the scene when
    /// <see cref="DoApproachWalk"/> is set. (Was FieldE.) See DefBkgrEntry.</summary>
    public ushort ApproachTileOffset { get; set; }

    /// <summary>+0x10. Approach-walk facing (16-bit heading; 45° multiples in shipped data).
    /// (Was Gap10.)</summary>
    public ushort ApproachHeading { get; set; }

    /// <summary>+0x12. Gate: non-zero → perform the approach-walk before the scene
    /// (1 on every active TOWN entry). (Was Field12.)</summary>
    public byte DoApproachWalk { get; set; }

    public byte Field13 { get; set; }           // offset 13 — unread by engine (const 0)
    public byte Field14 { get; set; }           // offset 14 — unread by engine (const 0)
}
