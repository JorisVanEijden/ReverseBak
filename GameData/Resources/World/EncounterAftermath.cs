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
