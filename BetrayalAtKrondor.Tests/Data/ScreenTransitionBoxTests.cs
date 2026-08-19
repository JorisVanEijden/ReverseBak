namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Animation;
using System.Linq;
using Xunit;

/// <summary>
/// The box transitions' step geometry (<c>anim_screenTransitionEffect</c> @0x53ab5).
/// </summary>
public class ScreenTransitionBoxTests {
    // The one shipped box-out, from C31 frame 17.
    private const int X = 70, Y = 66, W = 1450, H = 606;

    [Fact]
    public void TheStepCountIsTheLargerHalfExtent() =>
        // max(725, 303). Using the smaller would finish while the long axis was still short of the
        // edge, leaving a band of the old frame on screen.
        Assert.Equal(725, ScreenTransitionBox.StepCount(W, H));

    [Fact]
    public void HalvingDiscardsTheOddPixel() =>
        // The original uses sar, so an odd extent and its even predecessor agree.
        Assert.Equal(ScreenTransitionBox.StepCount(100, 40), ScreenTransitionBox.StepCount(101, 40));

    [Fact]
    public void TheFirstBoxOutStepIsAlmostNothing() {
        (int bx, int by, int bw, int bh) = ScreenTransitionBox.BoxAt(X, Y, W, H, 1);

        Assert.True(bw <= 2 && bh <= 2, "the reveal starts from the centre, not from a visible box");
        Assert.Equal(X + W / 2, bx + (bw / 2));
        Assert.Equal(Y + H / 2, by + (bh / 2));
    }

    [Fact]
    public void TheLastStepCoversTheLongAxisExactly() {
        int steps = ScreenTransitionBox.StepCount(W, H);
        (int bx, _, int bw, _) = ScreenTransitionBox.BoxAt(X, Y, W, H, steps);

        // The long axis drives the step count, so its per-step increment is exactly 1000/1000.
        Assert.Equal(X, bx);
        Assert.Equal(W / 2 * 2, bw);
    }

    [Fact]
    public void TheShortAxisFallsOnePixelShort_AndThatIsTheOriginalsArithmetic() {
        // NOT a rounding bug in the port. The original divides once up front
        // (halfHeight * 1000 / steps = 417 for the shipped box-out) and multiplies back, so the
        // final half-height is 302 where the true half is 303. The reveal therefore stops one
        // pixel inside the region on the short axis, and the frame's own redraw covers the seam.
        // Pinned because "fix" here would mean diverging from the original by a pixel.
        int steps = ScreenTransitionBox.StepCount(W, H);
        (_, int by, _, int bh) = ScreenTransitionBox.BoxAt(X, Y, W, H, steps);

        Assert.Equal(302 * 2, bh);
        Assert.Equal(Y + 1, by);
    }

    [Fact]
    public void TheBoxStaysCentredAtEveryStep() {
        int steps = ScreenTransitionBox.StepCount(W, H);
        for (var step = 1; step <= steps; step++) {
            (int bx, int by, int bw, int bh) = ScreenTransitionBox.BoxAt(X, Y, W, H, step);
            Assert.Equal(X + W / 2, bx + (bw / 2));
            Assert.Equal(Y + H / 2, by + (bh / 2));
        }
    }

    [Fact]
    public void TheBoxGrowsMonotonically() {
        int steps = ScreenTransitionBox.StepCount(W, H);
        var lastW = -1;
        var lastH = -1;
        for (var step = 1; step <= steps; step++) {
            (_, _, int bw, int bh) = ScreenTransitionBox.BoxAt(X, Y, W, H, step);
            Assert.True(bw >= lastW, "width went backwards at step " + step);
            Assert.True(bh >= lastH, "height went backwards at step " + step);
            lastW = bw;
            lastH = bh;
        }
    }

    [Fact]
    public void TheTwoDirectionsShareTheGeometryAndDifferOnlyInWhereTheyStart() {
        Assert.Equal(1, ScreenTransitionBox.BoxOutFirstStep);
        Assert.Equal(ScreenTransitionBox.StepCount(W, H), ScreenTransitionBox.BoxInFirstStep(W, H));
    }

    [Fact]
    public void ADegenerateRegionDoesNotDivideByZero() {
        (int bx, int by, int bw, int bh) = ScreenTransitionBox.BoxAt(10, 20, 1, 1, 1);

        Assert.Equal((10, 20, 0, 0), (bx, by, bw, bh));
    }

    [Fact]
    public void BoxINCopiesTheCOMPLEMENTOfTheBoxNotTheBox() {
        // The whole difference between the two effects, and it is NOT the step direction alone.
        // Reproducing box-in as "box-out with the loop reversed" plays a centre-out wipe backwards,
        // which is a different picture.
        const int steps = 100;
        var strips = ScreenTransitionBox.RevealedByBoxIn(0, 0, 400, 200, 50).ToList();
        (int left, int top, int boxWidth, int boxHeight) =
            ScreenTransitionBox.BoxAt(0, 0, 400, 200, 50);

        Assert.Equal(4, strips.Count);
        Assert.Equal((400 * 200) - (boxWidth * boxHeight), strips.Sum(s => s.Width * s.Height));
        foreach ((int sx, int sy, int sw, int sh) in strips) {
            bool disjoint = sx + sw <= left || sx >= left + boxWidth
                || sy + sh <= top || sy >= top + boxHeight;
            Assert.True(disjoint || sw == 0 || sh == 0,
                $"strip ({sx},{sy},{sw},{sh}) overlaps the box");
        }
        _ = steps;
    }

    [Fact]
    public void TheSHORTAxisFallsAPixelShortAndTheBoxNEVERQUITECloses() {
        // *** THE STEP COUNT IS THE LONG AXIS, SO THE SHORT ONE TRUNCATES. *** With 725 steps the
        // short half-extent advances by 303000/725 = 417 thousandths a step, and 417 x 725 / 1000
        // is 302 rather than 303. So the last box is two pixels short of the area's height and
        // box-in leaves a one-pixel band along the top and the bottom.
        //
        // This is the original's own arithmetic, not a rounding choice of ours — pinned so that
        // "the wipe does not quite finish" is recognised as faithful rather than fixed.
        int steps = ScreenTransitionBox.StepCount(W, H);
        (int _, int _, int outWidth, int outHeight) =
            ScreenTransitionBox.RevealedByBoxOut(X, Y, W, H, steps);

        Assert.Equal(W, outWidth);
        Assert.Equal(H - 2, outHeight);
        Assert.Equal(W * 2, ScreenTransitionBox.RevealedByBoxIn(X, Y, W, H, steps)
            .Sum(s => s.Width * s.Height));
    }
}
