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

    // ---- the lock graphic (UI_DrawLock @0x5bca0) ------------------------------------------------

    /// <summary>The image set the lock is drawn from.</summary>
    public const string LockIconSet = "INVLOCK.BMX";

    /// <summary>The latch, drawn over the lock body and animated when it opens.</summary>
    public const int LatchImageIndex = 0;

    /// <summary>
    /// The lock body's image index, 1..4, chosen by the lock's difficulty.
    /// </summary>
    /// <remarks>
    /// <b>The same thresholds <see cref="LockPicking.DifficultyTier"/> already carries</b> — this
    /// is what that tier is FOR, which its own doc could only say was "the figure the UI shows".
    /// A harder lock is drawn as a heavier lock.
    /// </remarks>
    public static int LockImageIndexFor(int lockDifficulty) =>
        LockPicking.DifficultyTier(lockDifficulty);

    /// <summary>Where the lock is drawn, in VGA px: the narrow panel's own box.</summary>
    /// <remarks>
    /// (13, 11, 82, 121) — byte-identical to the narrow background <c>UI_DrawInventory</c> draws
    /// when the mode flag is set, which is why lock mode needs
    /// <see cref="Inventory.InventoryPanelMode.ShopMode.On"/>: the lock sits IN that box.
    /// </remarks>
    public const int PanelVgaX = 13;

    /// <inheritdoc cref="PanelVgaX"/>
    public const int PanelVgaY = 11;

    /// <inheritdoc cref="PanelVgaX"/>
    public const int PanelVgaWidth = 82;

    /// <inheritdoc cref="PanelVgaX"/>
    public const int PanelVgaHeight = 121;

    /// <summary>The latch's position in VGA px, and it is FIXED.</summary>
    /// <remarks>
    /// Drawn BEFORE the body, so the body overlaps it — a padlock hanging from a hasp. Drawing the
    /// latch last puts it in front of the lock, which looks like a separate object lying on top.
    /// (The open animation walks this y upward; the closed state is the resting position here.)
    /// </remarks>
    public const int LatchVgaX = 29;

    /// <inheritdoc cref="LatchVgaX"/>
    public const int LatchVgaY = 34;

    /// <summary>The body's baseline in VGA px.</summary>
    public const int BodyVgaY = 63;

    /// <summary>
    /// The body's left edge in VGA px — <b>centred in the panel, not placed at its origin</b>.
    /// </summary>
    /// <remarks>
    /// <c>13 + (82 - bodyWidth) / 2</c> (0x5bd10). The four difficulty images are different widths,
    /// so a fixed x would step the lock sideways as the difficulty changed. Placing it at the panel
    /// origin — the obvious guess, since that is where the box is drawn — puts it in the corner.
    /// </remarks>
    public static int BodyVgaX(int bodyWidthVga) =>
        PanelVgaX + ((PanelVgaWidth - bodyWidthVga) / 2);

    /// <summary>
    /// The latch's y offsets as the lock swings open, in VGA px — the whole animation.
    /// </summary>
    /// <remarks>
    /// <c>UI_DrawLock(bIsOpen=1)</c> walks a counter 0..24 in steps of two and draws the latch at
    /// <c>34 - offset</c>, clearing and redrawing the panel each pass. <b>The last pass is not 24</b>:
    /// at 24 the original subtracts two again (0x5bcca), so the latch settles at 22 and the final
    /// frame is a repeat. Ending at 24 lifts the latch two pixels too far and the animation stops
    /// on a frame the player never saw in 1993.
    ///
    /// <para>Offsets, not positions, because they are what the original computes; subtract from
    /// <see cref="LatchVgaY"/> to place the sprite.</para>
    /// </remarks>
    public static int[] OpeningLatchOffsets() {
        var frames = new int[13];
        for (var i = 0; i < frames.Length; i++) {
            frames[i] = i * 2;
        }
        frames[frames.Length - 1] = frames[frames.Length - 2];

        return frames;
    }

    /// <summary>
    /// The item flags the appended pick stack carries.
    /// </summary>
    /// <remarks>
    /// Zero — explicitly cleared. The stack is synthesized, so it inherits no condition, no
    /// equipped bit and no modifiers from whichever member's picks it stands for.
    /// </remarks>
    public const int PickStackItemFlags = 0;
}
