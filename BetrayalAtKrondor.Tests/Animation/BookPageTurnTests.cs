namespace BetrayalAtKrondor.Tests.Animation;

using GameData.Resources.Animation;
using Xunit;

/// <summary>
/// The shape of the cutscene book-page turn (TASK-159 slice).
/// </summary>
/// <remarks>
/// <b>These pin the SHAPE and deliberately not the magnitude.</b> The step count is RE-derived but
/// the skew curve is our own approximation — <c>skew</c> appears nowhere in the reconstructed
/// source. A test asserting the 0.1 peak would pin a guess and make correcting it a test failure
/// rather than a one-line change.
/// </remarks>
public class BookPageTurnTests {
    [Fact]
    public void THETURNRunsTwentyOneSteps_Inclusive() =>
        // MaxBookStep is the last VALID step, not the count. Off by one here and the page never
        // fully closes.
        Assert.Equal(21, BookPageTurn.TotalSteps);

    [Fact]
    public void THEPAGEStartsFullWidthAndEndsClosed() {
        Assert.Equal(1.0, BookPageTurn.WidthFactorAt(0), 6);
        Assert.Equal(0.0, BookPageTurn.WidthFactorAt(BookPageTurn.TotalSteps), 6);
    }

    [Fact]
    public void THEWIDTHOnlyEverNarrows() {
        double previous = double.MaxValue;
        for (var step = 0; step <= BookPageTurn.TotalSteps; step++) {
            double width = BookPageTurn.WidthFactorAt(step);
            Assert.True(width < previous, $"width must decrease at every step; step {step}");
            previous = width;
        }
    }

    [Fact]
    public void ASTEPPastTheEndDoesNotTurnThePageInsideOut() =>
        // *** The clamp earns its place here. *** Arg2 comes from script data, and a mod-authored
        // value past the end would give a NEGATIVE width — a page drawn mirrored rather than closed.
        Assert.Equal(0.0, BookPageTurn.WidthFactorAt(999), 6);

    [Fact]
    public void ANEGATIVEStepIsTreatedAsTheStart() =>
        Assert.Equal(1.0, BookPageTurn.WidthFactorAt(-5), 6);

    [Fact]
    public void THESKEWIsFlatAtBothEnds() {
        // *** THE LOAD-BEARING PROPERTY, and it is both ends, not just the start. *** A curve that
        // finished mid-skew would snap visibly on the last frame and would not join up with the
        // next turn.
        Assert.Equal(0.0, BookPageTurn.SkewFractionAt(0), 6);
        Assert.Equal(0.0, BookPageTurn.SkewFractionAt(BookPageTurn.TotalSteps), 6);
    }

    [Fact]
    public void THESKEWPeaksInTheMiddleAndIsSymmetric() {
        // A half-sine arch. Symmetry is what makes the page look the same turning as it did opening.
        for (var step = 0; step <= BookPageTurn.TotalSteps; step++) {
            Assert.Equal(BookPageTurn.SkewFractionAt(step),
                BookPageTurn.SkewFractionAt(BookPageTurn.TotalSteps - step), 6);
        }
        Assert.True(BookPageTurn.SkewFractionAt(BookPageTurn.TotalSteps / 2) > 0.99);
    }

    [Fact]
    public void THESKEWNeverLeavesTheZeroToOneBand() =>
        // It scales a height, so a value outside 0..1 is a page skewed further than its own size.
        Assert.All(System.Linq.Enumerable.Range(0, BookPageTurn.TotalSteps + 1),
            step => Assert.InRange(BookPageTurn.SkewFractionAt(step), 0.0, 1.0));

    [Fact]
    public void SKEWATScalesTheFractionByTheHeight() {
        double mid = BookPageTurn.SkewAt(BookPageTurn.TotalSteps / 2, 1000);
        Assert.Equal(1000 * BookPageTurn.MaxSkewFraction * BookPageTurn.SkewFractionAt(BookPageTurn.TotalSteps / 2),
            mid, 6);
    }
}
