namespace GameData.Resources.Data;

using GameData.Resources.Location;

// One DEF_ZONE.DAT payload entry. Layout mirrors IDA's `def_zone` struct
// (size 19). Consumers: zoneTrigger_phase1 @0x74a82 and zoneTrigger_phase2 @0x74af2.
//
// *** CROSSING A ZONE BOUNDARY IS CONFIRMED, NOT AUTOMATIC. *** The handler is two
// phases and phase 1 is a PROMPT that gates the whole thing:
//
//   phase 1 (0x74a82):
//     if (!isTriggerEnabled(trigger))   -> do not cross
//     else if (DialogId1 == 0)          -> do not cross      <- note: no prompt means NO crossing
//     else                              -> cross only if dialog_Show(DialogId1) == 0
//
//   phase 2 (0x74af2), reached only when phase 1 said cross:
//     if (DialogId2 != 0) dialog_Show(DialogId2, 0);   // a message, not a question
//     if (Location.ZoneNumber != currentZoneNumber) { dispose_zone_data(); SetPlayerLocation(&Location); resource_load_ZxxDEF_DAT(); }
//     else                                            { TeleportToLocation(&Location); }
//     ... setOnFireKey / fireOnce bookkeeping ... FinishDialogWait(2);
//
// An earlier version of this comment said the handler shows DialogId2 and that
// DialogId1 was "not referenced ... may be editor-only". BOTH HALVES WERE WRONG, and
// porting from it would have produced a boundary that never asks and crosses the
// instant you touch it. The shipped data is the tell: all 39 records carry a
// DialogId1 (2700xxx) and every single DialogId2 is zero.
//
public class DefZoneEntry {
    public ushort Gap0 { get; set; }            // offset 0 — IDA-marked gap, 2 bytes
    public Location Location { get; set; } = new(); // offset 2 — zone+tile+offset+rotation, 7 bytes
    public uint DialogId1 { get; set; }         // offset 9 — THE CONFIRM PROMPT. Zero = never crosses.
    public uint DialogId2 { get; set; }         // offset D — post-confirmation message; zero in all 39 shipped records
    public byte Gap11 { get; set; }             // offset 11 — IDA-marked gap, 1 byte
    public byte Field12 { get; set; }           // offset 12 — IDA-named char; usage TBD
}
