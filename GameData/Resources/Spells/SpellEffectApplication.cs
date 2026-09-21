namespace GameData.Resources.Spells;

/// <summary>
/// Applying a spell's magnitude once it has been computed — the calculation switch of IDA
/// <c>Cast_Spell</c> (ovr173, around 0x686a7), plus the delivery animations that precede it.
/// </summary>
public static class SpellEffectApplication {
    /// <summary>
    /// <b>The duration magnitude is computed HERE, not by the magnitude function.</b>
    /// </summary>
    /// <param name="cost">The effective cost, after the prologue's modifiers.</param>
    /// <param name="duration">The spell record's duration.</param>
    /// <remarks>
    /// This is the other half of <see cref="SpellEffectMagnitude"/> answering 0 for the
    /// cost-times-duration calculation — that is not a gap, it is a division of labour. A port that
    /// treats the magnitude function as the whole story gives every duration spell an effect of
    /// zero.
    /// </remarks>
    public static int DurationMagnitude(int cost, int duration) {
        if (duration > 0) {
            return cost * duration;
        }

        int divisor = duration == MostNegativeDuration ? OverflowGuard : -duration;

        return divisor == 0 ? 0 : cost / divisor;
    }

    /// <summary>
    /// The duration value the original guards against negating, because negating it overflows.
    /// </summary>
    public const int MostNegativeDuration = unchecked((short)0x8000);

    /// <summary>What the original substitutes for that value rather than negating it.</summary>
    public const int OverflowGuard = 0x7fff;

    /// <summary>
    /// <b>A negative duration divides instead of multiplying</b> — the exact mirror of what a
    /// negative effect does on the cost-times-damage calculation, down to the same guard against
    /// negating the most-negative value.
    /// </summary>
    /// <remarks>
    /// So the sign of a record field flips the arithmetic from scaling up to scaling down, in two
    /// separate calculations, with no flag to say so. Reading either field as a plain magnitude
    /// inverts the spell.
    /// <para><b>Deliberately callerless.</b> A restatement of DurationMagnitude, which CombatRuntime uses.</para>
    /// </remarks>
    public static bool NegativeDurationDivides => true;

    /// <summary>
    /// <b>A duration of exactly zero would divide by zero.</b>
    /// </summary>
    /// <remarks>
    /// The original's branches are "greater than zero, multiply" and "otherwise, divide by the
    /// negated value" — and zero falls into the second, dividing by nothing. No shipped spell pairs
    /// this calculation with a zero duration, so it never fires; this port answers 0 rather than
    /// reproducing a divide fault.
    /// <para><b>Deliberately callerless.</b> A recorded fact; DurationMagnitude answers 0 instead of faulting.</para>
    /// </remarks>
    public static bool ZeroDurationWouldFault => true;

    /// <summary>
    /// An effect lasts <b>one tick longer</b> on a target lacking a particular combat-status bit.
    /// </summary>
    /// <remarks>
    /// Applied after the arithmetic and before the effect is registered, so it is a flat bonus
    /// rather than a scaled one — and it depends on the <i>target's</i> state, not the caster's or
    /// the spell's. Easy to miss entirely, and it shifts every duration by one.
    /// </remarks>
    public static int AdjustDurationForTarget(int duration, bool targetHasStatusBit) =>
        targetHasStatusBit ? duration : duration + 1;

    /// <summary>
    /// What the registered effect carries in its per-spell flag byte: <b>the spell's colour</b>.
    /// </summary>
    /// <remarks>
    /// The field the pool calls a flag is fed the record's colour, so a value that reads as
    /// presentation is doing duty as effect data.
    /// <para><b>Deliberately callerless.</b> CombatRuntime.RegisterLingeringEffect passes 0 for the flag: the spell record carries no colour yet, and no port handler reads the flag.</para>
    /// </remarks>
    public static bool EffectFlagIsTheSpellColour => true;



    /// <summary>
    /// <b>Resistance is checked here, and it skips the effect outright.</b>
    /// </summary>
    /// <remarks>
    /// The counterpart to weakness, and they are not symmetrical. Weakness doubles the cost in the
    /// prologue, so it scales the effect; resistance is tested on the duration path and jumps past
    /// the application entirely, so it <i>cancels</i> it. Modelling resistance as "halve the cost"
    /// to mirror weakness would let a resistant creature still take a reduced effect where the
    /// original gives it none.
    ///
    /// <para>Note also that this is where the real resistance test lives — the copy inside the
    /// magnitude function is on a path that returns 0 either way, and is vestigial.</para>
    ///
    /// <para>It is not the only one, though: the tail checks resistance three more times, each for
    /// a different purpose. See <see cref="SpellCastTail.ResistanceCheckSites"/> — one boolean
    /// applied once will not reproduce it.</para>
    /// </remarks>
    public static bool ResistanceSkipsEffect(bool targetResists) => targetResists;

