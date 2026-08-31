namespace GameData.Resources.World;

/// <summary>
/// Clicking a building, gate or other fixed object in the world —
/// <c>wcursor_click_fixedobj_full</c> (WCURSOR.C:259).
/// </summary>
/// <remarks>
/// <b>One click, six ways out.</b> The object can be out of reach, refuse the moment, hand off to a
/// hotspot event, open a lock, open its own inventory, or take the party into a town scene — and
/// which it does is decided by three flag bits, a lock key and a world event, not by what the
/// object looks like.
/// </remarks>
public static class FixedObjectClick {
    /// <summary>What a click on a fixed object ends up doing.</summary>
    public enum Outcome {
        /// <summary>Nothing at all — not even a sound. See <see cref="IsWithinReach"/>.</summary>
        Ignored,

        /// <summary>"Nothing happens" — the object has no interaction to offer.</summary>
        NothingToDo,

        /// <summary>Refused for now, with its own dialog.</summary>
        Refused,

        /// <summary>A hotspot event was dispatched and took the click.</summary>
        HotspotHandled,

        /// <summary>Its lock was picked; a lock never leads anywhere but a container.</summary>
        Locked,

        /// <summary>The object's own inventory opened.</summary>
        OpensInventory,

        /// <summary>The party entered the town scene the object leads to.</summary>
        EntersTownScene,
    }

    /// <summary>Dialog for an object with nothing to offer.</summary>
    public const int NothingToDoDialog = 0x9a;

    /// <summary>
    /// Dialog for a SECONDARY click — the describe answer.
    /// </summary>
    /// <remarks>
    /// <b>Established 2026-08-19: the gate it sits behind is the BUTTON, not a mode.</b> The
    /// original tests <c>menupage_state_0e7c() != 1</c>, and that state is
    /// <see cref="Menu.MenuClickButton"/> — which button the poll found down. So this is not "not
    /// just now"; it is what a right-click on a building says, with the object's own kind published
    /// for the message to name.
    /// </remarks>
    public const int DescribeDialog = 0x60;

    /// <summary>The sound a click on a fixed object makes.</summary>
    public const int ClickSound = 0x30;

    /// <summary>The world event the entry gate reads.</summary>
    public const int EntryEventKey = 0x753a;

    /// <summary>The hotspot event kind a fixed object dispatches.</summary>
    public const int HotspotEventKind = 7;

    // ---- the three flag bits on the interact-message subrecord --------------------------------

    /// <summary>Bit 0 — the object is gated on <see cref="EntryEventKey"/>.</summary>
    /// <remarks>
    /// <b>Set means GATED, clear means open.</b> The original tests <c>!(flags &amp; 1)</c> and
    /// calls the result "flag 1 clear", so the polarity reads backwards from the bit's name.
    /// </remarks>
    public const int GatedOnEventFlag = 1;

    /// <summary>Bit 1 — the object opens its own inventory once its message has played.</summary>
    public const int OpensInventoryFlag = 2;

    /// <summary>
    /// Bit 5 — the message plays BEFORE the hotspot dispatch rather than after.
    /// </summary>
    /// <remarks>
    /// <b>And a refusal there cancels the rest of the click.</b> The early message is the only one
    /// whose return value is acted on: a negative answer drops the hotspot and stops everything
    /// after it. So this bit changes both when the line is heard and whether the player can decline.
    /// </remarks>
    public const int MessageFirstFlag = 0x20;

