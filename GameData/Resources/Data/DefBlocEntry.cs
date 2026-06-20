namespace GameData.Resources.Data;

// One DEF_BLOC.DAT payload entry. Layout mirrors IDA's `def_bloc` struct
// (size 8). Consumer: blocTrigger_phase1 (ovr187:0x74c64) — BLOC is phase-1
// only (excluded from the phase-2 main-fire dispatcher). On entering a
// Bloc-trigger tile it shows DialogId via dialog_Show(DialogId, 1).
//
// Gap0/Gap6/Field7: verified NOT read by the engine (2026-06-20) — the handler
// reads only DialogId. Gap0 varies (17 distinct, bitmask-shaped) but is inert;
// vacant (status 0) records carry leftover garbage there (984/1007/1015).
// Editor metadata, preserved for round-trip.
public class DefBlocEntry {
    public ushort Gap0 { get; set; }      // offset 0 — unread by engine (editor metadata; bitmask-shaped but inert)
    public uint DialogId { get; set; }    // offset 2 — shown via dialog_Show(DialogId, 1) by blocTrigger_phase1
    public byte Gap6 { get; set; }        // offset 6 — unread by engine (const 0)
    public byte Field7 { get; set; }      // offset 7 — unread by engine (const 0)
}
