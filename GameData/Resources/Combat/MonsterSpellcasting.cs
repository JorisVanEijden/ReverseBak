namespace GameData.Resources.Combat;

using GameData.Resources.Spells;

/// <summary>
/// How a spellcasting monster decides what to do with its turn, and which spell it reaches for —
/// <c>monster_chooseSpellcastAction</c> (ovr171 @0x65e3e) and <c>monster_selectSpellToCast</c>
/// (ovr173 @0x66c20).
///
/// <para>Distinct from <see cref="MonsterTurnRoutines"/>, which holds the bespoke per-creature
/// routines. This is the generic caster AI: a table-driven preference order over eight action
/// slots, plus a filter chain that picks a spell out of the caster's book at the moment it is
/// needed. <b>There is no fixed monster spell</b> — the same creature can cast different spells on
/// different turns.</para>
/// </summary>
public static class MonsterSpellcasting {
    /// <summary>Action slots in a pattern row, and attempts per turn.</summary>
    public const int SlotCount = 8;

    /// <summary>
    /// The preference order for each spellcast pattern — <c>spellcastPattern_actionPriority</c>.
    /// </summary>
    /// <remarks>
    /// <b>Every row begins with its own pattern number.</b> So the pattern a monster carries is
    /// literally "the action I try first", and the rest of the row is its fallback order. Eight
    /// patterns, each a permutation of the eight slots, which is not something the MONST data hints
    /// at — the field is labelled as an attack pattern and is really a row index.
    /// </remarks>
    private static readonly int[][] Priority = {
        new[] { 1, 8, 6, 3, 7, 2, 4, 5 },
        new[] { 2, 1, 3, 7, 5, 8, 6, 4 },
        new[] { 3, 6, 8, 7, 1, 2, 4, 5 },
        new[] { 4, 3, 5, 6, 2, 1, 7, 8 },
        new[] { 5, 4, 7, 6, 3, 8, 1, 2 },
        new[] { 6, 8, 1, 3, 2, 7, 4, 5 },
        new[] { 7, 3, 2, 8, 6, 1, 5, 4 },
        new[] { 8, 2, 3, 7, 5, 1, 6, 4 },
    };

    /// <summary>Highest pattern the table defines. Pattern 0 has no row — see <see cref="Casts"/>.</summary>
    public const int MaxPattern = 8;

    /// <summary>
    /// <b>Pattern 0 means the monster never casts.</b>
    /// </summary>
    /// <remarks>
    /// The attempt loop is entered only while the pattern is non-zero, and the test sits before the
    /// <i>first</i> attempt as well as between them — so a pattern of 0 produces no spell action at
    /// all, ever.
    ///
    /// <para>The table is laid out to depend on that. Indexing is <c>pattern × 16</c> bytes from the
    /// table's base, so row 0 would sit in the <i>preceding</i> array — the last four entries of the
    /// action-thunk table — and would decode as nonsense slot numbers. The rows for patterns 1-8
    /// follow it. The overlap is safe only because the code guarantees row 0 is never read, which is
    /// worth knowing before anyone "fixes" the table by giving it a row 0.</para>
    /// </remarks>
    public static bool Casts(int spellcastPattern) =>
        spellcastPattern > 0 && spellcastPattern <= MaxPattern;

    /// <summary>
    /// The action slot this pattern tries on the given attempt.
    /// </summary>
    /// <returns>A slot in 1-8, or 0 when the pattern never casts or the attempt is out of range.</returns>
    public static int SlotFor(int spellcastPattern, int attempt) {
        if (!Casts(spellcastPattern) || attempt < 0 || attempt >= SlotCount) {
            return 0;
        }

        return Priority[spellcastPattern - 1][attempt];
    }

    /// <summary>The combined health-and-stamina below which the monster does not act at all.</summary>
    public const int MinimumPoolToAct = 5;

    /// <summary>
    /// Whether the monster is well enough to attempt anything.
    /// </summary>
    /// <remarks>
    /// Below the threshold the routine jumps the attempt counter straight past the end of the loop,
    /// so it does not try slot 8 or any other — it makes <i>no</i> attempt and falls through to
    /// whatever the caller does with a monster that took no action.
    /// </remarks>
    public static bool WellEnoughToAct(int healthStaminaCombined) =>
        healthStaminaCombined >= MinimumPoolToAct;

    /// <summary>The percentage chance that an attempt is actually made rather than skipped.</summary>
    public const int CommitPercent = 91;

    /// <summary>
    /// Whether this attempt is taken, given its d100 roll.
    /// </summary>
    /// <remarks>
    /// <b>The skip does not end the turn.</b> A failed roll advances the attempt counter and moves
    /// on to the next slot in the row, so a monster with a long fallback list still usually acts —
    /// roughly one attempt in eleven is simply passed over. Reading this as "9% chance to do
    /// nothing" makes casters far more passive than they are.
    /// </remarks>
    public static bool CommitsToAttempt(int rollUnder100) => rollUnder100 < CommitPercent;