    /// <summary>
    /// Whether the object is close enough to click at all.
    /// </summary>
    /// <remarks>
    /// <b>A TRAPPED OBJECT IS ONLY CLICKABLE FROM THE PARTY'S OWN TILE, AND SILENTLY SO.</b>
    /// The original returns before the sound and before any dialog, so clicking one from the next
    /// tile along produces nothing whatsoever — no message, no click. A port that answers "you are
    /// too far away" is being more helpful than the original and changes what the player learns
    /// from the silence.
    ///
    /// <para><b>*** THE TEST IS THE FLAG, NOT THE PRESENCE OF THE RECORD. ***</b> This parameter
    /// used to be <c>hasHotspot</c> and callers passed "the encounter subrecord exists", which
    /// wrongly pinned 28 of the 96 encounter-bearing containers to the party's own tile. The
    /// original is
    /// <c>pSub8 != 0 &amp;&amp; pSub8-&gt;hotspot_action.bHas_hotspot != 0</c> (WCURSOR.C:285) — the
    /// subrecord AND a non-zero byte inside it, which IDA names
    /// <c>containerData_encounter.firesTrapEncounter</c> and <c>handle_Grave</c> @0x77d5b tests the
    /// same way. canassa's <c>bHas_hotspot</c> is the misleading half: it reads as "there is a
    /// hotspot" when it is a field within one.</para>
    ///
    /// <para>An object that fires no trap has no such restriction.</para>
    /// </remarks>
    public static bool IsWithinReach(bool firesTrapEncounter, int objectTileX, int objectTileY,
        int partyTileX, int partyTileY) =>
        !firesTrapEncounter || (objectTileX == partyTileX && objectTileY == partyTileY);

    /// <summary>
    /// Whether the entry gate lets the click through.
    /// </summary>
    /// <remarks>
    /// Ungated objects always pass; a gated one needs the world event set.
    /// </remarks>
    public static bool GatePasses(int flags, int eventValue) =>
        (flags & GatedOnEventFlag) == 0 || eventValue != 0;

    /// <summary>
    /// The dialog argument the gate publishes, which the message can branch on.
    /// </summary>
    /// <remarks>
    /// <b>Zero when the gate passes and one when it does not</b> — set either way, and before the
    /// message is played, so a gated object can say something different while it is still shut.
    /// </remarks>
    public static int GateArgument(int flags, int eventValue) =>
        GatePasses(flags, eventValue) ? 0 : 1;

    /// <summary>
    /// <b>A LOCKED OBJECT NEVER LEADS ANYWHERE.</b>
    /// </summary>
    /// <remarks>
    /// The warp into a town scene sits only on the unlocked branch, so an object carrying a lock
    /// key is a container whatever else it has — picking the lock opens its inventory at most. A
    /// port that runs the lock and then falls through to the warp turns every locked chest into a
    /// door.
    /// </remarks>
    public static bool CanEnterTownScene(int lockKey) => lockKey == 0;

    /// <summary>
    /// <b>The warp's two bytes are unpacked by <c>GdsSceneRules.UnpackScene</c>, not here.</b>
    /// </summary>
    /// <remarks>
    /// A kind carrying its destination in the high byte overrides the destination it was handed —
    /// and that is the same rule <c>GDS_RunScene</c> applies to every scene reference, already
    /// owned by the GDS layer. Restating it here would give the game two copies of one packing
    /// convention, free to drift.
    /// </remarks>
    public static bool WarpIsUnpackedByTheGdsLayer => true;

    /// <summary>
    /// What the click does, once it is in reach and the moment is right.
    /// </summary>
    /// <param name="lockKey">The object's lock, or 0 for none.</param>
    /// <param name="hasMessage">Whether it carries an interact message at all.</param>
    /// <param name="hasWarp">Whether its hotspot subrecord names a town scene.</param>
    public static Outcome Resolve(int lockKey, bool hasMessage, bool hasWarp, int flags,
        int eventValue) {
        if (!hasMessage) {
            // No line to say is the same as nothing to do, locked or not.
            return Outcome.NothingToDo;
        }

        if (!CanEnterTownScene(lockKey)) {
            return GatePasses(flags, eventValue) && (flags & OpensInventoryFlag) != 0
                ? Outcome.OpensInventory
                : Outcome.Locked;
        }

        if (!GatePasses(flags, eventValue)) {
            // The message still plays; the click simply stops after it.
            return Outcome.Refused;
        }

        if (hasWarp) {
            return Outcome.EntersTownScene;
        }

        return (flags & OpensInventoryFlag) != 0 ? Outcome.OpensInventory : Outcome.NothingToDo;
    }
}
