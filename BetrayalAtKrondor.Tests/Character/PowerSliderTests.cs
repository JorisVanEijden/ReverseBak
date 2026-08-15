namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The power slider's interaction rules — UI_SelectSpellCost. The geometry is CastRingLayout's
/// existing hit test; these are the behaviours layered on it.
/// </summary>
public class PowerSliderTests {
    [Fact]
    public void PositionsAreZeroBasedAndPowersAreOneBased() {
        // The routine converts at every use; carrying the position through makes every cast a point
        // weak.
        Assert.Equal(1, CastRingLayout.PowerAtPosition(0));
        Assert.Equal(30, CastRingLayout.PowerAtPosition(29));
        Assert.Equal(0, CastRingLayout.PositionForPower(1));
    }

    [Fact]
    public void TheConversionsAreInverses() {
        for (var position = 0; position < CastRingLayout.PositionCount; position++) {
            Assert.Equal(position,
                CastRingLayout.PositionForPower(CastRingLayout.PowerAtPosition(position)));
        }
    }

    [Fact]
    public void ThePreviewFollowsTheCursor() {
        Assert.Equal(6, CastRingLayout.PreviewPower(hoveredPosition: 5));
    }

    [Fact]
    public void AndResetsToZeroWhenTheCursorLeavesTheBand() {
        // Redrawn with a cost of zero rather than left showing the last value.
        Assert.Equal(0, CastRingLayout.PreviewPower(hoveredPosition: -1));
    }

    [Fact]
    public void AClickOffTheBandDoesNothingAtAll() {
        // Not a cancel and not a clamp — the click is tested inside the hovered branch.
        Assert.True(CastRingLayout.ClickCommitsImmediately);
        Assert.Equal(0, CastRingLayout.PreviewPower(-1));
    }

    [Fact]
    public void CancellingIsDistinctFromSelectingTheLowestPower() {
        Assert.Equal(-1, CastRingLayout.Cancelled);
        Assert.NotEqual(CastRingLayout.Cancelled, CastRingLayout.PowerAtPosition(0));
    }

    [Fact]
    public void AndTheCancelKeyIsHeldUntilReleased() {
        // Otherwise the same press cancels the screen underneath as well.
        Assert.True(CastRingLayout.CancelWaitsForKeyRelease);
    }
}
