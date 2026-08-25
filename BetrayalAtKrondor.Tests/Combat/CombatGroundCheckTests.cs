namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using GameData.Resources.World;
using Xunit;

/// <summary>
/// Whether there is enough open ground to hold a fight — <c>combatgrid_tiles_over_thresh</c>.
/// </summary>
public class CombatGroundCheckTests {
    private const int CellSize = 300;   // StartData.CombatGridCellSize as shipped

    [Fact]
    public void TheBarIsTwentyFourOfTheHundredAndFourSampledCells() {
        Assert.Equal(104, CombatGroundCheck.SampledCells);
        Assert.False(CombatGroundCheck.Passes(23));
        Assert.True(CombatGroundCheck.Passes(24));
        Assert.True(CombatGroundCheck.Passes(104));
    }

    [Fact]
    public void TheFullBufferIsSwept_NotJustTheUndergroundPlayablePart() {
        // Underground only 7 of the 13 rows are in play, but the check sweeps all thirteen — it
        // asks whether the footprint fits, not whether its playable part does. Sampling only the
        // playable rows would leave 56 cells and make the bar nearly half instead of a quarter.
        Assert.Equal(CombatGrid.Width * CombatGrid.Height, CombatGroundCheck.SampledCells);
        Assert.NotEqual(CombatGrid.Width * CombatGrid.UndergroundPlayableRows,
            CombatGroundCheck.SampledCells);
    }

    [Fact]
    public void WaterIsNotFightableGroundButTheBridgeOverItIs() {
        // The distinction the set exists to draw.
        Assert.False(CombatGroundCheck.IsOpenGround((int)WorldEntityType.Water));
        Assert.True(CombatGroundCheck.IsOpenGround(2));
        Assert.True(CombatGroundCheck.IsOpenGround((int)WorldEntityType.Ground));
        Assert.True(CombatGroundCheck.IsOpenGround((int)WorldEntityType.Road));
        Assert.True(CombatGroundCheck.IsOpenGround((int)WorldEntityType.Door));
    }

    [Fact]
    public void ItIsTheWALKABLESetMinusThePit() {
        // *** THE WHOLE DIFFERENCE IS ONE KIND. *** CheckMoveDestination accepts 0, 1, 2, 14, 15
        // and 23; this accepts all of them but 15. The party walks onto a pit — that is how they
        // fall in — and cannot stand on one to fight. Reusing the movement check here would lay an
        // arena out across open pits.
        int[] walkable = { 0, 1, 2, 14, 15, 23 };

        foreach (int kind in walkable) {
            Assert.Equal(kind != CombatGroundCheck.WalkableOnlyKind,
                CombatGroundCheck.IsOpenGround(kind));
        }

        Assert.Equal((int)WorldEntityType.Pit, CombatGroundCheck.WalkableOnlyKind);
        Assert.Equal(walkable.Length - 1, CombatGroundCheck.OpenGroundKinds.Count);
    }

    [Fact]
    public void EverythingElseIsClosedGround() {
        foreach (int kind in new[] { 3, 4, 5, 6, 10, 15, 26, 37, 42 }) {
            Assert.False(CombatGroundCheck.IsOpenGround(kind), $"kind {kind}");
        }
    }

    [Fact]
    public void TheFootprintIsCentredOnThePartysLineOfSight() {
        (int leftAcross, _) = CombatGroundCheck.SampleOffset(0, 0, CellSize);
        (int rightAcross, _) = CombatGroundCheck.SampleOffset(CombatGrid.Width - 1, 0, CellSize);

        Assert.Equal(-1050, leftAcross);
        Assert.Equal(1050, rightAcross);
        Assert.Equal(0, leftAcross + rightAcross);
    }

    [Fact]
    public void HalfTheWidthIsDERIVED_SoADifferentCellSizeStaysCentred() {
        // The original writes 1200, which is half of eight 300-unit cells. Restating the 1200 would
        // put the footprint off-centre for any other cell size.
        Assert.Equal(1200, CombatGroundCheck.HalfWidthOf(CellSize));

        (int left, _) = CombatGroundCheck.SampleOffset(0, 0, 400);
        (int right, _) = CombatGroundCheck.SampleOffset(CombatGrid.Width - 1, 0, 400);
        Assert.Equal(0, left + right);
    }

    [Fact]
    public void SamplesSitHalfACellNEARERThanTheCombatantThatWillStandThere() {
        // *** THE ORIGINAL'S OWN INCONSISTENCY, NOT A ROUNDING CHOICE. *** The sweep folds the
        // half-cell into its sideways step and not into its forward one, so X is centred and Y is
        // the near edge. A port that centred both would sample a different row of ground on the far
        // side of the arena.
        for (var row = 0; row < CombatGrid.Height; row++) {
            (int across, int away) = CombatGroundCheck.SampleOffset(3, row, CellSize);

            int placementAcross = (3 * CellSize) + (CellSize / 2)
                - CombatGroundCheck.HalfWidthOf(CellSize);
            int placementAway = (row * CellSize) + (CellSize / 2) + CombatGroundCheck.ForwardOffset;

            Assert.Equal(placementAcross, across);
            Assert.Equal(placementAway - (CellSize / 2), away);
        }
    }

    [Fact]
    public void TheNearestRowStartsInFrontOfTheParty_NotUnderThem() {
        // 3200 units clear of the party, so the arena is laid out ahead rather than around them.
        (_, int nearest) = CombatGroundCheck.SampleOffset(0, 0, CellSize);
        Assert.Equal(CombatGroundCheck.ForwardOffset, nearest);
        Assert.True(nearest > 0);
    }
}
