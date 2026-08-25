namespace GameData.Resources.World;

/// <summary>
/// What happens when a combat encounter ends — <c>combTrigger_phase2</c>'s tail (ovr187 @0x7409d).
/// </summary>
/// <remarks>
/// The arena reports one of four outcomes and each does something different, so a port that treats
/// "the fight ended" as one case gets three of them wrong.
/// </remarks>
public static class EncounterAftermath {
    /// <summary>What the arena reports back.</summary>
    /// <remarks>
    /// <b>Only two of the four do anything, and they do opposite things.</b> The numbering is the
    /// original's, since it is what the arena writes.
    /// </remarks>
    public enum Outcome {
        /// <summary>Nothing to settle — no events, no move, not even a redraw.</summary>
        Nothing = 0,

        /// <summary>Resolved: the encounter's post event fires and it is marked as fought.</summary>
        Resolved = 1,

        /// <summary>The party left: it is relocated to one of the record's landings.</summary>
        PartyMoved = 2,

        /// <summary>Reported and handled like <see cref="Nothing"/>.</summary>
        Unhandled = 3,
    }

    /// <summary>
    /// <b>The encounter's FOUGHT time is stamped whatever the outcome.</b>
    /// </summary>
    /// <remarks>
    /// The write happens before the outcome is even examined, so an encounter that reports
    /// <see cref="Outcome.Nothing"/> is still recorded as fought at this moment. Stamping it only on
    /// a win would leave the other outcomes looking like the fight never happened.
    ///
    /// <para>Note this is the FOUGHT stamp, not the visit stamp
    /// <see cref="CombatEncounterOpening.WasRecentlyVisited"/> reads — they are separate tables.</para>
    /// </remarks>
    public static bool FoughtTimeIsStampedRegardless => true;

    /// <summary>
    /// Which outcome a finished fight reports.
    /// </summary>
    /// <param name="enemiesAlive">Enemies still standing.</param>
    /// <param name="partyAlive">Party members still standing.</param>
    /// <param name="partyFled">Whether the party left rather than finished the fight.</param>
    /// <remarks>
    /// <b>"The fight ended" is not one case, and the difference decides whether the encounter ever
    /// fires again.</b> Only a WIN is <see cref="Outcome.Resolved"/>: a flight relocates the party
    /// and marks nothing, and a wipe marks nothing either — treating either as resolved clears an
    /// ambush the party did not beat.
    ///
    /// <para><b>The flight answer wins over the roster.</b> A party that runs from a fight it was
    /// winning has still not resolved it, so the flag is asked first rather than inferred from who
    /// is left standing.</para>
    /// </remarks>
    public static Outcome OutcomeFor(int enemiesAlive, int partyAlive, bool partyFled = false) {
        if (partyFled) {
            return Outcome.PartyMoved;
        }
        if (partyAlive <= 0) {
            return Outcome.Nothing;
        }
        return enemiesAlive <= 0 ? Outcome.Resolved : Outcome.Nothing;
    }

    /// <summary>Whether the party is relocated.</summary>
    public static bool RelocatesTheParty(Outcome outcome) => outcome == Outcome.PartyMoved;

    /// <summary>Whether the encounter's post event fires and it is marked as fought.</summary>
    public static bool FiresThePostEvent(Outcome outcome) => outcome == Outcome.Resolved;

    /// <summary>
    /// Whether the scene and the map are rebuilt afterwards.
    /// </summary>
    /// <remarks>
    /// <b>The two outcomes that changed something, and no others.</b> Reloading unconditionally
    /// costs a rebuild after every trivial outcome; never reloading leaves a defeated encounter
    /// still standing in the world.
    /// </remarks>
    public static bool ReloadsSceneAndMap(Outcome outcome) =>
        outcome != Outcome.Nothing && outcome != Outcome.Unhandled;

    // ---------------------------------------------------------------- where the party lands

