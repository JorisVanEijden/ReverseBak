namespace GameData.Resources.World;

/// <summary>
/// Clicking a tunnel or a tunnel exit — <c>wcursor_click_npc_or_trap</c> (WCURSOR.C:857), the
/// handler for entity kinds <b>20</b> and <b>39</b>.
/// </summary>
/// <remarks>
/// <b>Not the ladder's handler, and the difference is the whole level-traversal mechanic.</b>
/// <c>wcursor_click_world_hotspot</c> switches on <c>kind - 6</c>: 20 lands on case 14 and 39 on
/// case 33, both this routine, while only 42 (<see cref="TraversalClick">the ladder</see>) reaches
/// case 36. Giving all three the ladder's handler is what left the Mac Mordain Cadal with no exit —
/// its stairs answered "nothing happens" because the ladder's handler has no idea what a hotspot
/// action is.
///
/// <para><b>The traversal here is a HOTSPOT DISPATCH, not a dialog.</b> A tunnel's
/// <c>SUBREC_HOTSPOT</c> carries a grid coordinate naming which of the tile's hotspots to run, and
/// the routine calls <c>hotspotevt_dispatch_at_point(8, x, y)</c> with it. The interact message is
/// only the fallback for an object that has no hotspot — the reverse of the ladder, where the
/// message's own Teleport is the mechanism.</para>
/// </remarks>
public static class TunnelClick {
    /// <summary>The sound the click makes, shared with every fixed-object click.</summary>
    public const int ClickSound = 0x30;

    /// <summary>Dialog when there is nothing here to act on.</summary>
    /// <remarks>The same "nothing happens" record the building and ladder clicks use, and this
    /// routine reaches it from three directions: no fixed object, an object with neither a hotspot
    /// nor a message, and a hotspot dispatch that matched nothing.</remarks>
    public const int NothingToDoDialog = 0x9a;

    /// <summary>
    /// Dialog for a SECONDARY click — <b>0x62</b>, and it is neither the building's 0x60 nor the
    /// ladder's 0xae.
    /// </summary>
    /// <remarks>Three fixed-object handlers, three describe lines. The button test is
    /// <see cref="Menu.MenuClickButton"/>.</remarks>
    public const int DescribeDialog = 0x62;

    /// <summary>
    /// <b>THE REACH TEST IS UNCONDITIONAL, AND IT COMES FIRST.</b>
    /// </summary>
    /// <remarks>
    /// Before the sound, before the dialog, before anything:
    /// <c>pos.nWorld_x / 64000 == g_apCombat_zone_actor_lists[0]-&gt;bParty_x</c> and the same for
    /// y — the object must be in the party's own map tile. A click from outside returns in silence,
    /// with no message at all.
    ///
    /// <para><b>Same comparison as <see cref="FixedObjectClick.IsWithinReach"/>, different rule.</b>
    /// The building's is gated on the object firing a trap, so most of its objects are clickable
    /// from anywhere; a tunnel's is not gated on anything. Routing through the building's method
    /// would mean passing a hard-coded <c>true</c> for a flag this routine never reads, which reads
    /// as a claim about the object rather than about the handler.</para>
    ///
    /// <para>And it is the opposite of <see cref="TraversalClick.HasNoReachGuard"/>: the ladder has
    /// no test at all. Copying either rule onto the other kind gets it wrong in one direction or
    /// the other — unreachable tunnels, or ladders that can be worked from across the zone.</para>
    /// </remarks>
    public static bool IsWithinReach(int objectTileX, int objectTileY, int partyTileX,
        int partyTileY) =>
        objectTileX == partyTileX && objectTileY == partyTileY;

    /// <summary>What the click does, given what the object carries.</summary>
    public enum Outcome {
        /// <summary>Outside the party's map tile — no sound, no message, nothing.</summary>
        OutOfReach,

        /// <summary>A secondary click: <see cref="DescribeDialog"/>.</summary>
        Describe,

        /// <summary>Dispatch the zone hotspot at the object's own key.</summary>
        DispatchHotspot,

        /// <summary>No hotspot, but a message to play.</summary>
        PlayMessage,

        /// <summary>Neither: <see cref="NothingToDoDialog"/>.</summary>
        NothingToDo,
    }

    /// <summary>
    /// Which of the five the click takes.
    /// </summary>
    /// <remarks>
    /// <b>The hotspot wins over the message when both are present.</b> The original's control flow
    /// says so plainly: the message is only consulted to decide whether there is anything at all to
    /// do, and the dispatch block is entered first regardless. An object carrying both would
    /// otherwise play its line and stay put.
    /// </remarks>
    public static Outcome For(bool isPrimary, bool inReach, bool hasFixedObject, bool hasHotspot,
        long interactDialogId) {
        if (!inReach) {
            return Outcome.OutOfReach;
        }
        if (!isPrimary) {
            return Outcome.Describe;
        }
        if (!hasFixedObject) {
            return Outcome.NothingToDo;
        }
        if (hasHotspot) {
            return Outcome.DispatchHotspot;
        }

        return interactDialogId != 0 ? Outcome.PlayMessage : Outcome.NothingToDo;
    }
}
