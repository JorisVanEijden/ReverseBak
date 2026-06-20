namespace GameData.Resources.Data;

// One DEF_DIAL.DAT payload entry. IDA's `def_dial` struct is 7 bytes;
// the on-disk payload is 8 bytes (the 8th byte is always 0 in shipping
// data — included here for round-trip completeness).
//
// Consumer: dialTrigger_phase2 (ovr187:0x74418). DIAL has no phase-1 handler.
//
// Handler:
//   if (DialogId != 0) dialog_Show(DialogId, 1);
//   ... standard cleanup gated by def_file_struct.setOnFireKey/field_A/field_11 ...
//
// Field0/Field1/Field6: verified NOT read by the engine (2026-06-20) — the handler
// reads only DialogId; gating is the trigger header (isTriggerEnabled). They vary in
// the data (bitmask-shaped) but are editor metadata, preserved for round-trip. See
// docs/field-data-progress.md (DEF family).
public class DefDialEntry {
    public byte Field0 { get; set; }    // offset 0 — unread by engine (editor metadata)
    public byte Field1 { get; set; }    // offset 1 — unread by engine (editor metadata)
    public uint DialogId { get; set; }  // offset 2 — passed to dialog_Show with talkType=1
    public byte Field6 { get; set; }    // offset 6 — unread by engine (editor metadata)
    public byte Pad7 { get; set; }      // offset 7 — always 0 in shipping data; not modeled by IDA's struct
}
