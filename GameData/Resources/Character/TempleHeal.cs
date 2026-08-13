namespace GameData.Resources.Character;

/// <summary>
/// What a temple charges to cure a character — <c>charscreen_temple_heal_price</c>
/// (<c>SRC/CHAR/CHARSCRN.C</c>).
///
/// <para>The price is built from the afflictions the character is actually carrying: each one
/// charged contributes its rank at its own rate plus a flat fee, and the total is then scaled by
/// the temple's percentage. Because a healthy character prices at zero, <b>the same function
/// doubles as "does this character need healing at all"</b> — the screen uses a non-zero price to
/// decide whether to offer a Cure button, which is why the original's parameter name (a "filter")
/// is misleading. It is a multiplier.</para>
/// </summary>
public static class TempleHeal {
    /// <summary>Flat fee added for each affliction that is charged for at all.</summary>
    public const int PerConditionFee = 10;

    /// <summary>
    /// Rate per rank for each affliction, indexed by <see cref="ActorCondition"/>.
    ///
    /// <para><b>Healing is free</b> — the original <c>continue</c>s past it rather than charging,
    /// which is consistent with it being the one beneficial entry in the set. Near-death is by far
    /// the dearest at thirty per rank; the plague and poison follow at ten. Note the flat fee is
    /// only added for conditions that are charged, so a character whose only affliction is Healing
    /// still prices at zero and reads as "needs nothing".</para>
    /// </summary>
    private static readonly int[] RatePerRank = {
        4,   // Sick
        10,  // Plagued
        10,  // Poisoned
        3,   // Drunk
        0,   // Healing — not charged at all; see below
        2,   // Starving
        30,  // NearDeath
    };

    /// <summary>The one affliction the temple does not charge for.</summary>
    private const ActorCondition FreeCondition = ActorCondition.Healing;

    /// <summary>
    /// The price to cure one character, already scaled by the temple's rate.
    /// </summary>
    /// <param name="multiplierPercent">
    /// The temple's percentage. 100 is face value; the original passes this down from the caller
    /// and applies it as a final <c>total * pct / 100</c>, so the rounding is a single truncation at
    /// the end rather than per condition.
    /// </param>
    /// <returns>Zero when the character needs nothing — which callers use as exactly that test.</returns>
    public static long Price(ActorConditions conditions, int multiplierPercent) {
        if (conditions == null) {
            return 0;
        }

        long total = 0;
        for (var i = 0; i < ActorConditions.Count; i++) {
            var condition = (ActorCondition)i;
            if (condition == FreeCondition) {
                continue;
            }
            int rank = conditions[condition];
            if (rank == 0) {
                continue;
            }
            total += (rank * RatePerRank[i]) + PerConditionFee;
        }
        return total * multiplierPercent / 100;
    }

    /// <summary>
    /// Whether a temple has anything to offer this character, which the original asks by pricing
    /// them and comparing against zero.
    /// </summary>
    public static bool NeedsHealing(ActorConditions conditions) => Price(conditions, 100) != 0;
}
