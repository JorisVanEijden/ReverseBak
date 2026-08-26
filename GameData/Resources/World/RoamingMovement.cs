namespace GameData.Resources.World;

/// <summary>
/// How a roaming encounter actor wanders the overworld before you engage it —
/// <c>updateRoamingEncounterActors</c> (IDA ovr188 @0x7600b), which is
/// <c>rgnenc_corpse_tbl_iterate_22byte</c> in the reconstructed C (RGNENC.C:677).
///
/// <para><b>CORRECTION: this DOES exist in canassa, under a name that describes nothing it does.</b>
/// An earlier note here said no counterpart existed and sent readers to the disassembly. The
/// routine iterates fixed-object entries and steps every roaming actor; there is no corpse table
/// and no 22-byte anything in it. It was missed by grepping for the behaviour rather than reading
/// the module — the same way <c>combatenc_corpse_tbl_spawn_actor</c> (which spawns a roster actor
/// and is not about corpses either) was misfiled. The disassembly-derived model below was checked
/// against the C line by line and needed no correction; what the C added is recorded here.</para>
///
/// <para><b>One more misname in the same module, worth knowing before trusting a gate:</b>
/// <c>rgnenc_slot_actor_kind_eq_placed</c> returns <c>kind == 3</c>, which is
/// <see cref="RoamingKind"/> — <b>not</b> placed/standing, which is 4. A renderer or updater that
/// took the name at face value would gate on exactly the wrong half of the actors.</para>
///
/// <para>Each tick, an actor takes one fixed step along its current heading and then turns only if
/// it has arrived at a waypoint. The waypoints are the slot's alternate spawn points, reused as a
/// patrol route.</para>
/// </summary>
public static class RoamingMovement {
    /// <summary>
    /// World units an actor advances per tick. Fixed — there is no speed per creature.
    /// </summary>
    /// <remarks>
    /// <b>It is exactly a QUARTER of a road cell, and that is what makes the waypoint test work.</b>
    /// <c>RoadTravel.CellSize</c> is 0x640 and this is 0x190; the waypoints sit on the cell lattice,
    /// so four steps land an actor precisely on the next one. Change the step — to a per-creature
    /// speed, to a frame-rate-scaled float — and the exact-equality arrival test
    /// (<see cref="IsAtWaypoint"/>) stops firing, and every patrol walks off in a straight line for
    /// ever. Expressed against the cell size so the two cannot drift apart.
    /// </remarks>
    public const int StepDistance = RoadTravel.CellSize / 4;   // 0x190

    /// <summary>Half a turn in the 16-bit angle unit; the about-face.</summary>
    public const int HalfTurn = 0x8000;

    /// <summary>Quarter turn, the circuit patterns' corner.</summary>
    public const int QuarterTurn = 0x4000;

    /// <summary>
    /// The actor kind that roams. <b>Only the walking kind moves</b> — see
    /// <see cref="EncounterActorPose.WalkingKind"/>. Standing actors are drawn and never updated, so
    /// a movement pattern set on one has no effect at all.
    /// </summary>
    public const int RoamingKind = EncounterActorPose.WalkingKind;

    /// <summary>What an actor does with its waypoints.</summary>
    public enum Pattern {
        /// <summary>Never moves. The only pattern that takes no step.</summary>
        Stationary = 0,

        /// <summary>Walks between two waypoints, about-facing at each.</summary>
        BackAndForth = 1,

        /// <summary>Four waypoints, turning by <b>minus</b> a quarter turn at each.</summary>
        CircuitTurningNegative = 2,

        /// <summary>Four waypoints, turning by <b>plus</b> a quarter turn at each.</summary>
        CircuitTurningPositive = 3,

        /// <summary>Follows the road, about-facing at either end of a two-waypoint route.</summary>
        RoadFollowing = 4,
    }

