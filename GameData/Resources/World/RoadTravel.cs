namespace GameData.Resources.World;

// Moved from UnityProject/Assets/Scripts/World/RoadTravel.cs (TASK-202). It is pure RE
// knowledge about the road lattice — the 0x640 cell, the 8-compass gate, the per-heading
// lattice invariants, the three-sample diagonal probe — and none of it is presentation, so
// by the layering rule it belongs beside the other decoded world rules rather than in the
// consumer. Its absence from here is what led to a duplicate of its gate being written.
using System;
using System.Collections.Generic;

/// <summary>Why a continuation sweep ended.</summary>
public enum RoadSweep {
    /// <summary>No road continues from here; travel stops.</summary>
    None,

    /// <summary>Exactly one continuation — the party turns to follow it.</summary>
    Turn,

    /// <summary>Two continuations. The road forks and travel stalls.</summary>
    Fork,
}

/// <summary>
/// Road following ("travel" mode): the rules that let the party walk a road a cell at a time and
/// take its bends automatically.
///
/// <para>Ported from <c>worldmove_probe_adjacent_cell</c>, <c>worldmove_crossing_apply_offset</c>
/// and <c>worldmove_sweep_adjacent_cells</c> (<c>SRC/GAME/WORLD/WORLDMOV.C</c>), against
/// <c>docs/specs/collision-system.md</c> §3.5.</para>
///
/// <para>Pure and stateless: every world query goes through an <c>isRoadAt</c> delegate, so the
/// whole thing is testable without a zone. <see cref="Collision.ProximityWorld.TryScan"/> is what
/// supplies it in the game.</para>
///
/// <para>Not to be confused with <see cref="PartyMovement"/>'s wall-slide pivot: that widens by
/// 1° looking for <i>any</i> opening to get unstuck, while this steps in exact 45° increments
/// looking for the <i>road's</i> continuation and refuses to guess when there are two.</para>
/// </summary>
public static class RoadTravel {
    /// <summary>World units per grid cell.</summary>
    public const int CellSize = 0x640;   // 1600

    /// <summary>Half a cell — the lattice lines run through cell centres.</summary>
    public const int HalfCell = CellSize / 2;

    /// <summary>45° in BaK angle space, and the sweep's increment.</summary>
    public const int CompassStep = 0x2000;

    /// <summary>180° in BaK angle space.</summary>
    public const int HalfTurn = 0x8000;

    /// <summary>
    /// Terrain kinds that count as travellable road. 1 and 2 are road and bridge; the original
    /// tests exactly this pair in <c>worldmove_prox_query_at_cell</c> and nothing else qualifies.
    /// </summary>
    public static bool IsRoadKind(int kind) => kind == 1 || kind == 2;

    /// <summary>Whether a heading is exactly one of the eight compass directions.</summary>
    /// <remarks>
    /// The original gates the sweep on <c>(dir &gt;&gt; 13) &lt;&lt; 13 == dir</c>. This is why a
    /// party whose heading was nudged off the lattice — by the wall-slide pivot, say — simply
    /// cannot travel until it re-aligns.
    /// </remarks>
    public static bool IsCompassHeading(ushort heading) => heading % CompassStep == 0;

    /// <summary>Compass index 0..7 for a heading, where 0 is 0° and each step is 45°.</summary>
    public static int CompassIndex(ushort heading) => (heading / CompassStep) & 7;

    /// <summary>
    /// The offset a step of <paramref name="delta"/> in this direction applies.
    /// </summary>
    /// <remarks>
    /// <b>These are axis offsets, not trigonometry.</b> A diagonal moves the full delta on
    /// <i>both</i> axes, so it covers about 1.41× the ground an orthogonal step does. That is the
    /// original's <c>worldmove_crossing_apply_offset</c> exactly, and it is what keeps travel on
    /// an exact integer lattice — resolving the diagonals with sin/cos would drift off it.
    /// </remarks>
    public static (int Dx, int Dy) AxisOffset(ushort heading, int delta) {
        switch (CompassIndex(heading)) {
            case 0: return (0, delta);        // 0
            case 1: return (-delta, delta);   // 45
            case 2: return (-delta, 0);       // 90
            case 3: return (-delta, -delta);  // 135
            case 4: return (0, -delta);       // 180
            case 5: return (delta, -delta);   // 225
            case 6: return (delta, 0);        // 270
            default: return (delta, delta);   // 315
        }
    }

