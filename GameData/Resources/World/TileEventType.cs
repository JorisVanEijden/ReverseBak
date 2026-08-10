namespace GameData.Resources.World;

// Discriminator for TileEventTrigger.Type. Values 0-11 match the enum
// embedded in the original game's def_file_struct.type field. Each value
// names a "def_*.dat" family that the trigger's EntryNumber indexes into.
//
// Note: Comm and Heal never appear in shipping data and have no runtime
// handler. Soun has a handler but no data. The other ten types are actively
// used — Bloc included: it ships 81 records and IS handled, by the activate
// pass only (canassa hotspotevt_dlg_run_msg_event, case 11 of
// hotspotevt_activate_at_player). It is the game's data-driven invisible
// wall: when its gates pass it plays a dialog and reports "interacted",
// which reverts the step that entered the tile. See
// docs/specs/collision-system.md §3.4.1.
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
