namespace GameData.Resources.Character;

/// <summary>
/// The shared lock-picking rules — <c>picklock_screen_run</c> (<c>SRC/SCREENS/PICKLOCK.C</c>), which
/// doors, chests, ladders and locked NPCs all route through.
/// </summary>
public static class LockPicking {
    /// <summary>Object id of a lockpick.</summary>
    public const int LockpickObjectId = 0x50;

    /// <summary>
    /// Which lock is being picked, passed to the prompt dialog as an event argument.
    ///
    /// <para><b>It is not a difficulty.</b> The original hands it straight to the DDX as
    /// <c>nEvtArgCount</c>, so it only selects which wording the prompt uses — the actual challenge
    /// comes from the score. Naming it "mode" invites reading it as a tier.</para>
    /// </summary>
    public enum LockContext {
        /// <summary>A locked NPC (<c>wcursor</c> @607, which also passes the owning actor).</summary>
        Person = 0,

        /// <summary>A door (<c>wcursor_object_toggle_open_close</c>).</summary>
        Door = 1,

        /// <summary>A container (<c>wcursor</c> @332).</summary>
        Container = 2,

        /// <summary>A ladder or other fixed traversal object (<c>wcursor_click_fixedobj_picklock</c>).</summary>
        Traversal = 3,
    }

    /// <summary>Score above which a lock is at the hardest tier.</summary>
    public const int TierFourAbove = 0x64;

    /// <summary>Score above which a lock is at the third tier.</summary>
    public const int TierThreeAbove = 0x50;

    /// <summary>Score above which a lock is at the second tier.</summary>
    public const int TierTwoAbove = 0x32;

    /// <summary>
    /// The difficulty tier a lock's score falls into, 1 (easiest) to 4.
    ///
    /// <para>The bands are <b>open at the bottom</b>: a score of exactly 80 is tier 2, not 3,
    /// because each test is a strict "greater than". So the thresholds are the first score of the
    /// tier above, not the last of their own.</para>
    /// </summary>
    public static int DifficultyTier(int score) =>
        score > TierFourAbove ? 4
        : score > TierThreeAbove ? 3
        : score > TierTwoAbove ? 2
        : 1;

    /// <summary>
    /// Whether an attempt can even be made.
    ///
    /// <para><b>Any item at all is enough to open the screen</b> — the original builds a working
    /// inventory from the party's shared stock, appends the lockpicks if there are any, and only
    /// refuses when that comes to nothing. So a party with no picks but other shared items still
    /// gets the screen and simply cannot succeed at it.</para>
    /// </summary>
    /// <param name="sharedItemCount">Items in the party's shared inventory.</param>
    /// <param name="lockpickCount">Lockpicks held across the party.</param>
    public static bool CanAttempt(int sharedItemCount, int lockpickCount) =>
        sharedItemCount + (lockpickCount > 0 ? 1 : 0) > 0;

    /// <summary>
    /// The attribute the picker is chosen by: the party's <b>best</b> LockPicking, not the leader
    /// and not the character whose screen is open.
    /// </summary>
    public const ActorAttribute PickerAttribute = ActorAttribute.LockPicking;
}
