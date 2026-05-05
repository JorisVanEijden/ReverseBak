namespace GameData.Resources.Data;

// One DEF_COMB.DAT payload entry. Layout mirrors IDA's `def_comb`
// struct (size 399). Consumer: ovr187:sub_ovr187_74D.
//
// Combat encounter definition. The handler structurally mirrors the
// DEF_TRAP handler — same isEncounterIdWhitelisted gate, same Stealth
// attribute / Dragon's Breath spell-timer guard against triggering.
// After the gate, control flows into a different post-trigger path.
//
// Bar-D runtime Spice86 trace is deferred as follow-up work.
public class DefCombEntry {
    public ushort Field0 { get; set; }                  // 0
    public uint EncounterNumber { get; set; }           // 2  — index passed to isEncounterIdWhitelisted
    public uint DialogId1 { get; set; }                 // 6
    public uint DialogId2 { get; set; }                 // A
    public byte GapE { get; set; }                      // E
    public ushort GlobalKey { get; set; }               // F  — IDA-named
    public byte Gap11 { get; set; }                     // 11
    public ushort Field12 { get; set; }                 // 12
    public byte[] Gap14 { get; set; } = new byte[8];    // 14
    public ushort Field1C { get; set; }                 // 1C
    public byte[] Gap1E { get; set; } = new byte[8];    // 1E
    public ushort Field26 { get; set; }                 // 26
    public byte[] Gap28 { get; set; } = new byte[8];    // 28
    public ushort Field30 { get; set; }                 // 30
    public byte[] Gap32 { get; set; } = new byte[8];    // 32
    public byte Field3A { get; set; }                   // 3A
    public ushort MonsterNumber { get; set; }           // 3B — IDA-named
    public byte[] Gap3D { get; set; } = new byte[336];  // 3D — 336 bytes of opaque data
    public ushort Field18D { get; set; }                // 18D — bit 0 gates the "stealth check" path
}
