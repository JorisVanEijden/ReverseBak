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

    // Boolean in practice (0/1). Only set on Dial records (~30%) and 2 Comb
    // records across all shipping files. Dial handler: when set, runs an
    // extra cleanup step before SetGlobalFlag5200_5209.
    public byte FieldA { get; set; }

    // Gate: trigger fires only while this condition holds (decoded from the
    // required save-state key; null = no requirement).
    public Condition? Requires { get; set; }

    // Gate: trigger is suppressed while this condition holds (decoded from the
    // forbidden save-state key; null = no suppression).
    public Condition? Forbids { get; set; }

    // The state mutation applied when the trigger fires (decoded from the
    // set-on-fire key; null = sets nothing).
    public Effect? OnFire { get; set; }

    // Boolean in practice (0/1). Only ever set on 7 Dial records across all
    // shipping files. Dial handler: when set, returns early — skipping both
    // the FieldA cleanup and SetGlobalFlag5200_5209.
    public ushort Field11 { get; set; }
}
