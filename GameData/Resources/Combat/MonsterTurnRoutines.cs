namespace GameData.Resources.Combat;

/// <summary>What a bespoke creature routine decides to do with its turn.</summary>
public enum MonsterMove {
    /// <summary>Swing at the nearest enemy.</summary>
    Melee,

    /// <summary>Cast, at the kind named by <see cref="MonsterTurn.SpellKind"/>.</summary>
    Cast,

    /// <summary>Take a ranged shot.</summary>
    Shoot,

    /// <summary>Do nothing offensive and raise a guard.</summary>
    Defend,
}

/// <summary>One creature routine's decision for the turn.</summary>
public readonly struct MonsterTurn {
    public MonsterTurn(MonsterMove move, int spellKind = 0, bool braces = false) {
        Move = move;
        SpellKind = spellKind;
        Braces = braces;
    }

    public MonsterMove Move { get; }

    /// <summary>The spell kind chosen, when <see cref="Move"/> is <see cref="MonsterMove.Cast"/>.</summary>
    public int SpellKind { get; }

    /// <summary>
    /// Set alongside <see cref="MonsterMove.Defend"/>: the creature also flips a stance flag, so a
    /// defending turn comes in two grades rather than one.
    /// </summary>
    public bool Braces { get; }
}

/// <summary>
/// The per-creature turn routines that sit in front of the generic capability cascade — the
/// <c>SRC/COMBAT/AI/</c> handlers a class id dispatches to. See <see cref="CombatAi"/> for the
/// dispatch itself and why a class with a routine here never reaches the cascade.
///
/// <para>Only the <b>decisions</b> are here. Walking the path, rolling the attack and resolving the
/// spell belong to whoever runs combat.</para>
/// </summary>
public static class MonsterTurnRoutines {
    /// <summary>Tile distance at which a creature swings instead of doing anything else.</summary>
    public const int MeleeReach = 1;

    /// <summary>Damage a routine's melee swing rolls, inclusive at both ends.</summary>
    public const int MeleeMinDamage = 0x19;

    /// <summary>Damage a routine's melee swing rolls, inclusive at both ends.</summary>
    public const int MeleeMaxDamage = 0x31;

    /// <summary>Exclusive bound of the roll that decides cast-versus-shoot at range.</summary>
    public const int CloseRangeCastRollBound = 10;

    /// <summary>The spell kind used when a routine has only one.</summary>
    public const int DefaultSpellKind = 4;

    /// <summary>The stronger spell kind, taken on the lower half of the roll.</summary>
    public const int AlternateSpellKind = 5;

    /// <summary>Below this roll the wandering routine casts <see cref="AlternateSpellKind"/>.</summary>
    public const int AlternateSpellRoll = 0x32;

    /// <summary>At or above this roll the wandering routine gives up on casting.</summary>
    public const int CastGiveUpRoll = 0x50;

    /// <summary>
    /// Melee if you can reach, otherwise cast or shoot — the simpler of the two routines.
    /// </summary>
    /// <param name="distanceToNearest">Tiles to the nearest enemy.</param>
    /// <param name="castRoll">A roll in <c>[0, 10)</c>.</param>
    /// <remarks>
    /// <b>The test is <c>roll &gt;= distance</c>, so this creature casts MORE the closer it is</b>,
    /// and past ten tiles it never casts at all. That is the opposite of the intuition that a caster
    /// opens at range and closes to melee, and it is easy to "fix" into a bug: swap the comparison
    /// and the creature only ever casts from far away, where the original always shoots.
    /// </remarks>
    public static MonsterTurn CloseOrRanged(int distanceToNearest, int castRoll) {
        if (distanceToNearest <= MeleeReach) {
            return new MonsterTurn(MonsterMove.Melee);
        }

        return castRoll >= distanceToNearest
            ? new MonsterTurn(MonsterMove.Cast, DefaultSpellKind)
            : new MonsterTurn(MonsterMove.Shoot);
    }

    /// <summary>
    /// The routine that wanders first: the creature walks to a <b>randomly chosen reachable
    /// tile</b> — not toward anything — and only then decides what to do from where it landed.
    /// </summary>
    /// <param name="distanceToNearest">Tiles to the nearest enemy, measured after the walk.</param>
    /// <param name="roll">A roll in <c>[0, 100)</c>, used for three decisions at once.</param>
    /// <param name="halfStat">Half the creature's first stat, its casting power.</param>
    /// <param name="lineOfFireClear">Whether a projectile path to the target exists.</param>
    /// <remarks>
    /// <b>One roll drives three outcomes</b>, so they are not independent: under 0x32 casts the
    /// alternate spell, 0x32 to 0x4F casts the default one, and 0x50 or over gives up and defends.
    /// Rolling separately for "do I cast" and "which spell" would change the distribution even with
    /// the same thresholds.
    ///
    /// <para><b>A creature whose half-stat is exactly 1 never casts</b> — the guard tests
    /// inequality, not a minimum — so it defends instead. Reading that as "needs at least 1" would
    /// let the weakest casters cast.</para>
    /// </remarks>
    public static MonsterTurn AfterWandering(int distanceToNearest, int roll, int halfStat,
        bool lineOfFireClear) {
        if (distanceToNearest <= MeleeReach) {
            return new MonsterTurn(MonsterMove.Melee);
        }

        bool casts = halfStat != 1
            && distanceToNearest >= 2
            && roll < CastGiveUpRoll
            && lineOfFireClear;

        if (casts) {
            return new MonsterTurn(MonsterMove.Cast,
                roll < AlternateSpellRoll ? AlternateSpellKind : DefaultSpellKind);
        }

        return new MonsterTurn(MonsterMove.Defend, braces: roll > AlternateSpellRoll);
    }

    /// <summary>
    /// Whether the wandering routine acts at all after its walk.
    /// </summary>
    /// <remarks>
    /// <b>This is a build difference, and we target the build that has it.</b> On the 1.02 CD
    /// release the whole post-move decision is wrapped in a check on the creature's second flag: a
    /// flagged creature walks and its turn ends there. The floppy build attacks regardless. Porting
    /// the floppy behaviour would give those creatures a free action every turn.
    /// </remarks>
    public static bool ActsAfterMoving(bool secondFlagSet) => !secondFlagSet;
}
