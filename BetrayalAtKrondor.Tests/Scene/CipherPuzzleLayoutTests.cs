namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Scene;
using Xunit;

/// <summary>The cipher screen's column geometry — <c>sub_ovr191_6EA</c> @0x7934a.</summary>
public class CipherPuzzleLayoutTests {
    // REQ_PUZL's own first column: canonical (150, 192), 75 x 90.
    private const int ColumnX = 150;
    private const int ColumnY = 192;
    private const int ColumnW = 75;
    private const int ColumnH = 90;

    [Fact]
    public void ColumnsAreConsecutiveActionIdsFrom128() {
        Assert.Equal(128, CipherPuzzleLayout.ActionIdFor(0));
        Assert.Equal(142, CipherPuzzleLayout.ActionIdFor(14));
        Assert.Equal(147, CipherPuzzleLayout.ActionIdFor(19));
    }

    [Fact]
    public void ThereAreTWENTYEntriesButOnlyFIFTEENPositions() {
        // 143..147 are parked on top of 142. A screen that sizes its enable/disable loop to the
        // fifteen visible ones leaves those five in the state the data shipped them in — and the
        // loader keeps faceless click areas navigable, so they linger in the keyboard ring.
        Assert.Equal(20, CipherPuzzleLayout.MaxColumns);
        Assert.Equal(15, CipherPuzzleLayout.DistinctColumns);
    }

    [Fact]
    public void EveryShippedTargetFitsWithoutStacking() {
        // The longest in the base game is "EYE TO EYE" at ten.
        Assert.True(CipherPuzzleLayout.FitsWithoutOverlap(10));
        Assert.True(CipherPuzzleLayout.FitsWithoutOverlap(15));
        Assert.False(CipherPuzzleLayout.FitsWithoutOverlap(16));
    }

    [Fact]
    public void AColumnPastWhatTheScreenShipsHasNoActionId() {
        Assert.Equal(-1, CipherPuzzleLayout.ActionIdFor(CipherPuzzleLayout.MaxColumns));
        Assert.Equal(-1, CipherPuzzleLayout.ActionIdFor(-1));
    }

    [Fact]
    public void TheGlyphIsCentredInTheColumnAndLeansONEPixelRight() {
        // x is centred then nudged (inc dx, 0x79412); y is centred with no counterpart. Applying
        // the nudge to both axes, or to neither, is the easy way to get this subtly wrong.
        Assert.Equal(ColumnX + 37 - 10 + 1, CipherPuzzleLayout.GlyphX(ColumnX, ColumnW, 20));
        Assert.Equal(ColumnY + 45 - 15, CipherPuzzleLayout.GlyphY(ColumnY, ColumnH, 30));
    }

    [Fact]
    public void AGlyphWiderThanItsColumnStillStartsInsideIt() {
        // Degenerate but reachable with an override font: the centring must not run off the left.
        int x = CipherPuzzleLayout.GlyphX(ColumnX, ColumnW, ColumnW);

        Assert.Equal(ColumnX + 1, x);
    }

    [Fact]
    public void TheBevelIsINSETAndDeliberatelyAsymmetric() {
        // One pixel in at the top-left, two at the bottom-right — what makes the box read pressed.
        (int x, int y, int right, int bottom) =
            CipherPuzzleLayout.BevelRect(ColumnX, ColumnY, ColumnW, ColumnH);

        Assert.Equal(ColumnX + 1, x);
        Assert.Equal(ColumnY + 1, y);
        Assert.Equal(ColumnX + ColumnW - 2, right);
        Assert.Equal(ColumnY + ColumnH - 2, bottom);
        Assert.True(right - x < ColumnW - 2, "the inset must be narrower than the box");
    }

    [Fact]
    public void AClickROTATESTheColumnAndWrapsAtTheEnd() {
        // The whole interaction: a wheel of dial rows, never a typed letter.
        Assert.Equal(1, CipherPuzzleLayout.NextRow(0, 4));
        Assert.Equal(3, CipherPuzzleLayout.NextRow(2, 4));
        Assert.Equal(0, CipherPuzzleLayout.NextRow(3, 4));
    }

    [Fact]
    public void RotatingAColumnWithNoRowsIsNotADivideByZero() =>
        Assert.Equal(0, CipherPuzzleLayout.NextRow(0, 0));

    [Fact]
    public void EveryRowIsReachableByClickingRepeatedly() {
        var seen = new System.Collections.Generic.HashSet<int>();
        var row = 0;
        for (var click = 0; click < 5; click++) {
            seen.Add(row);
            row = CipherPuzzleLayout.NextRow(row, 5);
        }

        Assert.Equal(5, seen.Count);
        Assert.Equal(0, row);   // and it is back where it started
    }

    [Fact]
    public void TheRiddleIsDrawnTHREETIMESForTheEmboss() {
        // A highlight above, a shadow below, the body last on top. Drawing it once loses the
        // emboss that makes it readable against the chest lid.
        (int YOffset, int Pen)[] passes = CipherPuzzleLayout.TextPasses();

        Assert.Equal(3, passes.Length);
        Assert.Equal((-1, 65), passes[0]);
        Assert.Equal((1, 149), passes[1]);
        Assert.Equal((0, CipherPuzzleLayout.TextBodyPen), passes[2]);
    }

    [Fact]
    public void TheBodyIsDrawnLASTSoItCoversTheOtherTwo() {
        (int YOffset, int Pen)[] passes = CipherPuzzleLayout.TextPasses();

        Assert.Equal(0, passes[^1].YOffset);
        Assert.All(passes[..^1], p => Assert.NotEqual(0, p.YOffset));
    }

    // ---- the wheel's roll ---------------------------------------------------------------------

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(16)]
    public void TheIncomingLetterLANDSExactlyWhereTheOldOneWas(int fontHeight) {
        // The whole point of the two figures differing: the loop overshoots by one frame and steps
        // back, so travel is frames - 1, and the incoming letter starts exactly that far above.
        int start = CipherPuzzleLayout.IncomingLetterOffset(fontHeight);
        int travel = CipherPuzzleLayout.RollTravel(fontHeight);

        Assert.Equal(0, start + travel);
        Assert.Equal(CipherPuzzleLayout.RollFrames(fontHeight) - 1, travel);
    }

    [Fact]
    public void TheRollIsALWAYSSomeFramesLong() {
        // Even a degenerate font must not produce a zero-frame roll, which would read as a swap.
        Assert.True(CipherPuzzleLayout.RollFrames(0) > 0);
        Assert.True(CipherPuzzleLayout.RollFrames(1) > 0);
    }

    [Fact]
    public void TheIncomingLetterStartsABOVETheOutgoingOne() =>
        // Negative, because it falls in from the top of the clipped box.
        Assert.True(CipherPuzzleLayout.IncomingLetterOffset(10) < 0);
}
