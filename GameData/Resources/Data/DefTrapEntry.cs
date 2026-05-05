namespace GameData.Resources.Data;

// One DEF_TRAP.DAT payload entry. Layout mirrors IDA's `def_trap`
// struct (size 409). Consumer: ovr187:sub_ovr187_D74.
//
// The handler is gameplay-critical — it gates encounter triggering
// based on Stealth attribute, Dragon's Breath spell timer, and a
// per-encounter cooldown read from temp.gam. See the family doc for
// the high-level flow. Not all field semantics are confirmed; bar-D
// runtime trace via Spice86 is deferred as follow-up work.
public class DefTrapEntry {
    public ushort Gap0 { get; set; }                    // 0
    public uint EncounterNumber { get; set; }           // 2  — index passed to isEncounterIdWhitelisted and sub_stub168_2F
    public uint DialogId1 { get; set; }                 // 6  — passed to dialog_Show (talkType=1) before encounter trigger
    public uint DialogId2 { get; set; }                 // A  — IDA-named DWORD; usage in handler not visible in first 200 instructions
    public uint GapE { get; set; }                      // E  — 4 bytes
    public byte Field12 { get; set; }                   // 12
    public byte[] Gap13 { get; set; } = new byte[9];    // 13
    public byte Field1C { get; set; }                   // 1C
    public byte[] Gap1D { get; set; } = new byte[9];    // 1D
    public byte Field26 { get; set; }                   // 26
    public byte[] Gap27 { get; set; } = new byte[9];    // 27
    public byte Field30 { get; set; }                   // 30
    public byte[] Gap31 { get; set; } = new byte[9];    // 31
    public Coordinates64k Coordinates { get; set; } = new(); // 3A — adjusted player position when trap fires
    public ushort Field42 { get; set; }                 // 42 — assigned to camera rotation Z when trap fires
    public DefTrapStruct339 Struct339 { get; set; } = new(); // 44 — 339 bytes; field_1 is read as creatureType
    public ushort Field197 { get; set; }                // 197 — bit 0 gates the "stealth check" path
}

// Mirrors IDA's `unknownStruct339`. Mostly opaque; field_1 is read
// from the trap-handler as the creatureType global.
public class DefTrapStruct339 {
    public byte Gap0 { get; set; }                      // 0
    public ushort Field1 { get; set; }                  // 1 — assigned to global `creatureType` when trap fires
    public byte[] Gap3 { get; set; } = new byte[335];   // 3
    public byte Field152 { get; set; }                  // 152
}
