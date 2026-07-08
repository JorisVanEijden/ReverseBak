namespace GameData.Resources.World;

using GameData.Resources.GameState;

// One row from a Tzzxxyy.DAT chapter block (19 bytes on disk).
// Fires when the player is inside the sub-tile rectangle and the global-key
// preconditions hold (see TileEventTile docs for gating semantics).
// EntryNumber indexes into the def_<Type>.dat file named by Type.
public class TileEventTrigger
{
    public TileEventType Type { get; set; }

    public byte StartX { get; set; }
    public byte EndY { get; set; }
    public byte EndX { get; set; }
    public byte StartY { get; set; }

    public uint EntryNumber { get; set; }

    // One-shot flag (0/1). When set, firing this trigger writes a persistent per-(zone,tile,def)
    // save-state flag (markTriggerFired @0x74dbb: SetGlobalValue(400*zone + 10*tile + defFileNr, 1));
    // hasTriggerFired reads that same key inside isTriggerEnabled, so the trigger never fires again.
    // When 0 the trigger re-fires on every tile entry. All trigger handlers honour it; in the
    // shipping data authors only set it on ~30% of Dial records and 2 Comb records.
    public byte FireOnce { get; set; }

    // Gate: trigger fires only while this condition holds (decoded from the
    // required save-state key; null = no requirement).
    public Condition? Requires { get; set; }

    // Gate: trigger is suppressed while this condition holds (decoded from the
    // forbidden save-state key; null = no suppression).
    public Condition? Forbids { get; set; }

    // The state mutation applied when the trigger fires (decoded from the
    // set-on-fire key; null = sets nothing).
    public Effect? OnFire { get; set; }

    // Boolean in practice (0/1). Only ever set on 7 Dial records across all shipping files.
    // Dial handler: when set, returns early — skipping both the FireOnce marking
    // (markTriggerFired) and SetGlobalFlag5200_5209.
    public ushort Field11 { get; set; }
}
