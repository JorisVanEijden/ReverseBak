namespace GameData.Resources.World;

/// <summary>
/// Clicking a ladder, tunnel or tunnel exit — <c>wcursor_click_fixedobj_picklock</c>
/// (WCURSOR.C:999), the handler for entity kinds 20, 39 and 42.
/// </summary>
/// <remarks>
/// <b>Much shorter than the building click, and it differs in every part that matters.</b> No reach
/// test, no flag bits, no world-event gate, no warp — and its own describe line. What it does have
/// is a lock, and the lock is not optional.
/// </remarks>
public static class TraversalClick {
    /// <summary>The sound the click makes, shared with the building click.</summary>
    public const int ClickSound = 0x30;

    /// <summary>Dialog when there is no fixed object there, or it has nothing to say.</summary>
    /// <remarks>The same "nothing happens" record the building click uses.</remarks>
    public const int NothingToDoDialog = 0x9a;

    /// <summary>
    /// Dialog for a SECONDARY click — and it is <b>not</b> the building's.
    /// </summary>
    /// <remarks>
    /// A building answers 0x60 and a ladder 0xae. Two fixed-object handlers, two describe lines, so
    /// a port that shares one "you look at it" record between them says the wrong thing on one of
    /// the two. The button test is <see cref="Menu.MenuClickButton"/>.
    /// </remarks>
    public const int DescribeDialog = 0xae;

    /// <summary>The mode the lock flow is entered in — <b>3</b>, where a building uses 2.</summary>
    /// <remarks>
    /// It is published as the dialog argument before the picklock prompt plays, so the prompt can
    /// say something different for a ladder than for a chest. Passing the building's 2 would give a
    /// ladder a chest's wording.
    /// </remarks>
    public const int LockMode = 3;

    /// <summary>
    /// <b>THE LOCK FLOW IS ENTERED WHATEVER THE LOCK VALUE — THERE IS NO UNLOCKED SHORTCUT.</b>
    /// </summary>
    /// <remarks>
    /// The building click tests <c>lookupKey != 0</c> before running the lock; this one does not.
    /// <c>picklock_screen_run</c> is called unconditionally, and it opens its prompt and its screen
    /// even for a key of zero — a zero simply lands in the lowest difficulty tier.
    ///
    /// <para>So a ladder cannot be walked through by giving it no lock, and a port cannot ship an
    /// "unlocked ladders work now" half: the traversal is downstream of a screen that always runs.
    /// </para>
    /// </remarks>
    public static bool LockFlowAlwaysRuns => true;

    /// <summary>
    /// <b>The traversal itself is in the DIALOG, not in the handler.</b>
    /// </summary>
    /// <remarks>
    /// On a successful lock the handler plays the object's interact message and stops. Moving the
    /// party is that message's own Teleport action — so a handler that tries to move anyone is
    /// doing the dialog's job, and will do it twice once the dialog runs.
    ///
    /// <para>With no message the click answers <see cref="NothingToDoDialog"/> instead, so a
    /// traversal object whose dialog is missing says "nothing happens" rather than silently
    /// failing to move the party.</para>
    /// </remarks>
    public static bool TraversalLivesInTheDialog => true;

    /// <summary>
    /// <b>There is no reach test at all.</b>
    /// </summary>
    /// <remarks>
    /// The building click refuses a hotspot-bearing object outside the party's tile
    /// (<see cref="FixedObjectClick.IsWithinReach"/>). This one has no such guard: whatever the
    /// pick returns is acted on. Copying the building's guard here would make distant ladders
    /// silently unclickable.
    /// </remarks>
    public static bool HasNoReachGuard => true;

    /// <summary>The dialog a click answers with, given what is there.</summary>
    /// <param name="isPrimary">Which button — see <see cref="Menu.MenuClickButton"/>.</param>
    /// <param name="hasFixedObject">Whether a fixed object with params resolves at the position.</param>
    /// <param name="lockOpened">Whether the lock flow succeeded.</param>
    /// <param name="interactDialogId">The object's interact message, or 0.</param>
    /// <returns>The dialog to play, or 0 for none.</returns>
    public static long DialogFor(bool isPrimary, bool hasFixedObject, bool lockOpened,
        long interactDialogId) {
        if (!isPrimary) {
            return DescribeDialog;
        }

        if (!hasFixedObject) {
            return NothingToDoDialog;
        }

        if (!lockOpened) {
            // The lock flow has already had its say; a refusal adds nothing.
            return 0;
        }

        return interactDialogId != 0 ? interactDialogId : NothingToDoDialog;
    }
}