    /// <summary>
    /// The strength a grid spell puts on the field: <b>duration times cost</b>.
    /// </summary>
    /// <remarks>
    /// <b>Both grid paths use this same product</b> — the delivery category that paints an element
    /// on click, and the calculation that stamps the cell under the target — so it is one rule with
    /// two call sites rather than a coincidence. Either way a grid spell's power comes from the
    /// record's duration even though nothing about it lasts for a duration.
    /// </remarks>
    public static int GridElementStrength(int cost, int duration) => cost * duration;

    /// <summary>Delivery categories that play the ranged wind-up before the effect, and the only ones that
    /// earn casting skill (<see cref="AwardsCastingSkill"/>).</summary>
    public static readonly int[] RangedWindupCategories = { 0, 2, 3, 7, 8 };

    /// <summary>Delivery categories that play a melee swing instead.</summary>
    /// <remarks>
    /// <b>Deliberately callerless.</b> SpellCastSound.ForCombatCast plays the swing for every kind
    /// outside RangedKinds.
    ///
    /// <para><b>These are the two the original NAMES, not the whole swing set</b> — the arm is
    /// <c>case 1: case 4: default:</c>, so 5, 6 and -1 swing too. Use
    /// <see cref="SwingsInsteadOfCasting"/> for the predicate; this array is only the named pair.
    /// </para>
    /// </remarks>
    public static readonly int[] MeleeSwingCategories = { 1, 4 };

    /// <summary>
    /// Whether this delivery category swings rather than casting at range.
    /// </summary>
    /// <remarks>
    /// <b>The swing is the switch's DEFAULT arm, not two enumerated cases.</b> CSPELL.C reads
    /// <c>case 1: case 4: default:</c> falling into one body, so 1 and 4 are merely the two the
    /// original bothered to name — the grid kinds 5 and 6, the field-only -1 and anything else all
    /// swing as well. Written as <c>== 1 || == 4</c> this answered <b>false</b> for 5 and 6, which
    /// disagrees with the live path: <c>SpellCastSound.ForCombatCast</c> plays the swing for every
    /// kind outside <see cref="RangedWindupCategories"/> and is correct.
    ///
    /// <para>Nothing called this, so the disagreement was never reachable — but it read like the
    /// canonical predicate and would have introduced the bug the moment it was wired. Stated as the
    /// complement so the three expressions of this one rule cannot drift apart again (2026-09-21).
    /// </para>
    ///
    /// <para>These are also the categories that skip the casting-skill award, which is why
    /// <see cref="AwardsCastingSkill"/> is its exact negation.</para>
    /// </remarks>
    public static bool SwingsInsteadOfCasting(int deliveryCategory) =>
        System.Array.IndexOf(RangedWindupCategories, deliveryCategory) < 0;

    /// <summary>
    /// Whether the caster is paid casting skill for this delivery category.
    /// </summary>
    /// <remarks>
    /// Only the wind-up categories reach the award pair (CSPELL.C:1305) — see
    /// <c>CombatAdvancement.OnSpellCast</c>. The swing arm is also the switch's <c>default</c>, so
    /// any kind outside {0, 2, 3, 7, 8} — the grid kinds 5/6 and the field-only -1 included — teaches
    /// the caster nothing.
    /// </remarks>
    public static bool AwardsCastingSkill(int deliveryCategory) =>
        System.Array.IndexOf(RangedWindupCategories, deliveryCategory) >= 0;

    /// <summary>
    /// <b>STALE — CORRECTED 2026-09-14: the second test does NOT abort the cast.</b> See <see cref="SpellCastTail.SkyfireEndsTheCast"/>,
    /// corrected 2026-09-08 against both sources: the fixed-amount arm returns zero damage and the cast still reaches the charge. Superseded reading below.
    /// </summary>
    /// <remarks>
    /// The fixed-amount arm exists only to re-check whether the target is using metal, and on a
    /// non-metal target it clears the register holding the spell record pointer. That register is the
    /// tail's continue flag — see <see cref="SpellCastTail.RecordPointerDoublesAsContinueFlag"/> — so
    /// the second test is not redundant with the magnitude rule: the first makes Skyfire's damage
    /// zero, the second stops the cast before it animates, bills or lands.
    /// <para><b>Deliberately callerless.</b> Superseded by SpellCastTail.SkyfireEndsTheCast: SpellEffectMagnitude already yields zero and nothing aborts.</para>
    /// </remarks>
    public static bool SkyfireIsRecheckedAtApplication => true;
}
