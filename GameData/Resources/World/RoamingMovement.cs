namespace GameData.Resources.World;

/// <summary>
/// How a roaming encounter actor wanders the overworld before you engage it — IDA
/// <c>updateRoamingEncounterActors</c> (ovr188 @0x7600b). No counterpart exists in the reconstructed
/// C source; this is from the disassembly.
///
/// <para>Each tick, an actor takes one fixed step along its current heading and then turns only if
/// it has arrived at a waypoint. The waypoints are the slot's alternate spawn points, reused as a
/// patrol route.</para>
/// </summary>
public static class RoamingMovement {
    /// <summary>
    /// World units an actor advances per tick. Fixed — there is no speed per creature.
    /// </summary>
    public const int StepDistance = 400;

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
    /// Whether a road-following step's reported outcome means "the road bent here, take its
    /// heading".
    /// </summary>
    /// <remarks>
    /// Only consulted when the actor did <b>not</b> land on one of its two waypoints: arriving at an
    /// end of the route about-faces, and that decision wins over the road's own heading.
    /// </remarks>
    public static bool AdoptsRoadHeading(int stepOutcome) => stepOutcome == 2 || stepOutcome == 3;
}
