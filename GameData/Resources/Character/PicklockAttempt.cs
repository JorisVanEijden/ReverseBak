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
    /// The lock score each key kind opens — <c>g_abInvQuizAnswerTable</c> (PICKLOCK.C:29).
    /// </summary>
    /// <remarks>
    /// <b>A key's kind is an INDEX, not its value.</b> The eleven keys are object ids 61..71, so
    /// kind = <c>objectId - 60</c>, and this table turns that kind into the lock score it opens.
    /// The two are nothing like each other: kind 3 opens 101, kind 8 opens 60, kind 11 opens 106.
    ///
    /// <para>Comparing the kind directly against the lock score — which is what this class did until
    /// 2026-09-09 — lets a key open only a lock whose score happens to equal its own index, so of the
    /// eleven only kinds 1..11 could ever match anything and the interesting locks were unopenable.
    /// The palace ladder that ends chapter 1 scores <b>106</b>, which is kind 11's entry: under the
    /// old rule it wanted an object id of 166, and no such object ships.</para>
    ///
    /// <para>Index 0 and 12 are 0 — the table is 13 long and only 1..11 are keys.</para>
    /// </remarks>
    public static readonly int[] KeyLockScores =
        { 0, 0x32, 0x5a, 0x65, 0x66, 0x67, 0x68, 0x46, 0x3c, 0x50, 0x69, 0x6a, 0 };

    /// <summary>The lock score a key kind opens, or 0 for a kind that is not a key.</summary>
    public static int LockScoreForKeyKind(int keyKind) =>
        keyKind >= 0 && keyKind < KeyLockScores.Length ? KeyLockScores[keyKind] : 0;

    /// <summary>
    /// Trying a key.
    /// </summary>
    /// <param name="keyKind">
    /// The key's kind — <c>objectId - 60</c>, an index into <see cref="KeyLockScores"/>. The score
    /// it looks up <b>must equal the lock's exactly</b>: there is no "close enough", and a
    /// higher-scoring key is not a better key, only a different one.
    /// </param>
    /// <param name="skill">The picker's LockPicking, which only affects the breakage odds.</param>
    /// <param name="rnd">Returns a value in [0, 100); consulted only when the key does not fit.</param>
    public static AttemptResult WithKey(int keyKind, int lockScore, int skill, Func<int, int> rnd) {
        if (LockScoreForKeyKind(keyKind) == lockScore) {
            return AttemptResult.Opened;
        }
        return rnd != null && rnd(100) <= KeyBreakThreshold(keyKind, skill)
            ? AttemptResult.ToolBroke
            : AttemptResult.Failed;
    }

    /// <summary>
    /// Chance in 100 that a wrong key snaps:
    /// <c>(100 - KeyLockScores[kind] - skill/3) * 2 / 3</c>.
    ///
    /// <para>So a <b>higher-scoring key is safer</b> to try, and a skilled picker breaks fewer keys —
    /// the lock's own difficulty does not enter into it at all. The original reads the same table
    /// entry here that it compares with (PICKLOCK.C:130), so passing the kind straight in understates
    /// the threshold for every key.</para>
    /// </summary>
    public static int KeyBreakThreshold(int keyKind, int skill) =>
        (100 - LockScoreForKeyKind(keyKind) - (skill / 3)) * 2 / 3;

    /// <summary>The flag recording that this key kind has opened its lock.</summary>
    public static int PickedWithFlag(int itemKind) => PickedWithFlagBase + itemKind;
}
