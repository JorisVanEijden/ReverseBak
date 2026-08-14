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

    /// <summary>Exclusive bound of the roll that can call off a shot the creature could take.</summary>
    public const int AbortShotRollBound = 100;

    /// <summary>Below this roll the ranged routine does not shoot at all.</summary>
    public const int AbortShotRoll = 0x32;

    /// <summary>Exclusive bound of the roll choosing the heavy shot over the light one.</summary>
    public const int HeavyShotRollBound = 4;

    /// <summary>At or below this roll the ranged routine takes its heavy shot.</summary>
    public const int HeavyShotRoll = 2;

    /// <summary>The creature that always takes the heavy shot, whatever the roll says.</summary>
    public const int AlwaysHeavyCreature = 0x39;

    /// <summary>Tiles beyond which the volley routine shoots rather than closing.</summary>
    public const int VolleyMinimumDistance = 2;

    /// <summary>What the ranged routine settles on.</summary>
    public enum RangedChoice {
        /// <summary>No shot this turn — hand the turn to the generic move/attack picker.</summary>
        Reconsider,

        /// <summary>The creature's own heavy attack, with its knockback.</summary>
        HeavyShot,

        /// <summary>The weak fallback shot every one of them shares.</summary>
        LightShot,
    }

    /// <summary>A ranged routine's decision, with the numbers that go with it.</summary>
    public readonly struct RangedTurn {
        public RangedTurn(RangedChoice choice, int actionId = 0, int knockbackFrames = 0,
            int minDamage = 0, int maxDamage = 0, bool scalesWithStat = false) {
            Choice = choice;
            ActionId = actionId;
            KnockbackFrames = knockbackFrames;
            MinDamage = minDamage;
            MaxDamage = maxDamage;
            ScalesWithStat = scalesWithStat;
        }

        public RangedChoice Choice { get; }

        /// <summary>Which projectile animation plays.</summary>
        public int ActionId { get; }

        /// <summary>Knockback frames pushed onto the target; also the damage call's knock argument.</summary>
        public int KnockbackFrames { get; }

        /// <summary>Damage band, inclusive at both ends.</summary>
        public int MinDamage { get; }

        /// <summary>Damage band, inclusive at both ends.</summary>
        public int MaxDamage { get; }

        /// <summary>
        /// Whether the rolled damage is then scaled by the attacker's base stat percentage. Most
        /// routines apply their roll raw; the ones that set this hit for less as the creature is
        /// worn down.
        /// </summary>
        public bool ScalesWithStat { get; }
    }

    /// <summary>
    /// The heavy shot a creature owns, or <c>null</c> if it has none.
    /// </summary>
    /// <remarks>
    /// <b>The original has no default here and uses the values uninitialised</b> when a creature
    /// outside these four reaches this routine — a latent bug that the dispatch happens not to
    /// expose. We refuse instead of reproducing undefined behaviour.
    /// </remarks>
    public static RangedTurn? HeavyShotFor(int creatureType) => creatureType switch {
        0x29 => new RangedTurn(RangedChoice.HeavyShot, 2, 1, 0x14, 0x1d),
        0x2a => new RangedTurn(RangedChoice.HeavyShot, 3, 3, 0x14, 0x1d),
        0x2b => new RangedTurn(RangedChoice.HeavyShot, 0x32, 3, 0x14, 0x1d),
        AlwaysHeavyCreature => new RangedTurn(RangedChoice.HeavyShot, 0x32, 3, 0x14, 0x1d),
        _ => null,
    };

    /// <summary>The light shot, which is the same for every creature that uses this routine.</summary>
    public static RangedTurn LightShot() => new RangedTurn(RangedChoice.LightShot, 5, 1, 4, 8);

    /// <summary>
    /// The turn of the creatures that spit, breathe or hurl.
    /// </summary>
    /// <param name="abortRoll">A roll in <c>[0, 100)</c>.</param>
    /// <param name="heavyRoll">A roll in <c>[0, 4)</c>.</param>
    /// <remarks>
    /// <b>Half the time it does not shoot even with a clear line.</b> The abort roll is checked
    /// before anything else and hands the turn to the generic move/attack picker, so these creatures
    /// spend about half their turns repositioning rather than attacking. Skipping that roll would
    /// roughly double their damage output.
    ///
    /// <para>The heavy shot is the common case, not the rare one — three rolls in four take it — and
    /// <see cref="AlwaysHeavyCreature"/> takes it unconditionally. Its damage band is well over
    /// double the light one's, so which branch a port picks by default matters a lot.</para>
    /// </remarks>
    public static RangedTurn ChooseRangedTurn(bool lineOfFireClear, int abortRoll, int heavyRoll,
        int creatureType) {
        if (!lineOfFireClear || abortRoll < AbortShotRoll) {
            return new RangedTurn(RangedChoice.Reconsider);
        }
        if (heavyRoll <= HeavyShotRoll || creatureType == AlwaysHeavyCreature) {
            return HeavyShotFor(creatureType) ?? LightShot();
        }

        return LightShot();
    }

    /// <summary>
    /// The routine that opens with a volley: it shoots when the target is <b>not</b> adjacent and
    /// the way is clear, and otherwise falls back to moving or swinging.
    /// </summary>
    /// <remarks>
    /// <b>Despite living among the melee handlers, its preferred action is a ranged one.</b> A port
    /// that reads the name and closes to melee first would invert the creature's whole behaviour.
    ///
    /// <para>The fallback is two-stage in the original: try the move-or-attack picker, and only if
    /// that declines does the generic action picker run.</para>
    /// </remarks>
    public static bool VolleysRatherThanClosing(bool lineOfFireClear, int distanceToNearest) =>
        lineOfFireClear && distanceToNearest >= VolleyMinimumDistance;

    /// <summary>Damage band of that volley, inclusive at both ends.</summary>
    public const int VolleyMinDamage = 0xf;

    /// <summary>Damage band of that volley, inclusive at both ends.</summary>
    public const int VolleyMaxDamage = 0x22;

    /// <summary>Knockback frames the volley steps through, one render apart.</summary>
    public const int VolleyKnockbackFrames = 4;

    /// <summary>
    /// Whether the volley routine can act at all.
    /// </summary>
    /// <remarks>
    /// <b>Another build difference in our favour:</b> the 1.02 CD release returns early when there
    /// is no nearest actor to find. The floppy build carries on and dereferences it.
    /// </remarks>
    public static bool CanAct(bool hasTarget) => hasTarget;

    // ---- The three routines that shoot when they can and defer when they cannot ----------------

    /// <summary>Minimum range at which the target-clearing routine will shoot.</summary>
    public const int ChargeRoutineMinimumRange = 3;

    /// <summary>Below this roll the target-clearing routine passes up its shot.</summary>
    public const int ChargeRoutineSkipRoll = 5;

    /// <summary>Minimum range at which the three-attack routine will shoot.</summary>
    public const int MixedRoutineMinimumRange = 3;

    /// <summary>Exclusive bound of the roll choosing among the three attacks.</summary>
    public const int MixedAttackRollBound = 3;

    /// <summary>Minimum range at which the heavy-bolt routine will shoot.</summary>
    public const int BoltRoutineMinimumRange = 2;

    /// <summary>
    /// Whether the routine that forgets its target takes its shot.
    /// </summary>
    /// <param name="roll">A roll in <c>[0, 100)</c>.</param>
    /// <remarks>
    /// <b>It passes up the shot on a roll under five</b> — a 5% flinch, small enough to look like a
    /// rounding artefact and easy to drop. It also needs a longer range than the other routines
    /// before it will shoot at all.
    ///
    /// <para>The line-of-fire test here is asked in a different mode from the other routines' — the
    /// trace is called with a different flag. What the two modes differ in is not established, so it
    /// is passed through rather than assumed equivalent.</para>
    /// </remarks>
    public static bool TakesTheDistantShot(int distanceToNearest, bool lineOfFireClear, int roll) =>
        distanceToNearest >= ChargeRoutineMinimumRange
        && lineOfFireClear
        && roll >= ChargeRoutineSkipRoll;

    /// <summary>
    /// <b>That routine drops its target at the end of every turn, whatever it did.</b>
    /// </summary>
    /// <remarks>
    /// Not a detail: a creature that never carries a target between turns always reads as
    /// <i>disengaged</i> to the target filters in <see cref="CombatAi"/>, so it can never be found
    /// by the "engaged" role and is always eligible for the "disengaged" one. Dropping this line
    /// would quietly change who the rest of the field goes after.
    /// </remarks>
    public static bool ClearsTargetAfterActing => true;

    /// <summary>
    /// One of three attacks, chosen with a flat roll — the creature has no preference among them.
    /// </summary>
    /// <param name="roll">A roll in <c>[0, 3)</c>.</param>
    /// <remarks>
    /// <b>The knockback runs opposite to the damage</b>: the hardest of the three shoves least and
    /// the weakest shoves most, so they are not simply three strengths of one attack.
    ///
    /// <para>All three scale the rolled damage by the attacker's base stat percentage, which the
    /// other routines do not do at all — this creature hits for less as it is worn down.</para>
    /// </remarks>
    public static RangedTurn MixedAttack(int roll) => roll switch {
        0 => new RangedTurn(RangedChoice.HeavyShot, 2, 1, 0xf, 0x22, scalesWithStat: true),
        1 => new RangedTurn(RangedChoice.HeavyShot, 3, 2, 5, 34, scalesWithStat: true),
        _ => new RangedTurn(RangedChoice.HeavyShot, 4, 3, 5, 14, scalesWithStat: true),
    };

    /// <summary>
    /// Whether the three-attack routine shoots. <b>It wants more room than the others</b> — strictly
    /// beyond two tiles, where the rest settle for beyond one.
    /// </summary>
    public static bool TakesTheMixedAttack(int distanceToNearest, bool lineOfFireClear) =>
        lineOfFireClear && distanceToNearest > MixedRoutineMinimumRange - 1;

    /// <summary>The heaviest single attack in the bespoke set.</summary>
    public static RangedTurn HeavyBolt() => new RangedTurn(RangedChoice.HeavyShot, 4, 4, 0x2d, 0x4a);

    /// <summary>Whether the heavy-bolt routine shoots rather than deferring.</summary>
    public static bool TakesTheHeavyBolt(int distanceToNearest, bool lineOfFireClear) =>
        lineOfFireClear && distanceToNearest > BoltRoutineMinimumRange - 1;

    /// <summary>
    /// <b>The heavy-bolt creature refills one of its stats to full at the start of every turn</b>,
    /// before it decides anything.
    /// </summary>
    /// <remarks>
    /// It is a single assignment buried at the top of the routine and trivially missed, but it means
    /// this creature cannot be worn down through that stat at all — whatever drains it is undone
    /// each turn. A port without it has a materially weaker monster.
    /// </remarks>
    public static bool RefillsStatEachTurn => true;
}
