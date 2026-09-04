namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// Turning the party to face an encounter before an underground fight.
/// </summary>
public class ArenaFacingTests {
    [Fact]
    public void HeadingZeroIsPlusY_AndPlusXIsThreeQuarters() {
        // The convention the party's own step uses, derived from the original's fixed-point atan2
        // at its exact cases. Getting this backwards turns the party away from the encounter.
        Assert.Equal(0, ArenaFacing.HeadingTo(0, 1000));
        Assert.Equal(0xC000, ArenaFacing.HeadingTo(1000, 0));
        Assert.Equal(0x8000, ArenaFacing.HeadingTo(0, -1000));
        Assert.Equal(0x4000, ArenaFacing.HeadingTo(-1000, 0));
    }

    [Fact]
    public void ItAgreesWithTheStepTheHeadingWouldProduce() {
        // The round trip: a heading that steps toward (dx, dy) must be the heading this answers for
        // (dx, dy). Asserted at the four quarter turns, which are the only values that survive the
        // snap anyway.
        foreach (ushort heading in new ushort[] { 0, 0x4000, 0x8000, 0xC000 }) {
            double theta = heading / 65536.0 * 2.0 * System.Math.PI;
            var dx = (long)System.Math.Round(-System.Math.Sin(theta) * 10000);
            var dy = (long)System.Math.Round(System.Math.Cos(theta) * 10000);

            Assert.Equal(heading, ArenaFacing.HeadingTo(dx, dy));
        }
    }

    [Fact]
    public void TheSnapROUNDSRatherThanTruncates() {
        // *** THE BIAS IS THE WHOLE POINT. *** Masking alone truncates, and a party 44 degrees past
        // a quarter turn would be turned a full quadrant away from what it is looking at.
        Assert.Equal(0x4000, ArenaFacing.SnapToQuadrant(0x3FFF));   // just short — rounds UP
        Assert.Equal(0x4000, ArenaFacing.SnapToQuadrant(0x4001));   // just past — stays
        Assert.Equal(0, ArenaFacing.SnapToQuadrant(0x1FFF));
        Assert.Equal(0x4000, ArenaFacing.SnapToQuadrant(0x2000));   // exactly half way rounds up
    }

    [Fact]
    public void EveryAnswerIsAQuarterTurn() {
        for (var heading = 0; heading < 65536; heading += 137) {
            ushort snapped = ArenaFacing.SnapToQuadrant((ushort)heading);
            Assert.Equal(0, snapped % ArenaFacing.Quadrant);
        }
    }

    [Fact]
    public void TheSnapMakesTheAtan2sPrecisionUnobservable() {
        // Why a double is safe here. Anything within 45 degrees of a quarter turn snaps to it, so a
        // table-based atan2 and this one cannot disagree on the result.
        foreach (ushort exact in new ushort[] { 0, 0x4000, 0x8000, 0xC000 }) {
            for (var wobble = -0x1FFF; wobble <= 0x1FFF; wobble += 0x400) {
                var off = (ushort)((exact + wobble + 65536) % 65536);
                Assert.Equal(exact, ArenaFacing.SnapToQuadrant(off));
            }
        }
    }

    [Fact]
    public void ASingleCellBoxCentresOnThatCellRatherThanOnItsCorner() {
        // The far edge is the far side of the max cell. Averaging min and max instead would put the
        // centre on the cell's near corner, and a one-cell hotspot would point at its own edge.
        (long x, long y) = ArenaFacing.BoxCentre(0, 0, boxStartX: 4, boxEndY: 4, boxEndX: 4, boxStartY: 4);

        Assert.Equal((4 * WorldPlacement.SubCellSize) + (WorldPlacement.SubCellSize / 2), x);
        Assert.Equal(x, y);
    }

    [Fact]
    public void TheAXISPairingIsWhatTheOnDiskOrderDecidesHere_NotMinVersusMax() {
        // The box is stored minX, maxY, maxX, minY, and X is built from bytes 0 and 2 while Y is
        // built from 3 and 1. Getting THAT wrong — pairing 0 with 1 and 2 with 3 — moves the centre
        // for any box that is not square.
        (long x, long y) = ArenaFacing.BoxCentre(0, 0, boxStartX: 2, boxEndY: 20, boxEndX: 6,
            boxStartY: 4);

        Assert.Equal((2 + 6 + 1) * WorldPlacement.SubCellSize / 2, x);
        Assert.Equal((4 + 20 + 1) * WorldPlacement.SubCellSize / 2, y);
        Assert.NotEqual(x, y);
    }