    /// <summary>
    /// Which side of the encounter's box the party finished on — <c>worldmove_aabb_outcode_rotated</c>
    /// (WORLDMOV.C:192). The answer feeds <see cref="LandingFor"/>.
    /// </summary>
    /// <param name="partyTileX">The party's tile, which is also the base for the box's cells.</param>
    /// <param name="partyTileY"><inheritdoc cref="ApproachDirection" path="/param[@name='partyTileX']"/></param>
    /// <param name="partyWorldX">The party's absolute world position.</param>
    /// <param name="partyWorldY"><inheritdoc cref="ApproachDirection" path="/param[@name='partyWorldX']"/></param>
    /// <param name="boxStartX">The box's four bytes <b>in their on-disk order</b> — see the remarks.</param>
    /// <param name="boxEndY"><inheritdoc cref="ApproachDirection" path="/param[@name='boxStartX']"/></param>
    /// <param name="boxEndX"><inheritdoc cref="ApproachDirection" path="/param[@name='boxStartX']"/></param>
    /// <param name="boxStartY"><inheritdoc cref="ApproachDirection" path="/param[@name='boxStartX']"/></param>
    /// <returns>1, 2, 4 or 8 — never any other value.</returns>
    /// <remarks>
    /// <b>The box is stored minX, maxY, maxX, minY — NOT min, min, max, max.</b> The original walks
    /// it through a <c>unsigned char*</c> by index, so the on-disk order IS the meaning, and reading
    /// it as a conventional (min, min, max, max) rectangle swaps the two Y bounds and inverts the
    /// answer for every party above or below the box. <see cref="TileEventTrigger"/> stores the same
    /// four bytes in the same order under the names this signature uses, so passing its fields
    /// straight across is correct and re-sorting them is not.
    ///
    /// <para><b>What each answer means, stated against the box rather than the compass:</b></para>
    /// <list type="table">
    ///   <item><term>1</term><description>past the box's max-Y edge.</description></item>
    ///   <item><term>2</term><description>short of its min-X edge.</description></item>
    ///   <item><term>4</term><description>short of its min-Y edge.</description></item>
    ///   <item><term>8</term><description>anything else — the +X side, and also INSIDE the box.</description></item>
    /// </list>
    /// <para>So 1 and 4 are the two Y answers and 2 and 8 the two X answers. Which of those reads as
    /// north or east is a compass convention this routine does not establish, and guessing it is how
    /// a fleeing party gets thrown to the opposite side of the encounter.</para>
    ///
    /// <para><b>The two min edges are tested one cell in, the max edge is not.</b> The min-X and
    /// min-Y comparisons use <c>+ 1</c> and the max-Y comparison uses the stored byte, so a party
    /// standing exactly on the min-X column counts as 2 while one standing exactly on the max-Y row
    /// does not count as 1. The asymmetry is the original's; making it symmetric moves the boundary
    /// by a cell on three of the four sides.</para>
    ///
    /// <para><b>Only the second corner's Y is used.</b> The original computes its X and never reads
    /// it, so there is no fourth comparison to add — a port that tested both axes of both corners
    /// would be inventing a rule.</para>
    /// </remarks>
    public static int ApproachDirection(int partyTileX, int partyTileY,
        long partyWorldX, long partyWorldY,
        int boxStartX, int boxEndY, int boxEndX, int boxStartY) {
        if (WorldPlacement.CornerOf(partyTileY, boxEndY) < partyWorldY) {
            return 1;
        }
        if (partyWorldX < WorldPlacement.CornerOf(partyTileX, boxStartX + 1)) {
            return 2;
        }
        return partyWorldY < WorldPlacement.CornerOf(partyTileY, boxStartY + 1) ? 4 : 8;
    }

    /// <inheritdoc cref="ApproachDirection(int, int, long, long, int, int, int, int)"/>
    /// <param name="trigger">The hotspot whose box the party is being placed against.</param>
    /// <param name="partyTileX"><inheritdoc cref="ApproachDirection(int, int, long, long, int, int, int, int)" path="/param[@name='partyTileX']"/></param>
    /// <param name="partyTileY"><inheritdoc cref="ApproachDirection(int, int, long, long, int, int, int, int)" path="/param[@name='partyTileX']"/></param>
    /// <param name="partyWorldX"><inheritdoc cref="ApproachDirection(int, int, long, long, int, int, int, int)" path="/param[@name='partyWorldX']"/></param>
    /// <param name="partyWorldY"><inheritdoc cref="ApproachDirection(int, int, long, long, int, int, int, int)" path="/param[@name='partyWorldX']"/></param>
    /// <remarks>
    /// Prefer this over the eight-argument form: the box's order is the trap, and here it cannot be
    /// got wrong.
    /// </remarks>
    public static int ApproachDirection(TileEventTrigger trigger,
        int partyTileX, int partyTileY, long partyWorldX, long partyWorldY) =>
        trigger == null
            ? 1
            : ApproachDirection(partyTileX, partyTileY, partyWorldX, partyWorldY,
                trigger.StartX, trigger.EndY, trigger.EndX, trigger.StartY);

    /// <summary>Which of the record's four landings an approach direction selects.</summary>
    /// <remarks>
    /// <b>Only three directions have their own landing; the other five share the first.</b> The
    /// switch has arms for 2, 4 and 8 and everything else — 1, 3, 5, 6, 7 — falls to the default,
    /// which is the same entry direction 1 would have used. Confirmed against the jump table at
    /// 0x7430c rather than inferred from the four fields existing.
    ///
    /// <para><b>And the direction is read AFTER the fight, not before it.</b> It is measured from
    /// the party's position at that moment, so where they end up depends on which side of the
    /// encounter they finished on.</para>
    /// </remarks>
    public static Landing LandingFor(int approachDirection) {
        switch (approachDirection) {
            case 2: return Landing.Direction2;
            case 4: return Landing.Direction4;
            case 8: return Landing.Direction8;
            default: return Landing.Direction1;
        }
    }

    /// <summary>One of <c>DEF_COMB</c>'s four landing entries.</summary>
    public enum Landing {
        /// <summary>Also the default, for directions 1, 3, 5, 6 and 7.</summary>
        Direction1,
        Direction2,
        Direction4,
        Direction8,
    }

    /// <summary>
    /// A landing's world coordinate.
    /// </summary>
    /// <remarks>
    /// <b>THE STORED VALUES ARE OFFSETS INSIDE A TILE, NOT WORLD POSITIONS</b>, and the tile is the
    /// one the party is standing in when the fight ends — not the encounter's. Treating them as
    /// absolute would drop the party at the same handful of coordinates near the world origin
    /// whatever map they were on.
    /// </remarks>
    public static long WorldCoordinate(int tile, int fineOffset) =>
        ((long)tile * WorldPlacement.TileSize) + fineOffset;
}
