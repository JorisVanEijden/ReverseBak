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
    ///
    /// <para><b>This is not the whole repertoire.</b> It covers the six targeted-cast slots, which
    /// are the only ones that go through the selector. The heal slot bypasses it entirely and names
    /// two more spells by number — see <see cref="HealSpells"/> — neither of which could ever come
    /// back from here.</para>
    /// </remarks>
    public static bool InMonsterRepertoire(int spellId, bool isMartial, int targetingType) =>
        isMartial
        && (targetingType == 0 || targetingType == 1)
        && !NeverSelected(spellId);

    /// <summary>Everything a monster can cast: the selector's fifteen plus the heal slot's two.</summary>
    public static bool CastableByAMonster(int spellId, bool isMartial, int targetingType) =>
        InMonsterRepertoire(spellId, isMartial, targetingType) || IsHealSpell(spellId);

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

    // ---------------------------------------------------------------- the power invested
    // sub_ovr173_4D4 @0x66bd4, reached from both cast routines.

    /// <summary>
    /// The power a monster invests in a cast: <b>the spell's maximum, capped at one below its own
    /// combined health and stamina</b>.
    /// </summary>
    /// <param name="spellMaximumCost">The record's maximum cost.</param>
    /// <param name="healthStaminaPool">The caster's current combined pool.</param>
    /// <remarks>
    /// <b>Monsters never hold back.</b> Where the player picks a power on a slider, a monster always
    /// asks for the record's maximum — so a monster Evil Seek is always the 30-point version, and
    /// every cost-scaled effect in <see cref="SpellCostModifiers"/> lands at full strength.
    ///
    /// <para>The cap is the interesting half. Casting is paid for in health (see
    /// <c>SpellCastRoutines</c>), and the cap is <i>pool − 1</i> rather than the pool itself — so a
    /// monster will spend itself down to a single point but <b>never kill itself casting</b>. A port
    /// that caps at the pool instead would let casters suicide, and one that ignores the cap would
    /// let them spend health they do not have.</para>
    /// </remarks>
    public static int InvestedPower(int spellMaximumCost, int healthStaminaPool) =>
        spellMaximumCost >= healthStaminaPool ? healthStaminaPool - 1 : spellMaximumCost;

    /// <summary>
    /// <b>Only the second pass rolls to hit.</b>
    /// </summary>
    /// <remarks>
    /// The two cast routines differ in more than which one they are. The first-pass routine hands
    /// straight to <c>Cast_Spell</c> — the line-of-fire trace it already passed is its whole
    /// verification. The second-pass routine, which skipped that trace, instead rolls the ranged
    /// to-hit formula keyed on the caster's casting skill and only casts if it lands.
    ///
    /// <para>So the two passes verify the shot in different currencies: geometry on the first,
    /// probability on the second. Neither is a superset of the other.</para>
    /// </remarks>
    public static bool RollsToHit(int pass) => pass == 2;

    /// <summary>
    /// What a missed second-pass cast does: <b>turn to face the target, and nothing else</b>.
    /// </summary>
    /// <remarks>
    /// No sound, no animation of a failed cast, no cost — the routine computes the direction to the
    /// target and plays the caster's idle animation facing it. From the player's side a monster that
    /// misses this way is indistinguishable from one that simply turned.
    /// </remarks>
    public static bool AMissedCastOnlyTurnsToFace => true;

    // ---------------------------------------------------------------- action slot 1: heal an ally
    // sub_ovr171_4BD @0x65bcd and its spell chooser sub_ovr171_40C @0x65b1c.

    /// <summary>Gift of Sung — the heal proper, and a targeting-type-2 spell.</summary>
    public const int GiftOfSung = 7;

    /// <summary>Hocho's Haven — the fallback, a lingering effect rather than a heal.</summary>
    public const int HochosHaven = 6;

    /// <summary>The two spells the heal slot names directly.</summary>
    public static readonly int[] HealSpells = { GiftOfSung, HochosHaven };

    /// <summary>
    /// <b>The heal action does not use the selector at all.</b>
    /// </summary>
    /// <remarks>
    /// It names Gift of Sung and Hocho's Haven by number. Neither is martial-with-type-0-or-1, so
    /// neither could ever come back from <see cref="Selects"/> — which is why the repertoire is
    /// seventeen spells and not the fifteen the targeted slots imply. Gift of Sung is targeting type
    /// 2, so it lands in the heal delivery with its six-affliction gate and its 80% ceiling.
    /// </remarks>
    public static bool IsHealSpell(int spellId) =>
        spellId == GiftOfSung || spellId == HochosHaven;

    /// <summary>The value the minimum-health search starts from, above any real health.</summary>
    public const int HealSearchSentinel = 110;

    /// <summary>The bound of the roll the heal decision is taken against.</summary>
    public const int HealUrgencyRollBound = 80;

    /// <summary>
    /// Which heal spell to cast, or -1.
    /// </summary>
    /// <param name="allyHealthConsulted">The ally health the decision actually reads — see the remarks.</param>
    /// <param name="urgencyRoll">A roll in 0..79.</param>
    /// <param name="giftOfSungAvailable">Gift of Sung is affordable and known.</param>
    /// <param name="hochosHavenAvailable">Hocho's Haven is affordable and known.</param>
    /// <param name="targetAlreadyHasHochosHaven">The candidate already carries that effect.</param>
    /// <remarks>
    /// Gift of Sung when the consulted health is below the roll and the spell is available;
    /// otherwise Hocho's Haven, provided it is available and the candidate does not already have it.
    /// Neither, and the action does nothing.
    ///
    /// <para>The roll makes urgency probabilistic rather than a threshold: a badly hurt ally is
    /// <i>likely</i> to draw the real heal, never certain, and a lightly hurt one can draw it on a
    /// low roll.</para>
    /// </remarks>
    public static int ChooseHealSpell(int allyHealthConsulted, int urgencyRoll,
        bool giftOfSungAvailable, bool hochosHavenAvailable, bool targetAlreadyHasHochosHaven) {
        if (allyHealthConsulted < urgencyRoll && giftOfSungAvailable) {
            return GiftOfSung;
        }

        return hochosHavenAvailable && !targetAlreadyHasHochosHaven ? HochosHaven : -1;
    }

    /// <summary>
    /// <b>The chooser computes the worst-off ally's health and then ignores it.</b>
    /// </summary>
    /// <remarks>
    /// The loop walks the allies keeping a running minimum in one slot and overwriting a second slot
    /// with every ally's health as it goes. After the loop the decision reads the <i>second</i> slot
    /// — the last ally examined — and the minimum is never read again. Verified from the encoded
    /// displacements (<c>bp-4</c> for the minimum, <c>bp-2</c> for the value tested), not from the
    /// frame labels.
    ///
    /// <para>So "is anyone hurt enough to need the real heal" is answered by whoever happens to sit
    /// last in the actor table. Our port takes the value the original tests, so the behaviour
    /// matches; the parameter is named for what it is rather than what it was meant to be.</para>
    /// </remarks>
    public static bool HealUrgencyReadsTheLastAllyNotTheWorst => true;

    /// <summary>
    /// A monster <b>never heals itself</b> with this action.
    /// </summary>
    /// <remarks>
    /// Both the fast path and the full scan skip a candidate that is the caster, so a wounded lone
    /// caster with a heal spell will not use it on itself — it falls through to the next action in
    /// its pattern row.
    /// </remarks>
    public static bool HealsSelf => false;

    /// <summary>
    /// Whether an ally is a valid heal target: <b>alive and below full</b>.
    /// </summary>
    public static bool IsHealTarget(int statPercent) => statPercent > 0 && statPercent < 100;

    /// <summary>
    /// <b>The heal spell is chosen against the first actor and then cast at whoever is found.</b>
    /// </summary>
    /// <remarks>
    /// The chooser runs once, before any target is settled, with the table's first actor as its
    /// candidate. If that actor turns out not to need healing the action scans the rest of the field
    /// — and casts the spell already chosen. So the "does the target already have Hocho's Haven"
    /// test was asked about a different actor than the one that receives it.
    /// </remarks>
    public static bool HealSpellIsChosenBeforeTheTarget => true;

    /// <summary>Only one ally is healed per action; the scan stops at the first it treats.</summary>
    public static bool HealsOneAllyPerAction => true;
}
