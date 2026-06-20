namespace GameData.Resources.Data;

using GameData.Resources.GameState;

// One DEF_ENAB.DAT payload entry. Layout mirrors IDA's `def_enab` struct
// (size 7). Consumer: enabTrigger_phase2 (ovr187:0x74bf2). No phase-1 handler.
//
// Handler rolls (random%4096) % 100 and compares against Chance. If the
// roll <= Chance and GlobalKey != 0, sets that global to 1.
//
// Identical struct layout to def_disa (which clears the bit instead of
// setting it).
//
// Field0/Field1/Field5/Field6: verified NOT read by the engine (2026-06-20) — the
// handler reads only Chance + GlobalKey. Editor metadata, preserved for round-trip.
public class DefEnabEntry {
    public byte Field0 { get; set; }      // offset 0 — unread by engine (editor metadata)
    public byte Field1 { get; set; }      // offset 1 — unread by engine (editor metadata)
    public byte Chance { get; set; }      // offset 2 — activation probability 0-100
    public ushort GlobalKey { get; set; } // offset 3 — flag index to set to 1 when activated
    public byte Field5 { get; set; }      // offset 5 — unread by engine (editor metadata)
    public byte Field6 { get; set; }      // offset 6 — unread by engine (editor metadata)

    /// <summary>The global-state mutation this entry applies when it fires (sets the flag); null = no-op (GlobalKey 0).</summary>
    public Effect? OnFire { get; set; }
}
