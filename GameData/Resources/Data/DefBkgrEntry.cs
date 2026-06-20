namespace GameData.Resources.Data;

// One DEF_BKGR.DAT payload entry. Layout mirrors IDA's `def_bkgr` struct
// (size 21), identical to def_town. Two-phase handler (reversed 2026-06-20):
//
//   phase1 bkgrTrigger_phase1 (ovr187:0x73ecf):
//       if (DialogId != 0) dialog_Show(DialogId, 0)              // entry narration
//   phase2 bkgrTrigger_phase2 (ovr187:0x73f34):
//       if (DoApproachWalk != 0) approachWalkToScenePosition(&ApproachTileOffset) // ovr180:0x6dc6a
//       GDS_RunScene(number=GdsSceneNumber, letter=1)            // enters GDS<n>A scene
//       if (setOnFireKey) SetGlobalValue(setOnFireKey, 1)
//
// So entering a BKGR/TOWN tile shows a narration dialog, then optionally
// auto-walks the party up to a spot (facing a heading) and runs a GDS scene.
// See docs/FileFormats/DEF_DAT family.md.
public class DefBkgrEntry {
    public ushort Gap0 { get; set; }            // offset 0  — IDA-marked gap, 2 bytes (unread)

    /// <summary>+0x02. GDS scene number — the trigger runs <c>GDS&lt;n&gt;A.DAT</c> via
    /// GDS_RunScene(number=this, letter=1). For DEF_TOWN this is 1..12 (the 12 town-gate
    /// scenes); for DEF_BKGR it is the backdrop scene number. (Was Field2.)</summary>
    public ushort GdsSceneNumber { get; set; }

    public ushort Gap4 { get; set; }            // offset 4  — IDA-marked gap, 2 bytes (unread)
    public uint DialogId { get; set; }          // offset 6  — shown on entry by bkgrTrigger_phase1 (0x73ecf) via dialog_Show(DialogId, 0); the narration text
    public uint GapA { get; set; }              // offset A  — consistently DialogId+1 in shipped data; not read by either handler (editor sibling ref)

    /// <summary>+0x0E. Approach-walk destination, as a packed sub-tile offset within the
    /// current world tile: <b>low byte = X offset, high byte = Y offset</b>. When
    /// <see cref="DoApproachWalk"/> is set, the handler passes <c>&amp;this</c> to
    /// approachWalk (sub_ovr180_8EA), which auto-travels the player to this spot before the
    /// scene. (Was FieldE.)</summary>
    public ushort ApproachTileOffset { get; set; }

    /// <summary>+0x10. Approach-walk facing — a 16-bit heading (0x10000 = full turn; shipped
    /// values are 45° multiples e.g. 0x4000/0x6000/0xA000/0xC000). Written to the camera
    /// entity's rotation by approachWalk. (Was Gap10.)</summary>
    public ushort ApproachHeading { get; set; }

    /// <summary>+0x12. Gate: when non-zero, perform the approach-walk
    /// (<see cref="ApproachTileOffset"/> / <see cref="ApproachHeading"/>) before running the
    /// scene. 1 on every active TOWN entry. (Was Field12.)</summary>
    public byte DoApproachWalk { get; set; }

    public byte Field13 { get; set; }           // offset 13 — unread by engine (const 0)
    public byte Field14 { get; set; }           // offset 14 — unread by engine (const 0)
}