    /// <summary>
    /// Whether the party is standing on the lattice line its heading travels along. Each
    /// direction preserves a different invariant, and a step that would leave the lattice is
    /// refused rather than rounded.
    /// </summary>
    public static bool IsOnLatticeLine(ushort heading, int x, int y) {
        int mx = Mod(x, CellSize);
        int my = Mod(y, CellSize);
        switch (CompassIndex(heading)) {
            case 0:
            case 4:
                return mx == HalfCell;                 // moving along y only
            case 2:
            case 6:
                return my == HalfCell;                 // moving along x only
            case 3:
            case 7:
                return mx == my;                       // (±d, ±d) preserves mx - my
            default:
                // (∓d, ±d) preserves mx + my. Both-zero is the corner case the original allows.
                return (mx + my == CellSize) || (mx == 0 && my == 0);
        }
    }

    /// <summary>
    /// Whether the cell one step along <paramref name="heading"/> is road.
    /// </summary>
    /// <param name="isRoadAt">Reports whether a world position stands on road or bridge.</param>
    /// <remarks>
    /// An orthogonal direction needs the single sample a cell ahead. A <b>diagonal needs three</b>:
    /// the cell ahead, then two more taken from the ORIGINAL position — an offset of
    /// (±0x32a, ±0x316) and then a further (±0x14, ±0x14) nudge from there. Both extra samples are
    /// measured from where the party stands, not chained off the first probe. Dropping them lets
    /// the party cut diagonally across a road's corner where the original would refuse.
    /// </remarks>
    public static bool ProbeAdjacentCell(int x, int y, ushort heading, Func<int, int, bool> isRoadAt) {
        if (isRoadAt == null) {
            throw new ArgumentNullException(nameof(isRoadAt));
        }

        (int dx, int dy) = AxisOffset(heading, CellSize);
        if (!isRoadAt(x + dx, y + dy)) {
            return false;
        }

        int index = CompassIndex(heading);
        if ((index & 1) == 0) {
            return true; // orthogonal: one sample is enough
        }

        (int sx, int sy) = DiagonalSample(index);
        if (!isRoadAt(x + sx, y + sy)) {
            return false;
        }

        (int nx, int ny) = DiagonalNudge(index);
        return isRoadAt(x + sx + nx, y + sy + ny);
    }

    /// <summary>
    /// Looks for the road's continuation, sweeping ±45° at a time out to 180°.
    /// </summary>
    /// <param name="backward">The original's mode 4: sweep from the reversed heading.</param>
    /// <param name="target">The heading to turn to, when the result is <see cref="RoadSweep.Turn"/>.</param>
    /// <remarks>
    /// Straight ahead is probed first and wins outright. Otherwise the sweep widens clockwise and
    /// anticlockwise together, and a <b>second</b> hit anywhere means a fork — the party stops
    /// rather than picking a side.
    /// <para>The original returns 0 for both "nothing found" and "forked", so a caller cannot
    /// tell them apart; both stop travel, so behaviour is unchanged by reporting them separately
    /// here. The distinction is worth having for anything that wants to say <i>why</i> travel
    /// stopped.</para>
    /// </remarks>
    public static RoadSweep FindContinuation(
        int x, int y, ushort heading, bool backward, Func<int, int, bool> isRoadAt, out ushort target) {
        target = heading;
        if (!IsCompassHeading(heading)) {
            return RoadSweep.None;
        }

        int start = backward ? heading + HalfTurn : heading;
        int end = backward ? heading : heading + HalfTurn;
        int offset = backward ? HalfTurn : 0;

        int cw = start;
        int ccw = start;
        var found = false;

        while (Wrap(cw) != Wrap(end)) {
            if (ProbeAdjacentCell(x, y, Wrap(cw), isRoadAt)) {
                if (found) {
                    return RoadSweep.Fork;
                }
                target = Wrap(cw + offset);
                if (Wrap(cw) == Wrap(ccw)) {
                    return RoadSweep.Turn; // straight on: taken immediately
                }
                found = true;
            }

            if (ProbeAdjacentCell(x, y, Wrap(ccw), isRoadAt)) {
                if (found) {
                    return RoadSweep.Fork;
                }
                target = Wrap(ccw + offset);
                found = true;
            }

            cw += CompassStep;
            ccw -= CompassStep;
        }

        return found ? RoadSweep.Turn : RoadSweep.None;
    }

