namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Animation;
using Xunit;

/// <summary>
/// The two box screen transitions — <c>anim_screenTransitionEffect</c> @0x53ab5, TTM opcodes
/// 0xA034 (box in) and 0xA094 (box out).
/// </summary>
/// <remarks>
/// <b>These were written against a second model of the same effect.</b> BoxTransition and
/// ScreenTransitionBox both described that function, the same two opcodes and the same rectangle
/// walk — and only ScreenTransitionBox had a caller (ScreenTransitionBoxExtensions). The duplicate
/// is gone; these moved onto the survivor.
///
/// <para><b>The two disagreed, which is the argument against keeping both.</b> The unconsumed one
/// clamped its step count to a minimum of 1, so a zero-size area got a step where the original —
/// and the survivor — take <c>max(halfWidth, halfHeight)</c> passes and simply run none. The
/// clamp was an invention, and it lived in the copy nothing exercised.</para>
/// </remarks>
public class BoxTransitionTests {
    [Fact]
    public void TheLARGERHalfExtentDecidesTheStepCount() {
        // So the box always reaches the far edge however oblong the area is.
        Assert.Equal(160, ScreenTransitionBox.StepCount(320, 200));   // half-width wins
        Assert.Equal(100, ScreenTransitionBox.StepCount(100, 200));   // half-height wins
    }

    [Fact]
    public void AZeroSizeAreaTakesNOSteps_NotOne() {
        // The faithful answer: max(0, 0) is 0 passes, and the caller's loop simply does not run.
        // The deleted duplicate clamped this to 1, which would blit one degenerate rectangle.
        Assert.Equal(0, ScreenTransitionBox.StepCount(0, 0));
    }

    [Fact]
    public void TheLastStepIsTheWholeArea() {
        int steps = ScreenTransitionBox.StepCount(320, 200);
        (int x, int y, int w, int h) = ScreenTransitionBox.BoxAt(0, 0, 320, 200, steps);

        Assert.Equal(0, x);
        Assert.Equal(0, y);
        Assert.Equal(320, w);
        Assert.Equal(200, h);
    }

    [Fact]
    public void TheBoxStaysCentredThroughout() {
        // Both axes reach their extreme on the same pass, which is what the thousandths scaling is
        // for — an oblong area must not finish one axis early and sit lopsided.
        int steps = ScreenTransitionBox.StepCount(320, 200);
        (int x, int y, int w, int h) = ScreenTransitionBox.BoxAt(0, 0, 320, 200, steps / 2);

        Assert.Equal(320 - (x + w), x);
        Assert.Equal(200 - (y + h), y);
    }

    [Fact]
    public void TheTWODIRECTIONSAreOneAlgorithmWalkedEitherWay() {
        // Box-out counts up from 1, box-in counts down from the step count. Implementing them
        // separately duplicates the arithmetic and lets the two drift — which is exactly what
        // having two models of this did.
        Assert.Equal(1, ScreenTransitionBox.BoxOutFirstStep);
        Assert.Equal(ScreenTransitionBox.StepCount(320, 200),
            ScreenTransitionBox.BoxInFirstStep(320, 200));
    }
}
