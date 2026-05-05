namespace GameData.Resources.Data;

// One DEF_ENAB.DAT payload entry. Layout mirrors IDA's `def_enab` struct
// (size 7). Consumer: ovr187:sub_ovr187_12A2.
//
// Handler rolls (random%4096) % 100 and compares against Chance. If the
// roll <= Chance and GlobalKey != 0, sets that global to 1.
//
// Identical struct layout to def_disa (which clears the bit instead of
// setting it).
public class DefEnabEntry {
    public byte Field0 { get; set; }      // offset 0 — usage not visible in handler
    public byte Field1 { get; set; }      // offset 1 — usage not visible in handler
    public byte Chance { get; set; }      // offset 2 — activation probability 0-100
    public ushort GlobalKey { get; set; } // offset 3 — flag index to set to 1 when activated
    public byte Field5 { get; set; }      // offset 5 — usage not visible in handler
    public byte Field6 { get; set; }      // offset 6 — usage not visible in handler
}
