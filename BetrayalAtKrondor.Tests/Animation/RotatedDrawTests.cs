namespace BetrayalAtKrondor.Tests.Animation;

using GameData.Resources.Animation;
using Xunit;

/// <summary>Where a rotated cutscene image lands (TASK-159 slice).</summary>
public class RotatedDrawTests {
    [Fact]
    public void NOROTATIONLeavesTheBoxAlone() =>
        Assert.Equal((100, 40), RotatedDraw.Bounds(100, 40, 0));

    [Theory]
    [InlineData(90)]
    [InlineData(270)]
    [InlineData(-90)]
    public void AQUARTERTurnSwapsTheDimensions(double angle) =>
        Assert.Equal((40, 100), RotatedDraw.Bounds(100, 40, angle));

    [Theory]
    [InlineData(180)]
    [InlineData(360)]
    [InlineData(-180)]
    public void AHALFTurnLeavesTheBoxAlone(double angle) =>
        // The box is |w cos| + |h sin| — even in the angle, so a half turn cannot change it.
        Assert.Equal((100, 40), RotatedDraw.Bounds(100, 40, angle));

    [Fact]
    public void ASQUAREGrowsByRootTwoOnTheDiagonal() {
        (int width, int height) = RotatedDraw.Bounds(100, 100, 45);

        // ceil(100 * cos45 + 100 * sin45) = ceil(141.42) = 142
        Assert.Equal(142, width);
        Assert.Equal(142, height);
    }

    [Fact]
    public void THEBOXISRoundedUpNeverDown() {
        // *** Rounding down would clip the corners the box exists to contain. *** 30 degrees on a
        // 10x10 gives 13.66; the box must be 14.
        (int width, int _) = RotatedDraw.Bounds(10, 10, 30);
        Assert.Equal(14, width);
    }

    [Theory]
    [InlineData(67.32422)]
    [InlineData(60.38086)]
    [InlineData(329.0625)]
    [InlineData(10.283203)]
    public void EVERYSHIPPEDAngleGrowsTheBoxWithoutLosingArea(double angle) {
        // The real angles from generated/TTM. A rotated rectangle's bounding box can be NARROWER
        // than the original in one axis (a wide thin bitmap turned upright), so the invariant is
        // area, not per-axis size — asserting width >= width would be wrong and would pass here by
        // luck.
        (int width, int height) = RotatedDraw.Bounds(120, 40, angle);

        Assert.True(width * height >= 120 * 40,
            $"the axis-aligned box must contain the rotated bitmap; got {width}x{height}");
    }

    [Fact]
    public void THEPIVOTStaysPutHoweverFarTheBoxGrows() {
        // *** THE POINT OF CENTRING. *** The box grows with the angle; if the top-left were the
        // anchor the image would walk across the screen as it turned. Same centre in, same centre
        // out, at any angle.
        const int centreX = 800;
        const int centreY = 600;

        foreach (double angle in new[] { 0.0, 30.0, 45.0, 67.32422, 90.0, 180.0, 329.0625 }) {
            (int w, int h) = RotatedDraw.Bounds(120, 40, angle);
            (int x, int y) = RotatedDraw.TopLeftFor(centreX, centreY, w, h, 1.0, 1.0);

            Assert.Equal(centreX, x + (int)System.Math.Round(w / 2.0, System.MidpointRounding.AwayFromZero));
            Assert.Equal(centreY, y + (int)System.Math.Round(h / 2.0, System.MidpointRounding.AwayFromZero));
        }
    }

    [Fact]
    public void THESCALEMovesTheOffsetNotTheAnchor() {
        // Doubling the scale doubles the half-box subtracted, and leaves the named point alone. If
        // the anchor were scaled too, a scaled rotation would orbit the screen origin.
        (int unscaledX, int unscaledY) = RotatedDraw.TopLeftFor(800, 600, 100, 40, 1.0, 1.0);
        (int scaledX, int scaledY) = RotatedDraw.TopLeftFor(800, 600, 100, 40, 2.0, 2.0);

        Assert.Equal(800 - 50, unscaledX);
        Assert.Equal(800 - 100, scaledX);
        Assert.Equal(600 - 20, unscaledY);
        Assert.Equal(600 - 40, scaledY);
    }
}
