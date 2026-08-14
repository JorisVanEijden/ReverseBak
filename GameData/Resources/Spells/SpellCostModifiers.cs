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
    /// The surcharge a global modifier adds — <b>half of the original cost</b>.
    /// </summary>
    /// <remarks>
    /// Taken from the cost as it arrived, not from the running value, so it is exactly +50% and not
    /// compounded with anything applied afterwards.
    /// </remarks>
    public static int Surcharge(int originalCost) => originalCost / 2;

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
    /// </remarks>
    public static int ApplyWeakness(int cost) => cost * 2;

    /// <summary>
    /// The cost the magnitude is finally computed from.
    /// </summary>
    /// <param name="cost">The cost as the caller supplied it.</param>
    /// <param name="surcharged">Whether the global surcharge modifier is active.</param>
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
