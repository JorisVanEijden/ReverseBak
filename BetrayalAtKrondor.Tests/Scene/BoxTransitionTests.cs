namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Animation;
using Xunit;

/// <summary>The rectangle both box transitions walk.</summary>
public class BoxTransitionTests {
    [Fact]
    public void TheWalkTakesAsManyPassesAsTheLONGERHalfAxis() {
        Assert.Equal(160, BoxTransition.StepCount(320, 200));   // half-width wins
        Assert.Equal(100, BoxTransition.StepCount(100, 200));   // half-height wins
    }

    /// <summary>A zero-sized area still takes one pass rather than dividing by zero.</summary>
    [Fact]
    public void AnEmptyAreaStillHasAStep() => Assert.Equal(1, BoxTransition.StepCount(0, 0));

    [Fact]
    public void TheLastPassCoversTheWholeArea() {
        int steps = BoxTransition.StepCount(320, 200);
        BoxTransition.Step last = BoxTransition.RectAt(0, 0, 320, 200, steps, steps);

        Assert.Equal(0, last.X);
        Assert.Equal(0, last.Y);
        Assert.Equal(320, last.Width);
        Assert.Equal(200, last.Height);
    }

    [Fact]
    public void TheFirstPassIsANearPointAtTheCentre() {
        int steps = BoxTransition.StepCount(320, 200);
        BoxTransition.Step first = BoxTransition.RectAt(0, 0, 320, 200, 1, steps);

        Assert.True(first.Width <= 4 && first.Height <= 4);
        Assert.Equal(160, first.X + first.Width / 2);
        Assert.Equal(100, first.Y + first.Height / 2);
    }

    /// <summary>
    /// The point of the thousandths: an oblong area's short axis must not finish early and sit
    /// still while the long one catches up.
    /// </summary>
    [Fact]
    public void BothAxesReachTheirExtremeOnTheSamePass() {
        int steps = BoxTransition.StepCount(320, 40);
        BoxTransition.Step last = BoxTransition.RectAt(0, 0, 320, 40, steps, steps);
        BoxTransition.Step nearly = BoxTransition.RectAt(0, 0, 320, 40, steps - 1, steps);

        Assert.Equal(320, last.Width);
        Assert.Equal(40, last.Height);
        Assert.True(nearly.Width < last.Width);
        Assert.True(nearly.Height <= last.Height);
    }

    [Fact]
    public void EveryStepStaysCentredOnTheArea() {
        int steps = BoxTransition.StepCount(200, 100);
        for (var i = 0; i <= steps; i++) {
            BoxTransition.Step r = BoxTransition.RectAt(30, 40, 200, 100, i, steps);
            Assert.Equal(130, r.X + r.Width / 2);
            Assert.Equal(90, r.Y + r.Height / 2);
        }
    }
}
