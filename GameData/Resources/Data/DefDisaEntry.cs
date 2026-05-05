namespace GameData.Resources.Data;

// One DEF_DISA.DAT payload entry. Layout mirrors IDA's `def_disa` struct
// (size 7). Consumer: ovr187:sub_ovr187_1230.
//
// Handler rolls (random%4096) % 100 and compares against Chance. If the
// roll <= Chance and GlobalKey != 0, sets that global to 0 (clears it).
//
// Identical struct layout to def_enab (which sets the bit instead of
// clearing it).
public class DefDisaEntry {
    public byte Field0 { get; set; }      // offset 0 — usage not visible in handler
    public byte Field1 { get; set; }      // offset 1 — usage not visible in handler
    public byte Chance { get; set; }      // offset 2 — activation probability 0-100
    public ushort GlobalKey { get; set; } // offset 3 — flag index to set to 0 when activated
    public byte Field5 { get; set; }      // offset 5 — usage not visible in handler
    public byte Field6 { get; set; }      // offset 6 — usage not visible in handler
}
