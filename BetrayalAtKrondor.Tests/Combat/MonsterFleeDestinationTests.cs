namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using GameData.Resources.Combat;
using Xunit;

/// <summary>Where a routed monster runs to.</summary>
public class MonsterFleeDestinationTests {
    private static System.Func<int> Rolls(params int[] values) {
        var q = new Queue<int>(values);
        return () => q.Count > 0 ? q.Dequeue() : 0;
    }

    [Fact]
    public void MoraleZeroMeansItWillNotMoveEvenAfterRouting() {
        // *** A DIFFERENT guard from MonsterMorale.NeverFleesMorale (0xff). *** That one never
        // decides to flee; this one can be routed but will not run. Folding them into one "never
        // flees" value changes the behaviour of one group or the other.
        Assert.False(MonsterFleeDestination.WillMove(0));
        Assert.True(MonsterFleeDestination.WillMove(MonsterMorale.NeverFleesMorale));
        Assert.NotEqual(MonsterFleeDestination.WontMoveMorale, MonsterMorale.NeverFleesMorale);
    }

    [Fact]
    public void ItDoesNotRunToTheFURTHESTTile() {
        // *** The coin flip is the point. *** Every improvement is taken only on RND(100) > 50, so
        // the destination is biased high without being the maximum. Picking the highest reachable
        // row would be deterministic and pile every routed monster onto one edge.
        // Accept the first improvement (row 1), then refuse everything else for the whole scan.
        (int X, int Y)? first = MonsterFleeDestination.Choose((x, y) => false, Rolls(90));

        Assert.NotNull(first);
        Assert.True(first.Value.Y < MonsterFleeDestination.Rows - 1,
            "a refused improvement leaves it short of the far row");
    }

    [Fact]
    public void ARollOfExactlyFiftyIsRefused() {
        Assert.False(MonsterFleeDestination.AcceptsImprovement(50));
        Assert.True(MonsterFleeDestination.AcceptsImprovement(51));
    }

    [Fact]
    public void RefusingEveryImprovementLeavesNoDestinationAtAll() {
        // An ordinary outcome, not an error: the monster simply stands still.
        Assert.Null(MonsterFleeDestination.Choose((x, y) => false, () => 0));
    }

    [Fact]
    public void ABlockedGridAlsoYieldsNothing() {
        Assert.Null(MonsterFleeDestination.Choose((x, y) => true, () => 100));
    }

    [Fact]
    public void AcceptingEveryImprovementReachesTheFarRow() {
        (int X, int Y)? far = MonsterFleeDestination.Choose((x, y) => false, () => 100);

        Assert.NotNull(far);
        Assert.Equal(MonsterFleeDestination.Rows - 1, far.Value.Y);
    }

    [Fact]
    public void FleeingDropsWhateverItWasFighting() {
        Assert.True(MonsterFleeDestination.ClearsTarget);
    }
}