    [Fact]
    public void SwappingMinAndMaxWithinAnAxisIsInvisibleHere_UnlikeInTheApproachOutcode() {
        // Worth pinning because the two consumers of this box differ. The centre is the MIDPOINT of
        // the pair, and a midpoint is commutative — so ArenaFacing cannot detect the bound swap
        // that EncounterAftermath.ApproachDirection inverts on. Someone comparing the two would
        // otherwise conclude one of them reads the box wrongly.
        (long _, long asStored) = ArenaFacing.BoxCentre(0, 0, 2, 20, 6, 4);
        (long _, long swapped) = ArenaFacing.BoxCentre(0, 0, 2, 4, 6, 20);

        Assert.Equal(asStored, swapped);
        Assert.NotEqual(EncounterAftermath.ApproachDirection(0, 0,
                WorldPlacement.CornerOf(0, 4), WorldPlacement.CornerOf(0, 12), 2, 20, 6, 4),
            EncounterAftermath.ApproachDirection(0, 0,
                WorldPlacement.CornerOf(0, 4), WorldPlacement.CornerOf(0, 12), 2, 4, 6, 20));
    }

    [Fact]
    public void TheCentreIsRelativeToThePartysOWNTile() {
        (long farX, long farY) = ArenaFacing.BoxCentre(3, 5, 2, 6, 4, 2);
        (long nearX, long nearY) = ArenaFacing.BoxCentre(0, 0, 2, 6, 4, 2);

        Assert.Equal(nearX + (3L * WorldPlacement.TileSize), farX);
        Assert.Equal(nearY + (5L * WorldPlacement.TileSize), farY);
    }

    [Fact]
    public void ThePartyIsTurnedTowardTheBoxAndOnlyToAQuarterTurn() {
        var trigger = new TileEventTrigger { StartX = 30, EndY = 34, EndX = 34, StartY = 30 };

        // Standing at the tile origin, the box is up and to the right; +Y dominates, so the snap
        // lands on 0 rather than on the diagonal it actually bears.
        ushort facing = ArenaFacing.FacingFor(trigger, 0, 0,
            WorldPlacement.CornerOf(0, 32), WorldPlacement.CornerOf(0, 0));
        Assert.Equal(0, facing);

        // Approaching from beyond it in Y turns the party back around.
        ushort fromBeyond = ArenaFacing.FacingFor(trigger, 0, 0,
            WorldPlacement.CornerOf(0, 32), WorldPlacement.CornerOf(0, 39));
        Assert.Equal(0x8000, fromBeyond);
    }

    [Fact]
    public void ANullTriggerAnswersZeroRatherThanThrowing() {
        Assert.Equal(0, ArenaFacing.FacingFor(null, 0, 0, 0, 0));
    }

    [Fact]
    public void OctantTowardIsZeroDeeperIntoTheArena() {
        // Octant 0 is AWAY from the camera, matching Combatant.FacingOctant, so a target one row
        // further in and no columns across is 0 — the direction a freshly deployed party already
        // faces.
        Assert.Equal(0, ArenaFacing.OctantToward(0, 1));
        Assert.Equal(0, ArenaFacing.OctantToward(0, 5));   // distance does not change the direction
    }

    [Fact]
    public void OctantTowardWalksTheEighthsInOrder() {
        // Each further octant is an eighth of a turn toward increasing columns. Pinned as a ring so
        // a sign flip anywhere shows up as a specific wrong facing rather than a vague one.
        Assert.Equal(1, ArenaFacing.OctantToward(1, 1));
        Assert.Equal(2, ArenaFacing.OctantToward(1, 0));
        Assert.Equal(3, ArenaFacing.OctantToward(1, -1));
        Assert.Equal(4, ArenaFacing.OctantToward(0, -1));
        Assert.Equal(5, ArenaFacing.OctantToward(-1, -1));
        Assert.Equal(6, ArenaFacing.OctantToward(-1, 0));
        Assert.Equal(7, ArenaFacing.OctantToward(-1, 1));
    }

    [Fact]
    public void OctantTowardRefusesTheActorsOwnCell() {
        // A cursor on the actor's own cell names no direction. Rounding atan2(0,0) would answer 0
        // — "straight ahead" — and turn the actor on a move that carried no information.
        Assert.Equal(-1, ArenaFacing.OctantToward(0, 0));
    }
}

