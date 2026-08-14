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

    /// <summary>Delivery categories that play the ranged wind-up before the effect.</summary>
    public static readonly int[] RangedWindupCategories = { 0, 2, 3, 7, 8 };

    /// <summary>Delivery categories that play a melee swing instead.</summary>
    public static readonly int[] MeleeSwingCategories = { 1, 4 };

    /// <summary>
    /// Whether this delivery category swings rather than casting at range.
    /// </summary>
    /// <remarks>
    /// <b>Two of the nine categories animate as a melee attack</b>, with a different sound, and they
    /// are also the two that skip the casting-skill award. A port that gives every spell the same
    /// wind-up loses the distinction between reaching out and striking.
    /// </remarks>
    public static bool SwingsInsteadOfCasting(int deliveryCategory) =>
        deliveryCategory == 1 || deliveryCategory == 4;

    /// <summary>
    /// Whether the caster is paid casting skill for this delivery category.
    /// </summary>
    /// <remarks>
    /// Only the wind-up categories reach the award pair — see
    /// <c>CombatAdvancement.OnSpellCast</c>. The melee-swing and grid categories are cast without
    /// teaching the caster anything.
    /// </remarks>
    public static bool AwardsCastingSkill(int deliveryCategory) =>
        !SwingsInsteadOfCasting(deliveryCategory)
        && deliveryCategory != 5
        && deliveryCategory != 6;

    /// <summary>
    /// <b>Skyfire is tested a second time here, and what that test does is unresolved.</b>
    /// </summary>
    /// <remarks>
    /// The fixed-amount case re-checks whether the target is using metal — the same question
    /// <see cref="SpellEffectMagnitude"/> already answers — and on a non-metal target clears the
    /// register holding the spell record pointer before falling into the shared tail. What that is
    /// meant to achieve has not been established, so it is recorded rather than modelled. Do not
    /// assume it is redundant with the magnitude rule without reading the tail.
    /// </remarks>
    public static bool SkyfireIsRecheckedAtApplication => true;
}
