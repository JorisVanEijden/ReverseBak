namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Scene;
using Xunit;

/// <summary>
/// Where the letter wheels sit — <c>cipher_puzzle_layout_letters</c>.
/// </summary>
public class CipherPuzzleRowLayoutTests {
    [Fact]
    public void TheAuthoredRectsAreThrownAway() {
        // REQ_PUZL ships fifteen columns marching from the left edge; the screen overwrites every
        // one before drawing. Using the file's positions puts a short word in the top-left corner.
        Assert.True(CipherPuzzleLayout.AuthoredColumnRectsAreOverwritten);
    }

    [Fact]
    public void TheRowIsCentredSoItGrowsOutwardsFromTheMiddle() {
        // A three-letter word and a seven-letter word share a centre, which is the visual point of
        // the screen — and is exactly what the authored left-aligned rects do not do.
        const int screen = 320, cell = 14, gap = CipherPuzzleLayout.ColumnGapVga;
        int three = CipherPuzzleLayout.RowSpan(3, cell, gap);
        int seven = CipherPuzzleLayout.RowSpan(7, cell, gap);
        int threeCentre = CipherPuzzleLayout.RowStartX(screen, three) + (three / 2);
        int sevenCentre = CipherPuzzleLayout.RowStartX(screen, seven) + (seven / 2);
        Assert.InRange(System.Math.Abs(threeCentre - sevenCentre), 0, 1);
        Assert.InRange(threeCentre, (screen / 2) - 1, (screen / 2) + 1);
    }

    [Fact]
    public void TheTrailingGapIsTakenBackOff() {
        // One letter spans exactly one cell, not a cell plus a dangling gap.
        Assert.Equal(14, CipherPuzzleLayout.RowSpan(1, 14, 2));
        Assert.Equal(30, CipherPuzzleLayout.RowSpan(2, 14, 2));
    }

    [Fact]
    public void TheTwoHalvesAreTakenSEPARATELY_NotAsHalfTheDifference() {
        // The original does (w >> 1) - (span >> 1). That differs from halving the difference
        // exactly when the container is EVEN and the span ODD — which is the live case here, since
        // the screen is 320 (or canonical 1600) and an odd cell width makes the span odd.
        const int screen = 320, span = 15;
        Assert.Equal((screen / 2) - (span / 2), CipherPuzzleLayout.RowStartX(screen, span));
        Assert.Equal(153, CipherPuzzleLayout.RowStartX(screen, span));
        Assert.NotEqual((screen - span) / 2, CipherPuzzleLayout.RowStartX(screen, span)); // 152
        // Both odd, and the two agree again — so the divergence is not universal.
        Assert.Equal((321 - 15) / 2, CipherPuzzleLayout.RowStartX(321, 15));
    }

    [Fact]
    public void ColumnsStepByCellPlusGap() {
        int start = CipherPuzzleLayout.RowStartX(320, CipherPuzzleLayout.RowSpan(3, 14, 2));
        Assert.Equal(start, CipherPuzzleLayout.ColumnX(0, start, 14, 2));
        Assert.Equal(start + 16, CipherPuzzleLayout.ColumnX(1, start, 14, 2));
        Assert.Equal(start + 32, CipherPuzzleLayout.ColumnX(2, start, 14, 2));
    }

    [Fact]
    public void TheRowSitsAtAFixedHeightOfItsOwn() {
        // 0x57, unrelated to whatever the REQ entries claim for y.
        Assert.Equal(87, CipherPuzzleLayout.RowTopVga);
        Assert.Equal(6, CipherPuzzleLayout.CellPaddingVga);
        Assert.Equal(2, CipherPuzzleLayout.ColumnGapVga);
    }
}
