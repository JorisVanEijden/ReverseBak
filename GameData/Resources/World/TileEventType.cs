namespace GameData.Resources.World;

// Discriminator for TileEventTrigger.Type. Values 0-11 match the enum
// embedded in the original game's def_file_struct.type field. Each value
// names a "def_*.dat" family that the trigger's EntryNumber indexes into.
//
// Note: Comm, Heal and Bloc never appear in shipping data and have no
// runtime handler in ovr187. Soun has a handler but no data. The other
// eight types are actively used.
public enum TileEventType : ushort
{
    Bkgr = 0,
    Comb = 1,
    Comm = 2,
    Dial = 3,
    Heal = 4,
    Soun = 5,
    Town = 6,
    Trap = 7,
    Zone = 8,
    Disa = 9,
    Enab = 10,
    Bloc = 11
}