    /// <summary>What an action slot actually invokes.</summary>
    public enum SlotAction {
        /// <summary>Slot 1 — a routine of its own, not a target-mode cast.</summary>
        SpecialFirst,

        /// <summary>Slot 8 — likewise, and the one every pattern keeps in reserve.</summary>
        SpecialLast,

        /// <summary>A cast aimed by one of the six target-selection modes.</summary>
        TargetedCast,
    }

    /// <summary>
    /// The target-selection mode a slot casts with, or -1 for the two special slots.
    /// </summary>
    /// <remarks>
    /// <b>The mapping is not in slot order.</b> Slots 2, 3 and 4 take modes 0, 1 and 2 as you would
    /// expect, and then it jumps: slot 5 takes mode 4, slot 6 takes mode 5, and slot 7 takes mode 3.
    /// Read from the thunk table entry by entry rather than assumed — an existing note in the
    /// database had them sequential, and they are not.
    /// </remarks>
    public static int TargetModeOf(int slot) {
        switch (slot) {
            case 2: return 0;
            case 3: return 1;
            case 4: return 2;
            case 5: return 4;
            case 6: return 5;
            case 7: return 3;
            default: return -1;
        }
    }

    /// <summary>Which kind of action a slot is.</summary>
    public static SlotAction ActionOf(int slot) {
        switch (slot) {
            case 1: return SlotAction.SpecialFirst;
            case 8: return SlotAction.SpecialLast;
            default: return SlotAction.TargetedCast;
        }
    }

    // ---------------------------------------------------------------- choosing the spell

    /// <summary>
    /// <b>A caster under Thoughts Like Clouds selects nothing.</b>
    /// </summary>
    /// <remarks>
    /// The very first test in the selector, before the book is even scanned. The same effect blocks
    /// the type-2 spell delivery (see <c>SpellCastRoutines.HealIsBlockedForFree</c>), so it shuts a
    /// monster caster down from both ends.
    /// </remarks>
    public static bool CanSelect(bool casterHasThoughtsLikeClouds) => !casterHasThoughtsLikeClouds;

    /// <summary>
    /// <b>The book is scanned from the highest spell number downwards.</b>
    /// </summary>
    /// <remarks>
    /// So a monster reaches for its most advanced matching spell first and only falls back to weaker
    /// ones. Scanning upwards would invert every caster's behaviour.
    /// </remarks>
    public static bool ScansHighToLow => true;

    /// <summary>
    /// <b>The scan starts one record past the end of the spell table.</b>
    /// </summary>
    /// <remarks>
    /// The loop is seeded with the spell <i>count</i> and its guard is "greater than -1", so the
    /// first iteration indexes <c>spellTable[count]</c> — a full 22-byte record beyond what the
    /// loader allocated and read. <c>Load_spells</c> allocates exactly <c>count × 22</c> bytes and
    /// reads exactly <c>count</c> records, so valid indices stop at <c>count - 1</c>.
    ///
    /// <para>It is harmless in practice: the bytes just past the table are the start of the
    /// spell-name pointer array, so the martial and targeting-type fields read as pointer halves and
    /// almost never match the caller's filters. Not reproduced — <see cref="FirstCandidate"/>
    /// starts at the last real spell — but recorded, because a note in the database described this
    /// loop as starting at <c>count - 1</c> and it does not.</para>
    /// </remarks>
    public static bool ScanStartsPastTheEndOfTheTable => true;

    /// <summary>The first spell number the port considers: the last real entry.</summary>
    public static int FirstCandidate(int spellCount) => spellCount - 1;

    /// <summary>
    /// Spells a monster will never select, whatever its book says.
    /// </summary>
    /// <remarks>
    /// Two, excluded by number after every other filter has passed. Thoughts Like Clouds is the one
    /// that would disable the caster itself, and Invitation would drag an enemy toward it — both are
    /// things the AI has no way to use sensibly, so they are simply struck out.
    /// </remarks>
    public static bool NeverSelected(int spellId) =>
        spellId == SpellIds.Invitation || spellId == SpellIds.ThoughtsLikeClouds;

    /// <summary>
    /// Whether a candidate survives the whole filter chain.
    /// </summary>
    /// <param name="spellId">The candidate.</param>
    /// <param name="matchesFilters">The requested martial flag and targeting type both match.</param>
    /// <param name="castable">The caster can afford and has learned it.</param>
    /// <param name="coinFlipHeads">The 50% roll the original takes per surviving candidate.</param>
    /// <param name="alreadyOnTarget">The target already carries this effect.</param>
    /// <remarks>
    /// <b>The coin flip is taken before the exclusions and the already-on-target test</b>, so a
    /// candidate that was going to be rejected anyway still consumes a random number. That only
    /// matters for reproducing the original's RNG stream, which this port does not attempt.
    ///
    /// <para>The already-on-target test is the reason a monster does not stack the same effect: it
    /// is the one place the engine consults the effect pool before casting, and it looks at the
    /// <i>target</i>, not the caster.</para>
    /// </remarks>
    public static bool Selects(int spellId, bool matchesFilters, bool castable, bool coinFlipHeads,
        bool alreadyOnTarget) =>
        matchesFilters && castable && coinFlipHeads && !NeverSelected(spellId) && !alreadyOnTarget;

