namespace GameData.Resources.Data;

// One DEF_BLOC.DAT payload entry. Layout mirrors IDA's `def_bloc` struct
// (size 8). Consumer not yet identified — the Bloc trigger type is in
// the def_files enum (value 11) and the data file ships, but no obvious
// consumer in ovr187 references def_bloc_dat as a symbol.
public class DefBlocEntry {
    public ushort Gap0 { get; set; }      // offset 0 — IDA-marked gap, 2 bytes
    public uint DialogId { get; set; }    // offset 2 — IDA-named u32 dialog reference
    public byte Gap6 { get; set; }        // offset 6 — IDA-marked gap, 1 byte
    public byte Field7 { get; set; }      // offset 7 — IDA-named char; usage TBD
}
