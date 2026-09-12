namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using System;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Skipping a target nobody can stand next to — <c>combataipath_select_target</c>'s approach gate.
/// </summary>
public class ApproachBlockedTests {
    // The monster is west and south of the target, so the approach cells are the target's east and
    // north neighbours — ApproachCell's own arithmetic.
    private const int ActorX = 2;
    private const int ActorY = 2;
    private const int TargetX = 6;
    private const int TargetY = 6;

    private static Func<int, int, bool> Blocking(params (int X, int Y)[] cells) {
        var set = new HashSet<(int, int)>(cells);
        return (x, y) => set.Contains((x, y));
    }

    [Fact]
    public void NEITHERCellBlockedIsReachable() {
        Assert.False(CombatAi.ApproachIsBlocked(ActorX, ActorY, TargetX, TargetY, Blocking()));
    }

    [Fact]
    public void ONECellBlockedIsStillReachable_BecauseTheOtherIsTheFallback() {
        // ApproachCell prefers the x-adjacent cell and falls back to the y-adjacent one, so a gate
        // that refused on either would reject targets the walk could reach.
        Assert.False(CombatAi.ApproachIsBlocked(
            ActorX, ActorY, TargetX, TargetY, Blocking((TargetX - 1, TargetY))));
        Assert.False(CombatAi.ApproachIsBlocked(
            ActorX, ActorY, TargetX, TargetY, Blocking((TargetX, TargetY - 1))));
    }

    [Fact]
    public void BOTHCellsBlockedIsSkipped() {
        Assert.True(CombatAi.ApproachIsBlocked(ActorX, ActorY, TargetX, TargetY,
            Blocking((TargetX - 1, TargetY), (TargetX, TargetY - 1))));
    }

    [Fact]
    public void STANDINGOnABlockedApproachCellCountsAsReachable() {
        // The two equality arms: a monster already in contact reads its own cell as blocked, and
        // without them would disqualify the target it is standing next to.
        Assert.False(CombatAi.ApproachIsBlocked(TargetX - 1, TargetY, TargetX, TargetY,
            Blocking((TargetX - 1, TargetY), (TargetX, TargetY - 1))));
    }

    [Fact]
    public void NOGridTestLeavesTheRuleOff() {
        Assert.False(CombatAi.ApproachIsBlocked(ActorX, ActorY, TargetX, TargetY, null));
    }

    [Fact]
    public void THESelectorSkipsAnUnreachableCandidateAndTakesTheNextOne() {
        var candidates = new List<TargetCandidate> {
            new TargetCandidate { X = 3, Y = 2, ApproachBlocked = true },
            new TargetCandidate { X = 8, Y = 2 },
        };

        int chosen = CombatAi.SelectTarget(0, 2, candidates, maxDistance: 100,
            role: TargetRole.Anyone, minAllyClearance: 0);

        Assert.Equal(1, chosen);
    }
}