    // ---------------------------------------------------------------- executing the action
    // monster_castSpellAtSelectedTarget @0x65a0f, and the six wrappers that differ only in mode.

    /// <summary>
    /// The selection parameter every one of the six wrappers passes: <b>6, always</b>.
    /// </summary>
    /// <remarks>
    /// Each wrapper packs its arguments into one dword as <c>(mode &lt;&lt; 16) | 6</c>, so the six
    /// differ in the selection mode and in nothing else. Verified for all six rather than inferred
    /// from the first.
    /// </remarks>
    public const int TargetSelectionParameter = 6;

    /// <summary>
    /// How much slack the target picker is given: <b>four, less a quarter of the casting skill</b>.
    /// </summary>
    /// <remarks>
    /// <b>A better caster gets a smaller number.</b> It runs from 4 at no skill down to 0 at 100, so
    /// whatever the picker does with it, skill makes the search stricter rather than wider — the
    /// opposite of the obvious reading, and easy to invert.
    /// </remarks>
    public static int CastingFactor(int accuracyCasting) => 4 - (accuracyCasting / 25);

    /// <summary>
    /// <b>Monsters only ever cast martial spells.</b>
    /// </summary>
    /// <remarks>
    /// Every call to the selector passes a martial flag of 1, on both passes. Half the catalogue is
    /// non-martial and no monster can reach any of it — Dragon's Breath, Nightfingers, Stardusk and
    /// the rest are player-only by this rule alone, with nothing in the creature data saying so.
    /// </remarks>
    public static bool OnlyCastsMartialSpells => true;

    /// <summary>
    /// The targeting types asked for, in order, on the first pass.
    /// </summary>
    /// <remarks>
    /// Type 0 first — the only type that can miss — and type 1 as the fallback. Together with the
    /// martial rule and the two struck-out spells, this is what bounds the whole monster repertoire
    /// to fifteen of the catalogue's forty-five.
    /// </remarks>
    public static readonly int[] FirstPassTargetingTypes = { 0, 1 };

    /// <summary>The single targeting type the second pass will accept.</summary>
    public static readonly int[] SecondPassTargetingTypes = { 1 };

    /// <summary>
    /// Whether a monster could ever choose this spell, from its catalogue fields alone.
    /// </summary>
    /// <param name="spellId">The spell.</param>
    /// <param name="isMartial">Its martial flag.</param>
    /// <param name="targetingType">Its targeting type.</param>
    /// <remarks>
    /// Fifteen spells satisfy this across the shipped catalogue. Every buff and utility spell is
    /// excluded by the martial flag, and Final Rest — the one that kills outright — is excluded by
    /// its targeting type, so no monster can ever cast it at the party.
    /// </remarks>
    public static bool InMonsterRepertoire(int spellId, bool isMartial, int targetingType) =>
        isMartial
        && (targetingType == 0 || targetingType == 1)
        && !NeverSelected(spellId);

    /// <summary>
    /// <b>The action makes two attempts at finding a target, and they are not the same attempt.</b>
    /// </summary>
    /// <remarks>
    /// The first asks the picker with the casting factor; if it yields a target, the spell must pass
    /// a health check <i>and a clear line of fire</i> before the cast goes through the normal
    /// routine. If it yields no target at all, the second asks again with a factor of zero — and
    /// that path accepts only targeting type 1, checks health with a different argument, <b>skips
    /// the line-of-fire test entirely</b>, and casts through a different routine.
    ///
    /// <para>So a monster that cannot see anything it likes will still cast, at something, through
    /// a wall. Modelling this as one retry of the same logic loses the distinction.</para>
    /// </remarks>
    public static bool RequiresLineOfFire(int pass) => pass == 1;

    /// <summary>The casting factor the second pass uses instead of the skill-derived one.</summary>
    public const int SecondPassCastingFactor = 0;

    /// <summary>
    /// Whether the turn is already spent before any of this runs.
    /// </summary>
    /// <remarks>
    /// A pre-check ahead of everything returns "acted" without casting. What it tests has not been
    /// read (<c>sub_ovr171_86</c> @0x65796), so this only records that the early exit exists and
    /// reports success rather than failure — a caller treating it as "did nothing" would let the
    /// monster act twice.
    /// </remarks>
    public static bool PreCheckReportsTheTurnSpent => true;
}
