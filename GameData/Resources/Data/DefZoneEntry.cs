namespace GameData.Resources.Data;

using GameData.Resources.Location;

// One DEF_ZONE.DAT payload entry. Layout mirrors IDA's `def_zone` struct
// (size 19). Consumer: ovr187:sub_ovr187_11A2.
//
// Handler:
//   if (DialogId2 != 0) dialog_Show(DialogId2, 0);
//   sub_stub188_43();   // no args — likely begins a zone-transition pre-step
//   if (Location.ZoneNumber != currentZoneNumber) {
//       dispose_zone_data();
//       SetPlayerLocation(&Location);
//       resource_load_ZxxDEF_DAT();
//   } else {
//       TeleportToLocation(&Location);
//   }
//   ... standard cleanup ...
//   ProcessDialogFlags(2);
//
// DialogId1 (offset 9) is named in IDA but not referenced in this handler;
// may be used in another consumer or be an editor-only field.
public class DefZoneEntry {
    public ushort Gap0 { get; set; }            // offset 0 — IDA-marked gap, 2 bytes
    public Location Location { get; set; } = new(); // offset 2 — zone+tile+offset+rotation, 7 bytes
    public uint DialogId1 { get; set; }         // offset 9 — IDA-named DWORD; not read by ZONE handler
    public uint DialogId2 { get; set; }         // offset D — IDA-named DWORD; passed to dialog_Show if nonzero
    public byte Gap11 { get; set; }             // offset 11 — IDA-marked gap, 1 byte
    public byte Field12 { get; set; }           // offset 12 — IDA-named char; usage TBD
}
