namespace GameData.Resources.Data;

// One DEF_DIAL.DAT payload entry. IDA's `def_dial` struct is 7 bytes;
// the on-disk payload is 8 bytes (the 8th byte is always 0 in shipping
// data — included here for round-trip completeness).
//
// Consumer: ovr187:sub_ovr187_AC8.
//
// Handler:
//   if (DialogId != 0) dialog_Show(DialogId, 1);
//   ... standard cleanup gated by def_file_struct.global_key3/field_A/field_11 ...
public class DefDialEntry {
    public byte Field0 { get; set; }    // offset 0 — IDA-named char; usage not visible in handler
    public byte Field1 { get; set; }    // offset 1 — IDA-named char; usage not visible in handler
    public uint DialogId { get; set; }  // offset 2 — passed to dialog_Show with talkType=1
    public byte Field6 { get; set; }    // offset 6 — IDA-named char; usage not visible in handler
    public byte Pad7 { get; set; }      // offset 7 — always 0 in shipping data; not modeled by IDA's struct
}