    /// <summary>
    /// How many of the slot's alternate spawn points act as waypoints.
    /// </summary>
    /// <remarks>
    /// The circuits read all four; the two-ended patterns read only the first two, so a slot may
    /// carry waypoints its pattern never looks at.
    /// </remarks>
    public static int WaypointCount(Pattern pattern) => pattern switch {
        Pattern.BackAndForth => 2,
        Pattern.CircuitTurningNegative => 4,
        Pattern.CircuitTurningPositive => 4,
        Pattern.RoadFollowing => 2,
        _ => 0,
    };

    /// <summary>
    /// The yaw added when the actor reaches a waypoint.
    /// </summary>
    /// <remarks>
    /// The sign is the original's, in its 16-bit angle unit; which way that appears on screen depends
    /// on the yaw convention and is not settled here.
    /// </remarks>
    public static int TurnOnReach(Pattern pattern) => pattern switch {
        Pattern.BackAndForth => HalfTurn,
        Pattern.CircuitTurningNegative => -QuarterTurn,
        Pattern.CircuitTurningPositive => QuarterTurn,
        Pattern.RoadFollowing => HalfTurn,
        _ => 0,
    };

    /// <summary>Whether this pattern takes a step at all.</summary>
    public static bool Moves(Pattern pattern) => WaypointCount(pattern) > 0;

    /// <summary>
    /// The offset one tick's step applies — <b>the same axis-offset routine road travel uses</b>,
    /// at a quarter of its distance.
    /// </summary>
    /// <remarks>
    /// Both the actor updater and the party's travel call
    /// <c>worldmove_crossing_apply_offset</c>, so a roaming actor moves on exactly the same integer
    /// lattice the party does. Diagonals move the full delta on <b>both</b> axes rather than being
    /// resolved with trigonometry — see <see cref="RoadTravel.AxisOffset"/>, where that is spelled
    /// out; restating it here would be the second copy.
    /// </remarks>
    public static (int Dx, int Dy) Step(ushort heading) =>
        RoadTravel.AxisOffset(heading, StepDistance);

    /// <summary>
    /// <b>Whether the step can be REFUSED — and for four of the five patterns it cannot.</b>
    /// </summary>
    /// <remarks>
    /// Patterns 1-3 apply the offset outright, with no walkability test of any kind: the actor is
    /// moved and only then asked whether it has landed on a waypoint. <b>Only
    /// <see cref="Pattern.RoadFollowing"/> probes first</b>, because it is the one that has to stay
    /// on a road, and a probe that fails turns it round (<see cref="BlockedTurn"/>) rather than
    /// stopping it.
    ///
    /// <para>So a patrolling monster walks through whatever is in its way. That reads as an
    /// oversight and a port is tempted to "fix" it by running patrols through the party's collision
    /// — which would strand them the first time an authored route clipped scenery, in a game where
    /// they simply walk on. It is a decision to take deliberately, not a bug to correct silently.
    /// </para>
    /// </remarks>
    public static bool StepCanBeBlocked(Pattern pattern) => pattern == Pattern.RoadFollowing;

    /// <summary>Whether a raw slot value is a pattern the updater acts on.</summary>
    public static bool IsKnown(int rawPattern) =>
        rawPattern >= (int)Pattern.BackAndForth && rawPattern <= (int)Pattern.RoadFollowing;

    /// <summary>
    /// Whether the actor has arrived at a waypoint.
    /// </summary>
    /// <remarks>
    /// <b>The test is exact equality on both axes, on integers.</b> Not a radius, not a tolerance —
    /// the actor turns only when its position lands precisely on the waypoint. That works only
    /// because the step is a fixed <see cref="StepDistance"/> and the routes were authored on that
    /// lattice, so a port that moves these actors in floating point, or with any other step size,
    /// gets monsters that miss every waypoint and walk off in a straight line forever. It is the
    /// single most breakable thing in this whole update.
    /// </remarks>
    public static bool IsAtWaypoint(int x, int y, int waypointX, int waypointY) =>
        x == waypointX && y == waypointY;

