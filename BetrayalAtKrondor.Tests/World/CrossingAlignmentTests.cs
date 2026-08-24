namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;

using Xunit;

/// <summary>
/// The alignment gate on <c>worldmove_crossing_check_8dir</c> — shared by the party, the map's
/// auto-travel and roaming encounter actors.
/// </summary>
public class CrossingAlignmentTests {
    private const int Centre = CrossingAlignment.SubCellCentre;   // 0x320
    private const int Cell = CrossingAlignment.SubCellSize;       // 0x640

    [Fact]
    public void OnlyTheEightFortyFiveDegreeHeadingsAreProbeable() {
        foreach (int h in new[] {
            CrossingAlignment.East, CrossingAlignment.NorthEast, CrossingAlignment.North,
            CrossingAlignment.NorthWest, CrossingAlignment.West, CrossingAlignment.SouthWest,
            CrossingAlignment.South, CrossingAlignment.SouthEast }) {
            Assert.True(CrossingAlignment.IsProbeableHeading(h));
        }
        // One unit off is not a near miss — the original returns before looking at anything.
        Assert.False(CrossingAlignment.IsProbeableHeading(CrossingAlignment.East + 1));
        Assert.False(CrossingAlignment.IsProbeableHeading(0x3000));
    }

    [Fact]
    public void TheAxisHeadingsConstrainOneCoordinateOnly() {
        // East/West cares about x and ignores y entirely — and vice versa. Requiring both would
        // reject most legitimate crossings.
        Assert.True(CrossingAlignment.IsAligned(Centre, 7, CrossingAlignment.East));
        Assert.True(CrossingAlignment.IsAligned(Centre + Cell * 3, 999, CrossingAlignment.West));
        Assert.False(CrossingAlignment.IsAligned(Centre + 1, Centre, CrossingAlignment.East));

        Assert.True(CrossingAlignment.IsAligned(7, Centre, CrossingAlignment.North));
        Assert.False(CrossingAlignment.IsAligned(Centre, Centre + 1, CrossingAlignment.South));
    }

    [Fact]
    public void OneDiagonalWantsEqualOffsets() {
        Assert.True(CrossingAlignment.IsAligned(300, 300, CrossingAlignment.NorthWest));
        Assert.True(CrossingAlignment.IsAligned(300 + Cell, 300 + Cell * 2, CrossingAlignment.SouthEast));
        Assert.False(CrossingAlignment.IsAligned(300, 301, CrossingAlignment.NorthWest));
    }

    [Fact]
    public void TheOtherDiagonalWantsThemToSumToACell() {
        Assert.True(CrossingAlignment.IsAligned(400, Cell - 400, CrossingAlignment.NorthEast));
        Assert.True(CrossingAlignment.IsAligned(Centre, Centre, CrossingAlignment.SouthWest));
        Assert.False(CrossingAlignment.IsAligned(400, 401, CrossingAlignment.NorthEast));
    }

    [Fact]
    public void TheCellCornerIsAlignedOnTheSumDiagonal_DespiteSummingToZero() {
        // The clause that is NOT a tidy special case: at a corner both offsets are 0, so the sum is
        // 0 rather than a whole cell. Without the extra test a mover standing exactly on a corner
        // could never take a diagonal crossing.
        Assert.True(CrossingAlignment.IsAligned(0, 0, CrossingAlignment.NorthEast));
        Assert.True(CrossingAlignment.IsAligned(Cell * 5, Cell * 9, CrossingAlignment.SouthWest));
        // And only when BOTH are zero.
        Assert.False(CrossingAlignment.IsAligned(0, 1, CrossingAlignment.NorthEast));
        Assert.False(CrossingAlignment.IsAligned(1, 0, CrossingAlignment.NorthEast));
    }

    [Fact]
    public void ModeFourProbesBackwards() {
        Assert.Equal(CrossingAlignment.West,
            CrossingAlignment.ProbeHeading(CrossingAlignment.East, CrossingAlignment.ReversedProbeMode));
        Assert.Equal(CrossingAlignment.East,
            CrossingAlignment.ProbeHeading(CrossingAlignment.East, mode: 1));
    }

    [Fact]
    public void NegativeHeadingsNormaliseToTheSameEight() {
        // R3D_DEG yields a short, so 180 degrees and beyond arrive negative.
        Assert.Equal(CrossingAlignment.West, CrossingAlignment.Normalise(-0x8000));
        Assert.Equal(CrossingAlignment.South, CrossingAlignment.Normalise(-0x4000));
        Assert.True(CrossingAlignment.IsAligned(Centre, 0, -0x8000));
    }
}
