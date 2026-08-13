namespace GameData.Resources.Character;

using System;

/// <summary>
/// What happens when you drop something on a lock — <c>picklock_screen_handle_drop</c>
/// (<c>SRC/SCREENS/PICKLOCK.C</c>).
///
/// <para>There are <b>two entirely different mechanics</b> behind the one screen, and they share
/// almost nothing: lockpicks succeed on a skill comparison with no roll at all, while a key either
/// fits exactly or risks snapping. <see cref="LockPicking.DifficultyTier"/> is only ever the
/// figure the UI shows; it decides nothing here.</para>
/// </summary>
public static class PicklockAttempt {
    /// <summary>Object id of a lockpick, and the item kind that means "picks" rather than a key.</summary>
    public const int LockpickKind = 0;

    /// <summary>
    /// Global flag base recording that a lock was opened with a given key kind
    /// (<c>LOCK_PICKED_WITH</c>): flag <c>7260 + kind</c>.
    /// </summary>
    public const int PickedWithFlagBase = 7260;

    /// <summary>
    /// Above this, a lock cannot be opened with picks at all — no matter the skill — and only its
    /// exact key will do.
    /// </summary>
    public const int MaxPickableScore = 100;

    /// <summary>Skill awarded for opening a lock with picks.</summary>
    public const int SkillOnSuccess = 2;

    /// <summary>Skill awarded on a failed pick, when the consolation roll lands.</summary>
    public const int SkillOnFailure = 1;

    /// <summary>Chance in 100 that a failed pick still teaches something.</summary>
    public const int FailureLearnChance = 40;

    /// <summary>What an attempt did.</summary>
    public enum AttemptResult {
        /// <summary>The lock is open.</summary>
        Opened,

        /// <summary>Nothing happened; the lock holds.</summary>
        Failed,

        /// <summary>The tool snapped and is gone from the party's stock.</summary>
        ToolBroke,
    }

    /// <summary>
    /// Picking a lock with lockpicks.
    ///
    /// <para><b>There is no roll.</b> It opens if the lock's score is at most
    /// <see cref="MaxPickableScore"/> <i>and</i> strictly below the picker's LockPicking — so the
    /// same character either can or cannot open a given lock, every time. Treating this as a
    /// percentage chance would make locks feel random when they are not.</para>
    /// </summary>
    /// <param name="rnd">Returns a value in [0, 100); consulted only on failure.</param>
    /// <param name="skillAwarded">
    /// LockPicking to award. Two on success; on failure, one when the consolation roll lands — you
    /// learn a little from a lock you could not open.
    /// </param>
    public static AttemptResult WithLockpicks(int lockScore, int skill, Func<int, int> rnd,
        out int skillAwarded) {
        if (lockScore <= MaxPickableScore && lockScore < skill) {
            skillAwarded = SkillOnSuccess;
            return AttemptResult.Opened;
        }

        skillAwarded = rnd != null && rnd(100) <= FailureLearnChance ? SkillOnFailure : 0;
        // A pick snaps more readily the further the lock is beyond you; at or below your skill it
        // cannot break at all, since the threshold goes negative.
        int breakThreshold = (lockScore - skill) * 2 / 3;
        return rnd != null && rnd(100) <= breakThreshold
            ? AttemptResult.ToolBroke
            : AttemptResult.Failed;
    }

    /// <summary>
    /// Trying a key.
    /// </summary>
    /// <param name="keyValue">
    /// The key's own value from the key table. <b>It must equal the lock's score exactly</b> —
    /// there is no "close enough", and a more valuable key is not a better key, only a different
    /// one.
    /// </param>
    /// <param name="skill">The picker's LockPicking, which only affects the breakage odds.</param>
    /// <param name="rnd">Returns a value in [0, 100); consulted only when the key does not fit.</param>
    public static AttemptResult WithKey(int keyValue, int lockScore, int skill, Func<int, int> rnd) {
        if (keyValue == lockScore) {
            return AttemptResult.Opened;
        }
        return rnd != null && rnd(100) <= KeyBreakThreshold(keyValue, skill)
            ? AttemptResult.ToolBroke
            : AttemptResult.Failed;
    }

    /// <summary>
    /// Chance in 100 that a wrong key snaps: <c>(100 - keyValue - skill/3) * 2 / 3</c>.
    ///
    /// <para>So a <b>more valuable key is safer</b> to try, and a skilled picker breaks fewer keys —
    /// the lock's own difficulty does not enter into it at all.</para>
    /// </summary>
    public static int KeyBreakThreshold(int keyValue, int skill) =>
        (100 - keyValue - (skill / 3)) * 2 / 3;

    /// <summary>The flag recording that this key kind has opened its lock.</summary>
    public static int PickedWithFlag(int itemKind) => PickedWithFlagBase + itemKind;
}
