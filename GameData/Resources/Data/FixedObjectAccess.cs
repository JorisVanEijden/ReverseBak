namespace GameData.Resources.Data;

/// <summary>
/// Reading the per-placement properties a fixed world object carries, under the names the
/// <i>engine</i> uses for them.
///
/// <para>The container record's optional blocks are the actor subrecords, one for one — the bits
/// line up exactly (<c>SaveGameContainerDataType</c> already records this):</para>
/// <list type="table">
/// <item><term>0x01 Lock</term><description><c>SUBREC_PARAMS</c>, 4 bytes</description></item>
/// <item><term>0x02 Dialog</term><description><c>SUBREC_INTERACT_MSG</c>, 6 bytes</description></item>
/// <item><term>0x04 Shop</term><description><c>SUBREC_EVENT_STATE</c>, 16 bytes</description></item>
/// <item><term>0x08 Encounter</term><description><c>SUBREC_HOTSPOT_ACTION</c>, 9 bytes</description></item>
/// <item><term>0x10 Timestamp</term><description><c>SUBREC_LAST_TOUCH</c>, 4 bytes</description></item>
/// <item><term>0x20 GlobalState</term><description><c>SUBREC_DOOR_VARIANT</c>, 2 bytes</description></item>
/// </list>
///
/// <para><b>The 4-byte params block is a union with several readings</b>, which is why our field
/// names for it are the chest ones and the door and ladder handlers appear to read something else.
/// Byte for byte: 0 is the proximity flags, 1 is the door/NPC lookup key <i>and</i> what the door
/// handler reads as its lock, 2 is the cipher-puzzle id, 3 is unused in those views. So
/// <see cref="SaveGameContainerLockData.Difficulty"/> is not only a chest's difficulty — it is the
/// same byte a door and a ladder take their lock from.</para>
/// </summary>
public static class FixedObjectAccess {
    /// <summary>
    /// The door's identity, from the <c>SUBREC_DOOR_VARIANT</c> block — our
    /// <c>GlobalStateIndex</c>, which is that subrecord and not a global-state key despite the name.
    /// Its open/shut state lives in global flag <c>7000 + variant</c>.
    /// </summary>
    public static int? DoorVariant(SaveGameContainerData? container) =>
        container?.GlobalStateIndex;

    /// <summary>
    /// The lock strength guarding this object, or 0 for unlocked.
    ///
    /// <para>Byte 1 of the params block, which doors read as <c>interact_msg.bFlags</c> and ladders
    /// as <c>door_or_npc_key.bLookup_key</c> — the same byte under two names. Feed it to
    /// <c>LockPicking.DifficultyTier</c>.</para>
    /// </summary>
    public static int LockValue(SaveGameContainerData? container) =>
        container?.LockData?.Difficulty ?? 0;

    /// <summary>Whether anything guards this object.</summary>
    public static bool IsLocked(SaveGameContainerData? container) => LockValue(container) != 0;

    /// <summary>
    /// The dialog played when the object is used — <c>SUBREC_INTERACT_MSG</c>'s message id.
    ///
    /// <para>For a ladder or tunnel this is <b>where the traversal lives</b>: the handler runs the
    /// lock check and then plays this, and the dialog's own Teleport action moves the party.</para>
    ///
    /// <para>This is the raw field. A <i>container</i> interaction (corpse, bag, well) should not
    /// use it directly — <see cref="InteractionDialogResolver"/> owns the choice between a
    /// profile's dialog and the container's own for that family, and doors and ladders have no
    /// interaction profile at all.</para>
    /// </summary>
    public static long? InteractDialogId(SaveGameContainerData? container) =>
        container?.DialogData?.DialogId;

    /// <summary>The describe (right-click) message index from the same block.</summary>
    public static int? ExamineMessageIndex(SaveGameContainerData? container) =>
        container?.DialogData?.ExamineMessageIndex;
}
