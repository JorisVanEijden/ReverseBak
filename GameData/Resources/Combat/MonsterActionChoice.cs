namespace GameData.Resources.Combat;

using System;

/// <summary>
/// How a monster picks between closing to melee, casting and shooting —
/// <c>combataiact_pick_melee_or_missl</c> (canassa CBTAIACT.C:23).
///
/// <para>One of the action routines behind the AI's discipline cascade
/// (<see cref="CombatAi.ChooseAction"/> decides WHICH discipline; this decides what the
/// melee-or-missile routine actually does with it).</para>
/// </summary>
public static class MonsterActionChoice {
    /// <summary>What the monster does this turn.</summary>
    public enum Action {
        /// <summary>Swing — the target is adjacent.</summary>
        Melee,

        /// <summary>Cast at the target.</summary>
        Cast,

        /// <summary>Shoot the target.</summary>
        Ranged,
    }

    /// <summary>Adjacent means melee; there is no roll.</summary>
    public const int MeleeDistance = 1;

    /// <summary>The die the ranged/cast choice is rolled on — <c>RND(10)</c>.</summary>
    public const int ChoiceDie = 10;

    /// <summary>The spell targeting type this routine casts with.</summary>
    public const int CastTargetingType = 4;

    /// <summary>The quarrel type this routine shoots with.</summary>
    public const int QuarrelType = 8;

    /// <summary>
    /// Delay range applied to the melee swing — <c>RNDR(0x19, 0x31)</c>, i.e. 25..49.
    /// </summary>
    public static readonly (int Min, int Max) MeleeDelayRange = (0x19, 0x31);

    /// <summary>
    /// Choose the action for a monster whose nearest opponent is <paramref name="distance"/> away.
    /// </summary>
    /// <param name="distance">Chebyshev distance to the nearest living opponent.</param>
    /// <param name="roll">A value in [0, <see cref="ChoiceDie"/>) — the original's <c>RND(10)</c>.</param>
    /// <remarks>
    /// <b>Beyond melee range, the CLOSER the target the more likely a spell.</b> The test is
    /// <c>RND(10) &gt;= distance</c>, so a target two tiles away is cast at eight times in ten while
    /// one nine tiles away is cast at only once in ten — and <b>at ten tiles or more the monster can
    /// never cast</b>, because a d10 roll cannot reach 10. That is the opposite of the intuition that
    /// spells are the long-range option, and a port that inverts the comparison would have monsters
    /// sniping spells across the arena and meleeing nothing.
    /// </remarks>
    public static Action Choose(int distance, int roll) {
        if (distance <= MeleeDistance) {
            return Action.Melee;
        }
        return roll >= distance ? Action.Cast : Action.Ranged;
    }

    /// <summary>
    /// The chance in ten that this routine casts rather than shoots, at a given distance.
    /// </summary>
    /// <remarks>Zero at <see cref="ChoiceDie"/> tiles or beyond, and undefined inside melee range.</remarks>
    public static int CastChanceInTen(int distance) {
        if (distance <= MeleeDistance) {
            return 0;
        }
        return Math.Max(0, ChoiceDie - distance);
    }
}