    /// <summary>
    /// The yaw added when road-following cannot take the step — the way is blocked, so the actor
    /// turns around rather than stopping.
    /// </summary>
    public const int BlockedTurn = HalfTurn;

    /// <summary>
    /// What the road sweep reported after a <see cref="Pattern.RoadFollowing"/> step.
    /// </summary>
    /// <remarks>
    /// The raw values are <c>worldmove_sweep_adjacent_cells</c>'s return codes, kept rather than
    /// renumbered because <see cref="AdoptsRoadHeading"/> is written against them.
    /// </remarks>
    public enum RoadOutcome {
        /// <summary>
        /// Nothing to follow — <b>or a fork, which is indistinguishable from it</b>.
        /// </summary>
        /// <remarks>
        /// The sweep returns 0 both when it finds no continuation and when it finds a second one.
        /// For the party those are the same outcome (travel stops); for a roaming actor they are
        /// too — it keeps its heading and walks on. Nothing here can report which happened, and
        /// <see cref="RoadSweep"/> separates them only for anything that wants to say WHY.
        /// </remarks>
        NoneOrForked = 0,

        /// <summary>
        /// The road runs straight on. <b>The heading is not changed</b> — it is already right.
        /// </summary>
        /// <remarks>
        /// The original returns its <c>mode</c> argument here, which the actor updater passes as 1.
        /// The reported target equals the current heading in this case, so excluding it from
        /// <see cref="AdoptsRoadHeading"/> is a no-op rather than a difference — worth stating,
        /// because it is otherwise the one code whose exclusion looks arbitrary.
        /// </remarks>
        StraightOn = 1,

        /// <summary>The road bends the way the sweep widens first.</summary>
        BendsOneWay = 2,

        /// <summary>The road bends the other way.</summary>
        BendsTheOther = 3,
    }

    /// <summary>
    /// Whether a road-following step's reported outcome means "the road bent here, take its
    /// heading".
    /// </summary>
    /// <remarks>
    /// Only consulted when the actor did <b>not</b> land on one of its two waypoints: arriving at an
    /// end of the route about-faces, and that decision wins over the road's own heading.
    /// </remarks>
    public static bool AdoptsRoadHeading(int stepOutcome) =>
        stepOutcome == (int)RoadOutcome.BendsOneWay
        || stepOutcome == (int)RoadOutcome.BendsTheOther;

    /// <summary>
    /// <b>The road is only ever consulted at a cell CENTRE.</b>
    /// </summary>
    /// <remarks>
    /// <c>worldmove_crossing_check_8dir</c> runs the sweep only when the position it just stepped to
    /// has both coordinates exactly half a cell into their cell; anywhere else it reports
    /// <see cref="RoadOutcome.NoneOrForked"/> without probing. So a road-follower ignores bends for
    /// three ticks out of four (<see cref="StepDistance"/> being a quarter cell) and takes them on
    /// the fourth — which is also why it can only ever turn where roads actually meet.
    /// </remarks>
    public static bool ConsidersTheRoadAt(int x, int y) =>
        Mod(x, RoadTravel.CellSize) == RoadTravel.HalfCell
        && Mod(y, RoadTravel.CellSize) == RoadTravel.HalfCell;

    // ---- one tick ---------------------------------------------------------------------------------

    /// <summary>Where a road-following step ended up, and what the sweep saw.</summary>
    public readonly struct RoadStep {
        public RoadStep(bool moved, long x, long y, int outcome = 0, ushort target = 0) {
            Moved = moved;
            X = x;
            Y = y;
            Outcome = outcome;
            Target = target;
        }

        /// <summary>False when the way was blocked — the actor turns around instead of moving.</summary>
        public bool Moved { get; }

        public long X { get; }

        public long Y { get; }

        /// <summary>A <see cref="RoadOutcome"/>.</summary>
        public int Outcome { get; }

        /// <summary>The heading the sweep reported, meaningful only for the two bends.</summary>
        public ushort Target { get; }
    }

