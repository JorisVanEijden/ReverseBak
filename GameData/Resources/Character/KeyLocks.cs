namespace GameData.Resources.Character;

/// <summary>
/// The eleven named key-locks, and what the party remembers about them.
/// </summary>
/// <remarks>
/// <b>Opening one with its key is remembered forever.</b> A key success writes
/// <c>7260 + lockNumber</c> (0x5bf35) and that is a GLOBAL, so unlike a container's open state it
/// survives a save. Picking a lock does not write it — the flag is specifically about having met
/// this kind of lock and learned which key it takes.
///
/// <para>The only thing that reads it is the lock-examine action (<c>sub_ovr166_417</c> @0x5c0b7):
/// a lock you have opened before is named, along with how many of its key the party is carrying,
/// instead of being assessed against the party's lockpicking.</para>
/// </remarks>
public static class KeyLocks {
    /// <summary>
    /// Each named lock's difficulty, indexed by lock number. Index 0 is the no-match slot.
    /// </summary>
    /// <remarks>
    /// From <c>locks_byte_dseg_12C5</c>. Lock <i>n</i> is opened by object <c>60 + n</c>, so this
    /// runs from the Peasant's Key at 61 to the Royal Key of Krondor at 71. The six entries over
    /// 100 are the key-only locks — no lockpicking skill can reach them.
    /// </remarks>
    public static readonly int[] Difficulties = { 0, 50, 90, 101, 102, 103, 104, 70, 60, 80, 105, 106 };

    /// <summary>The global that remembers lock <paramref name="lockNumber"/> has been opened.</summary>
    public const int OpenedGlobalBase = 7260;

    /// <summary>Above this a lock cannot be picked at all, only unlocked with its key.</summary>
    public const int KeyOnlyDifficulty = 100;

    /// <summary>The hardest named lock. Above it, nothing the party can carry will help.</summary>
    public const int HardestNamedLock = 106;

    /// <summary>What the examine message says.</summary>
    public enum Assessment {
        /// <summary>The party's best lockpicking is up to this lock.</summary>
        WithinSkill,

        /// <summary>Too hard for the party's lockpicking, but still a picking job.</summary>
        BeyondSkill,

        /// <summary>Over <see cref="KeyOnlyDifficulty"/> — it wants its key.</summary>
        KeyOnly,

        /// <summary>Harder than any lock a named key opens.</summary>
        BeyondEveryKey,
    }

    /// <summary>
    /// Which named lock has this difficulty, or 0 for none.
    /// </summary>
    /// <remarks>
    /// <b>The LAST match wins, not the first</b> — the original scans all eleven without breaking
    /// out. No two shipped entries share a difficulty so nothing turns on it today, but a mod that
    /// duplicated one would pick the later lock, and reading "first match" out of this would be
    /// wrong the moment it mattered.
    /// </remarks>
    public static int NumberFor(int difficulty) {
        var found = 0;
        for (var i = 1; i < Difficulties.Length; i++) {
            if (Difficulties[i] == difficulty) {
                found = i;
            }
        }

        return found;
    }

    /// <summary>The global key that remembers lock <paramref name="lockNumber"/>.</summary>
    public static int OpenedGlobal(int lockNumber) => OpenedGlobalBase + lockNumber;

    /// <summary>
    /// How a lock the party does NOT recognise reads to them.
    /// </summary>
    /// <remarks>
    /// The order is the original's and the later tests overwrite the earlier ones, so difficulty
    /// decides the answer outright once it passes 100 — the party's skill stops being consulted
    /// rather than being compared and found wanting.
    /// </remarks>
    public static Assessment Assess(int difficulty, int bestLockPicking) {
        if (difficulty > HardestNamedLock) {
            return Assessment.BeyondEveryKey;
        }
        if (difficulty > KeyOnlyDifficulty) {
            return Assessment.KeyOnly;
        }

        return bestLockPicking < difficulty ? Assessment.BeyondSkill : Assessment.WithinSkill;
    }
}
