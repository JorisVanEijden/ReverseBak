namespace GameData.Resources.Character;

/// <summary>
/// What the picklock screen actually shows you — <c>sub_ovr166_DF</c> @0x5bdf9.
/// </summary>
/// <remarks>
/// <b>The screen is the ordinary inventory screen over a SCRATCH container, not over a real one.</b>
/// The original copies the party's shared inventory into a temporary container and appends the
/// party's lockpicks to it, so what you drag onto the lock is a working set assembled for the
/// occasion. Nothing you see there is the container it came from.
/// </remarks>
public static class PicklockWorkingSet {
    /// <summary>"You need keys or picklocks" — shown when the working set comes out empty.</summary>
    public const int NothingToTryDialog = 86;

    /// <summary>
    /// <b>The picks are ONE stack, not the individual pack entries.</b>
    /// </summary>
    /// <remarks>
    /// The original calls <c>CountItemInWholeParty(Picklocks)</c> and appends a SINGLE item whose
    /// quantity is that whole-party total — it does not copy each member's picks across. So the
    /// screen shows "picklocks x7" once, however many members are carrying them.
    ///
    /// <para>This matters for the write-back and is easy to get wrong in both directions: the
    /// displayed stack has no owning member, so a pick that snaps cannot simply be removed from
    /// "the container it was in" — there is no such container. Equally, copying the real per-member
    /// entries instead would show several pick stacks where the original shows one.</para>
    /// </remarks>
    public static int PickStackQuantity(int partyWideLockpickCount) => partyWideLockpickCount;

    /// <summary>Whether the party's picks contribute a stack at all.</summary>
    /// <remarks>Strictly positive — a count of zero appends nothing rather than an empty stack.</remarks>
    public static bool HasPickStack(int partyWideLockpickCount) => partyWideLockpickCount > 0;

    /// <summary>
    /// How many entries the assembled working set holds.
    /// </summary>
    /// <param name="sharedItemCount">Items already in the party's shared inventory (the keys).</param>
    /// <param name="partyWideLockpickCount">Lockpicks held anywhere in the party.</param>
    /// <remarks>
    /// The picks add AT MOST ONE entry regardless of how many there are, which is what makes this
    /// a count of entries rather than of items. <see cref="LockPicking.CanAttempt"/> is the same
    /// arithmetic asking whether the screen opens at all.
    /// </remarks>
    public static int EntryCount(int sharedItemCount, int partyWideLockpickCount) =>
        sharedItemCount + (HasPickStack(partyWideLockpickCount) ? 1 : 0);

    /// <summary>
    /// The item flags the appended pick stack carries.
    /// </summary>
    /// <remarks>
    /// Zero — explicitly cleared. The stack is synthesized, so it inherits no condition, no
    /// equipped bit and no modifiers from whichever member's picks it stands for.
    /// </remarks>
    public const int PickStackItemFlags = 0;
}