    /// <summary>One actor's state between ticks.</summary>
    public readonly struct Pose {
        public Pose(long x, long y, ushort heading) {
            X = x;
            Y = y;
            Heading = heading;
        }

        public long X { get; }

        public long Y { get; }

        public ushort Heading { get; }
    }

    /// <summary>
    /// Advances one roaming actor by a tick — the body of
    /// <c>rgnenc_corpse_tbl_iterate_22byte</c> (RGNENC.C:677-760).
    /// </summary>
    /// <param name="pose">Where the actor is now.</param>
    /// <param name="pattern">Its route.</param>
    /// <param name="waypointX">The route's waypoints, <b>in world coordinates</b>.</param>
    /// <param name="waypointY"><inheritdoc cref="Tick" path="/param[@name='waypointX']"/></param>
    /// <param name="roadStep">
    /// Takes the road-following step, or reports that it was refused. Only
    /// <see cref="Pattern.RoadFollowing"/> calls it; null refuses every step, which turns such an
    /// actor on the spot rather than letting it walk through the world unchecked.
    /// </param>
    /// <remarks>
    /// <b>There is no current-waypoint index.</b> The actor steps and then compares its new position
    /// against EVERY waypoint of its route, turning on the first match. That is what makes exact
    /// equality workable, and it means nothing has to be remembered between ticks beyond the pose.
    ///
    /// <para><b>Patterns 1-3 take their step unconditionally</b> — no walkability test of any kind,
    /// so a patrolling monster walks through whatever is in its way. That reads as an oversight and
    /// invites a port to run patrols through the party's collision, which would strand them wherever
    /// an authored route clips scenery. Faithful here, and a decision to revisit deliberately rather
    /// than by accident.</para>
    ///
    /// <para><b>Arriving beats the road.</b> A road-follower that lands on an end of its route
    /// about-faces, and only an actor that landed on neither consults the sweep's heading.</para>
    /// </remarks>
    public static Pose Tick(Pose pose, Pattern pattern,
        System.Collections.Generic.IReadOnlyList<long> waypointX,
        System.Collections.Generic.IReadOnlyList<long> waypointY,
        System.Func<Pose, RoadStep> roadStep = null) {
        if (!Moves(pattern)) {
            return pose;
        }

        if (pattern != Pattern.RoadFollowing) {
            (int dx, int dy) = Step(pose.Heading);
            var moved = new Pose(pose.X + dx, pose.Y + dy, pose.Heading);
            return ArrivedAt(moved, waypointX, waypointY, WaypointCount(pattern))
                ? Turned(moved, TurnOnReach(pattern))
                : moved;
        }

        RoadStep step = roadStep?.Invoke(pose) ?? new RoadStep(false, pose.X, pose.Y);
        if (!step.Moved) {
            return Turned(pose, BlockedTurn);
        }

        var after = new Pose(step.X, step.Y, pose.Heading);
        if (ArrivedAt(after, waypointX, waypointY, WaypointCount(pattern))) {
            return Turned(after, TurnOnReach(pattern));
        }
        return AdoptsRoadHeading(step.Outcome)
            ? new Pose(after.X, after.Y, step.Target)
            : after;
    }

    private static bool ArrivedAt(Pose pose, System.Collections.Generic.IReadOnlyList<long> xs,
        System.Collections.Generic.IReadOnlyList<long> ys, int count) {
        if (xs == null || ys == null) {
            return false;
        }
        for (var i = 0; i < count && i < xs.Count && i < ys.Count; i++) {
            if (pose.X == xs[i] && pose.Y == ys[i]) {
                return true;
            }
        }
        return false;
    }

    private static Pose Turned(Pose pose, int delta) =>
        new Pose(pose.X, pose.Y, unchecked((ushort)(pose.Heading + delta)));

    private static int Mod(int value, int m) {
        int r = value % m;
        return r < 0 ? r + m : r;
    }
}
