namespace GameData.Resources.World;

/// <summary>
/// How a fight starts once it is going to happen — <c>combTrigger_phase2</c> (ovr187 @0x7409d),
/// the block after the avoidance roll and before the arena.
/// </summary>
/// <remarks>
/// <b>A SECOND STEALTH ROLL, ON DIFFERENT TERMS FROM THE FIRST.</b> The same function has already
/// rolled Stealth once to decide whether the party walks past
/// (<see cref="CombatEncounterAvoidance"/>). This one decides who gets the drop, and it uses the
/// RAW best Stealth — no thirty-percent bonus, no ceiling, no Dragon's Breath. Reusing the
/// avoidance chance here makes surprises far commoner than the game grants them.
/// </remarks>
public static class CombatEncounterOpening {
    /// <summary>Which of three situations the fight opens in — the dialog reads this.</summary>
    public enum Opening {
        /// <summary>The party got the drop. The arena is told, and Stealth is trained.</summary>
        PartySurprises = 0,

        /// <summary>Recently here, but the roll failed.</summary>
        NoSurprise = 1,

        /// <summary>Not here recently — the fight opens on level terms.</summary>
        NotRecent = 2,
    }

    /// <summary>Clock units in a minute — the unit the recency test divides into.</summary>
    public const int TicksPerMinute = 30;

    /// <summary>
    /// Minutes within which a previous visit still counts.
    /// </summary>
    /// <remarks>
    /// The comparison is <c>&gt;</c>, so exactly thirty minutes still counts as recent.
    /// </remarks>
    public const int RecencyMinutes = 30;

    /// <summary>
    /// Whether the party was near this encounter recently enough for a surprise to be possible.
    /// </summary>
    /// <param name="visitedTime">
    /// The encounter's stored visit time. <b>Nothing in the combat path writes it</b> — it is
    /// written when the player clicks the encounter in the world (the same stamp that gates the
    /// "you have already spoken to this one" reply), so a surprise is only ever available on an
    /// encounter the party has looked at. A separate stamp records when it was last FOUGHT.
    /// </param>
    /// <remarks>
    /// <b>AN UNVISITED ENCOUNTER READS ZERO, WHICH IS NOT "LONG AGO" AT THE START OF THE GAME.</b>
    /// With no visit recorded the elapsed time is the game clock itself, so during the first half
    /// hour of play every encounter passes the recency test and can be surprised. That is the
    /// original's behaviour, not a rounding artefact — the slot is genuinely zero and the
    /// subtraction genuinely small.
    ///
    /// <para>The division is unsigned in the original, so a visit stamped in the future — which a
    /// hand-edited save can produce — wraps to an enormous elapsed time and reads as "long ago"
    /// rather than as "very recent".</para>
    /// </remarks>
    public static bool WasRecentlyVisited(long gameTime, long visitedTime) {
        ulong elapsed = unchecked((ulong)(gameTime - visitedTime));

        return elapsed / TicksPerMinute <= RecencyMinutes;
    }

    /// <summary>
    /// The opening, given the recency and a d100.
    /// </summary>
    /// <param name="rollUnder100">The roll; <b>inclusive</b>, so a roll equal to the stat succeeds.</param>
    /// <param name="bestPartyStealth">The party's highest Stealth, used RAW.</param>
    public static Opening Resolve(bool recentlyVisited, int rollUnder100, int bestPartyStealth) {
        if (!recentlyVisited) {
            return Opening.NotRecent;
        }

        return rollUnder100 <= bestPartyStealth ? Opening.PartySurprises : Opening.NoSurprise;
    }

    /// <summary>Whether the arena is told the party opened with the advantage.</summary>
    /// <remarks>
    /// Only the surprise passes it on; both of the other openings start the fight level. The same
    /// flag is what the avoidance roll would have set had it succeeded — but a successful avoidance
    /// returns instead, so the two never both apply.
    /// </remarks>
    public static bool PartyHasTheDrop(Opening opening) => opening == Opening.PartySurprises;

    /// <summary>Stealth gained by the whole party for winning the surprise roll.</summary>
    /// <remarks>The same training the avoidance roll gives, for the same stat.</remarks>
    public const int TrainingOnSurprise = 1;

    /// <summary>
    /// <b>The creature the dialog names is the FIRST enemy slot's, not the encounter's.</b>
    /// </summary>
    /// <remarks>
    /// Just before the dialog plays, the record's first <c>EnemySlot</c> creature number is
    /// published to the global the dialog reads. An encounter mixing creature types therefore
    /// announces itself by whichever one the data lists first — which is a property of the slot
    /// order, not of the fight.
    /// </remarks>
    public static bool DialogNamesTheFirstSlotsCreature => true;
}
