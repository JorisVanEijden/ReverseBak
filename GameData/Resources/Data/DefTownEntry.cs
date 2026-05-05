namespace GameData.Resources.Data;

// One DEF_TOWN.DAT payload entry. Layout mirrors IDA's `def_town` struct
// (size 21) — identical to def_bkgr. Consumer: ovr187:sub_ovr187_C0B.
//
// Town handler is byte-for-byte identical to the Bkgr handler (only the
// def_file constant differs). See DefBkgrEntry for handler details.
public class DefTownEntry {
    public ushort Gap0 { get; set; }      // offset 0  — IDA-marked gap, 2 bytes
    public ushort Field2 { get; set; }    // offset 2  — passed as "number" to sub_ovr149_6CD with letter=1
    public ushort Gap4 { get; set; }      // offset 4  — IDA-marked gap, 2 bytes
    public uint DialogId { get; set; }    // offset 6  — IDA-named DWORD; consumer not visible in handler
    public uint GapA { get; set; }        // offset A  — IDA-marked gap, 4 bytes (often DialogId+1 in BKGR data; pattern TBD for TOWN)
    public ushort FieldE { get; set; }    // offset E  — address passed to sub_ovr180_8EA when Field12 != 0
    public ushort Gap10 { get; set; }     // offset 10 — IDA-marked gap, 2 bytes
    public byte Field12 { get; set; }     // offset 12 — gate for the FieldE action
    public byte Field13 { get; set; }     // offset 13 — usage not visible in handler
    public byte Field14 { get; set; }     // offset 14 — usage not visible in handler
}