    /// <summary>
    /// Whether travel mode may be engaged at all: not already travelling, and standing on road
    /// or bridge. This is the gate on the travel button (spec §3.5).
    /// </summary>
    public static bool CanEngage(bool alreadyTravelling, int currentKind) =>
        !alreadyTravelling && IsRoadKind(currentKind);

    /// <summary>
    /// Engaging travel: snap to the centre of a nearby road cell.
    /// </summary>
    /// <remarks>
    /// The current cell's centre is always tried first. If that is not road, three neighbours are
    /// tried — and <b>which</b> three depends on where in the cell the party stands, so the snap
    /// only ever reaches toward the quadrant it is already in. A party in the south-west quarter
    /// will not be pulled onto a road to its north-east.
    /// <para>If nothing road-like is within reach the position is left untouched and engaging
    /// fails, which is how the original refuses to start travel off-road.</para>
    /// </remarks>
    /// <returns>True when a cell was found; <paramref name="snapX"/>/<paramref name="snapY"/>
    /// then hold its centre.</returns>
    public static bool TryEngage(int x, int y, Func<int, int, bool> isRoadAt, out int snapX, out int snapY) {
        if (isRoadAt == null) {
            throw new ArgumentNullException(nameof(isRoadAt));
        }

        snapX = x;
        snapY = y;

        int cx = CellCentre(x);
        int cy = CellCentre(y);

        foreach ((int ox, int oy) in EngageCandidates(Mod(x, CellSize), Mod(y, CellSize))) {
            int candidateX = cx + (ox * CellSize);
            int candidateY = cy + (oy * CellSize);
            if (isRoadAt(candidateX, candidateY)) {
                snapX = candidateX;
                snapY = candidateY;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Cell offsets the engage snap tries, in the original's order: the centre, then the three
    /// neighbours on the side of the cell the party is standing in.
    /// </summary>
    /// <remarks>
    /// The original builds a 4-bit quadrant mask (<c>0xf</c> narrowed by two tests) which always
    /// collapses to exactly one of 1, 2, 4 or 8, then guards nine candidate blocks with it. That
    /// is the same as picking one of four fixed triples, which is what this returns.
    /// </remarks>
    private static IEnumerable<(int X, int Y)> EngageCandidates(int xInCell, int yInCell) {
        yield return (0, 0); // always the current cell first

        bool east = xInCell >= HalfCell;
        bool north = yInCell >= HalfCell;

        if (east && north) {           // mask 1
            yield return (0, 1);
            yield return (1, 1);
            yield return (1, 0);
        } else if (!east && north) {   // mask 2
            yield return (-1, 1);
            yield return (0, 1);
            yield return (-1, 0);
        } else if (!east) {            // mask 4 — south-west
            yield return (-1, 0);
            yield return (-1, -1);
            yield return (0, -1);
        } else {                       // mask 8 — south-east
            yield return (1, 0);
            yield return (0, -1);
            yield return (1, -1);
        }
    }

    /// <summary>Centre of the cell a coordinate falls in.</summary>
    public static int CellCentre(int value) => value - Mod(value, CellSize) + HalfCell;

    /// <summary>Whether the party is standing exactly on a cell centre, where the road may bend.</summary>
    public static bool IsOnCellCentre(int x, int y) =>
        Mod(x, CellSize) == HalfCell && Mod(y, CellSize) == HalfCell;

    /// <summary>
    /// Ticks between travel steps: <c>CellSize / stepSize</c>. Raising the step-size preference
    /// mid-travel resets the counter, so the caller must re-snap when it changes.
    /// </summary>
    public static int TicksPerStep(int stepSize) => stepSize <= 0 ? 0 : CellSize / stepSize;

    private static (int X, int Y) DiagonalSample(int index) {
        switch (index) {
            case 7: return (0x32a, 0x316);    // 315
            case 1: return (-0x32a, 0x316);   // 45
            case 5: return (0x32a, -0x316);   // 225
            default: return (-0x32a, -0x316); // 135
        }
    }

    private static (int X, int Y) DiagonalNudge(int index) {
        switch (index) {
            case 7: return (-0x14, 0x14);
            case 1: return (0x14, 0x14);
            case 5: return (-0x14, -0x14);
            default: return (0x14, -0x14);
        }
    }

    private static ushort Wrap(int angle) => unchecked((ushort)angle);

    // World coordinates go negative, and C#'s % follows the sign of the dividend; the lattice
    // tests need a non-negative residue or every test west/south of the origin would fail.
    private static int Mod(int value, int m) {
        int r = value % m;
        return r < 0 ? r + m : r;
    }
}
