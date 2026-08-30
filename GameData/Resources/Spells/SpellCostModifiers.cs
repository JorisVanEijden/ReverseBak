namespace GameData.Resources.Spells;

/// <summary>
/// What happens to a spell's cost before anything is computed from it — the prologue of IDA
/// <c>Cast_Spell</c> (ovr173 @0x6850c).
///
/// <para><b>The cost is the effect dial.</b> Nearly everything a spell does scales from it via
/// <see cref="SpellEffectMagnitude"/>, so the modifiers applied here change outcomes rather than
/// just prices — and they are applied in an order that does not commute.</para>
/// </summary>
public static class SpellCostModifiers {
    /// <summary>
    /// Targeting type meaning the spell has no target, whatever the caller passed.
    /// </summary>
    /// <remarks>
    /// <b>The target is discarded, not ignored.</b> The dispatcher nulls it outright, so every later
    /// step — including the weakness check below — behaves as if the cast were untargeted. Passing a
    /// target to one of these spells and expecting it to matter is a mistake the data invites.
    /// </remarks>
    public const int UntargetedTargetingType = 8;

    /// <summary>Whether this spell ignores whatever target it was handed.</summary>
    public static bool DiscardsTarget(int targetingType) =>
        targetingType == UntargetedTargetingType;

    /// <summary>
    /// The surcharge the <b>Infinity Pool</b> adds — half of the original cost.
    /// </summary>
    /// <remarks>
    /// <b>IT IS AN ITEM, NOT WEATHER.</b> The flag is <c>g_bStormAmplify</c> in the reconstructed
    /// source and the name says storm, but the only thing that raises it is
    /// <c>combat_arena_resume_dispatch</c>'s <c>case 0x0d</c> — object 13, the Infinity Pool —
    /// immediately after the cast menu it opened returns a spell. See
    /// <see cref="Combat.CombatItemUse"/>. The cast that follows is amplified and the flag is
    /// cleared, so it is exactly one spell per use.
    ///
    /// <para>Recorded because this parameter sat unfed for months as "the storm amplifier, which has
    /// no source on our side yet" — a search of the world for something that lives in an inventory.</para>
    ///
    /// Taken from the cost as it arrived, not from the running value, so it is exactly +50% and not
    /// compounded with anything applied afterwards.
    ///
    /// <para><b>An arithmetic shift, not a division.</b> The original is <c>c += c &gt;&gt; 1</c>,
    /// which rounds toward negative infinity — so a heal cast at -41 becomes -62 and not the -61
    /// that <c>c / 2</c> gives. The two agree on every even cost and on every positive one, which is
    /// exactly why the difference survives casual testing; it shows up only on an odd-cost heal.</para>
    /// </remarks>
    public static int Surcharge(int originalCost) => originalCost >> 1;

    /// <summary>
    /// A negative cost is a <b>sign plus a magnitude</b>, not a negative quantity.
    /// </summary>
    /// <remarks>
    /// The dispatcher records the sign in a flag and then negates the cost, so everything downstream
    /// works with a positive number and the flag decides what the sign meant. Feeding the negative
    /// value straight into the magnitude would invert every scaled effect it touches.
    /// </remarks>
    public static bool IsNegated(int cost) => cost < 0;

    /// <summary>
    /// <b>The negated flag does NOT decide whether the target is healed.</b>
    /// </summary>
    /// <remarks>
    /// It is easy to read <see cref="IsNegated"/> as "this is a heal" and reach for it when the
    /// delivered amount needs a sign. The original never does: the amount's sign comes from the
    /// spell record's own magnitude word, and the flag gates three other things — the to-hit roll
    /// (a negated cast never rolls), the caster's wind-up animation, and the armour wear billed to
    /// the caster. A restoring spell is one whose data is negative, not one that was paid for
    /// backwards.
    /// </remarks>
    public static bool NegatedCostMeansHealed => false;

    /// <summary>
    /// <b>A creature weak to a spell has the cost DOUBLED</b>, which is how it ends up taking about
    /// twice the effect.
    /// </summary>
    /// <remarks>
    /// The weakness is not applied to the damage — it is applied to the dial the damage is computed
    /// from, before the computation. So it multiplies only the cost-scaled part of a spell and does
    /// nothing at all to a flat-amount one, which is a distinction a port that doubles the final
    /// number would lose.
    ///
    /// <para>Its counterpart, resistance, works the other way round: it gates the effect inside the
    /// magnitude function rather than scaling the cost here.</para>
    ///
    /// <para><b>It is consulted for ANY target, including a heal.</b> The check is on the
    /// (creature, spell) pair and knows nothing about the sign, so a creature listed as vulnerable
    /// to a spell also receives double from one aimed at it to help.</para>
    ///
    /// <para>The doubling is undone further down the routine — the same bitmap is tested a second
    /// time and shifts the cost back right — so it is in force only while the magnitude and the
    /// duration/tile arms are computed. Nothing after that point sees the doubled value.</para>
    /// </remarks>
    public static int ApplyWeakness(int cost) => cost * 2;

    /// <summary>
    /// The cost the magnitude is finally computed from.
    /// </summary>
    /// <param name="cost">The cost as the caller supplied it.</param>
    /// <param name="surcharged">Whether the Infinity Pool is amplifying this cast.</param>
    /// <param name="targetIsWeak">Whether the target is weak to this spell.</param>
    /// <remarks>
    /// <b>The order does not commute.</b> Surcharge is taken from the original cost, the sign is
    /// stripped next, and the weakness doubling comes last — so a weak target facing a surcharged
    /// cast gets <c>|c + c/2| × 2</c>, not <c>|c| × 2 + c/2</c>. Reordering them is easy and the
    /// difference is largest exactly where it matters most.
    /// </remarks>
    public static int Effective(int cost, bool surcharged, bool targetIsWeak) {
        int result = cost;
        if (surcharged) {
            result += Surcharge(cost);
        }
        if (result < 0) {
            result = -result;
        }
        if (targetIsWeak) {
            result = ApplyWeakness(result);
        }

        return result;
    }

    /// <summary>Bytes per record in the spell table.</summary>
    public const int SpellRecordSize = 22;
}
