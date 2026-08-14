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
    public static int DurationMagnitude(int cost, int duration) => cost * duration;

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
    /// The strength a grid-effect spell places on the field: <b>duration times cost</b>.
    /// </summary>
    /// <remarks>
    /// The same product the duration calculation uses, but applied at delivery rather than to a
    /// target — so a grid spell's power comes from the record's duration even though nothing about
    /// it lasts for a duration.
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
