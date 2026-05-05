namespace GameData.Resources.Data;

// One DEF_BKGR.DAT payload entry. Layout mirrors IDA's `def_bkgr` struct
// (size 21). Field naming and offsets are loader-side; semantics are
// derived from the per-type handler at ovr187:sub_ovr187_5E4.
//
// Identical layout to def_town. Both handlers structurally match:
//   if (field_12 != 0) sub_ovr180_8EA(&field_E)
//   sub_ovr149_6CD(letter=1, number=field_2)
//
// See docs/FileFormats/DEF_DAT family.md for status markers per field.
public class DefBkgrEntry {
    public ushort Gap0 { get; set; }      // offset 0  — IDA-marked gap, 2 bytes
    public ushort Field2 { get; set; }    // offset 2  — passed as "number" to sub_ovr149_6CD with letter=1
    public ushort Gap4 { get; set; }      // offset 4  — IDA-marked gap, 2 bytes
    public uint DialogId { get; set; }    // offset 6  — IDA-named DWORD; consumer not visible in handler disasm yet
    public uint GapA { get; set; }        // offset A  — IDA-marked gap, 4 bytes
    public ushort FieldE { get; set; }    // offset E  — address passed to sub_ovr180_8EA when Field12 != 0
    public ushort Gap10 { get; set; }     // offset 10 — IDA-marked gap, 2 bytes
    public byte Field12 { get; set; }     // offset 12 — gate for the FieldE action
    public byte Field13 { get; set; }     // offset 13 — usage not visible in handler disasm
    public byte Field14 { get; set; }     // offset 14 — usage not visible in handler disasm
}
