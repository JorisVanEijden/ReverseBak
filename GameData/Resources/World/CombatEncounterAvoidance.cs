namespace GameData.Resources.World;

/// <summary>
/// Whether a combat encounter can be walked past — <c>combTrigger_phase2</c> (ovr187 @0x7409d),
/// the block between loading the DEF_COMB record and starting the fight.
/// </summary>
/// <remarks>
/// <b>Two separate questions, and the same flag answers them differently.</b> First, is the party
/// even allowed to try? Then, if so, how likely is it to work? <see cref="DefCombEntry.Avoidable"/>
/// gates the first and modifies the second, and Dragon's Breath does the same in mirror image — so
/// a rule written as one combined chance gets both halves wrong.
/// </remarks>
public static class CombatEncounterAvoidance {
    /// <summary>The stat the whole thing turns on.</summary>
    public const ActorAttribute Stat = ActorAttribute.Stealth;

    /// <summary>The ceiling the stat bonus is capped at, and the threshold that gates it.</summary>
    public const int BonusCeiling = 90;

    /// <summary>Percent of the stat added as a bonus, below the ceiling.</summary>
    public const int BonusPercent = 30;

    /// <summary>Stealth gained by the whole party for a successful evasion.</summary>
    public const int TrainingOnSuccess = 1;

    /// <summary>
    /// Whether the party gets to roll at all.
    /// </summary>
    /// <param name="avoidable">The encounter's own flag.</param>
    /// <param name="scouted">Whether the party has spotted it.</param>
    /// <param name="dragonsBreathActive">Whether the fog spell is running.</param>
    /// <remarks>
    /// <b>AN AVOIDABLE ENCOUNTER STILL HAS TO HAVE BEEN SPOTTED.</b> Marked avoidable and unscouted,
    /// the party walks into it — the flag is permission to try, not a free pass. Reading it as
    /// "avoidable means you can sneak past" makes 62 of the shipped encounters skippable that are
    /// not.
    ///
    /// <para><b>And Dragon's Breath opens the gate on encounters that are NOT avoidable</b>, where
    /// scouting does nothing. So the two routes into the roll are disjoint: scouting works only on
    /// flagged encounters, the fog only on unflagged ones. Neither is a general "avoid" mechanic.
    /// </para>
    /// </remarks>
    public static bool MayAttempt(bool avoidable, bool scouted, bool dragonsBreathActive) =>
        avoidable ? scouted : dragonsBreathActive;

    /// <summary>
    /// The chance of slipping past, as a percentage.
    /// </summary>
    /// <param name="bestPartyStealth">The highest Stealth in the party.</param>
    /// <remarks>
    /// <b>Ninety caps the BONUS, not the chance.</b> The test is on the raw stat, so a party
    /// already at ninety or above gets no bonus at all and its chance is simply its stat — a
    /// Stealth of 95 gives 95. What is clamped is the bonused result of a stat BELOW ninety, which
    /// can therefore never be lifted past it. Treating ninety as a ceiling on the answer would
    /// quietly cap the best sneaks in the game.
    ///
    /// <para><b>DRAGON'S BREATH ADDS ITS BONUS ONLY TO AN AVOIDABLE ENCOUNTER</b>, which is the
    /// mirror of the gate: on an unflagged encounter the fog is what lets the party roll, and it
    /// contributes nothing to the roll it just unlocked. Applying it in both cases hands the fog a
    /// bonus exactly where the original gives none.</para>
    ///
    /// <para>That bonus is half the distance left to certainty, so it is worth most to a poor
    /// sneak — and it is the only thing that can lift a sub-ninety stat past ninety.</para>
    /// </remarks>
    public static int Chance(int bestPartyStealth, bool avoidable, bool dragonsBreathActive) {
        int chance = bestPartyStealth;
        if (chance < BonusCeiling) {
            chance += chance * BonusPercent / 100;
            if (chance > BonusCeiling) {
                chance = BonusCeiling;
            }
        }

        if (avoidable && dragonsBreathActive) {
            chance += (100 - chance) / 2;
        }

        return chance;
    }

    /// <summary>
    /// Whether a roll evades the encounter.
    /// </summary>
    /// <remarks>
    /// <b>Inclusive:</b> a roll equal to the chance still gets past. The original compares with
    /// <c>ja</c> and fights only on a strictly greater roll.
    /// </remarks>
    public static bool Evades(int rollUnder100, int chance) => rollUnder100 <= chance;

    /// <summary>
    /// <b>A whitelisted encounter skips the whole block.</b>
    /// </summary>
    /// <remarks>
    /// <c>isEncounterIdWhitelisted</c> is tested before anything else, and a hit jumps straight to
    /// the fight — past the flag, past scouting, past the fog and past the roll. So there are
    /// encounters no amount of Stealth avoids, and they are chosen by id rather than by any
    /// property of the record.
    /// </remarks>
    public static bool AvoidanceIsSkipped(bool encounterIsWhitelisted) => encounterIsWhitelisted;
}
