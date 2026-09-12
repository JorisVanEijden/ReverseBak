namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;

using GameData.Resources.Combat;

using Xunit;

/// <summary>
/// Where a monster walks to reach its target — BESIDE it, never onto it (TASK-437).
/// </summary>
/// <remarks>
/// <b>The fixture is the measurement.</b> Both games were driven into the same chapter-1 encounter
/// from the same save on 2026-09-12, rosters verified identical cell for cell, and the same Defend
/// played on both sides. On the enemy's turn the original's monster at (6,7), speed 3, went to
/// <b>(5,4)</b> with <c>inner-&gt;dest</c> reading <b>(5,1)</b>; ours walked at the target's own
/// (4,1) and went to <b>(4,4)</b>. These pin the rule that produces (5,1).
/// </remarks>
public class AiApproachCellTests {
    // The monster, its target and the two other party members, exactly as they stood.
    private const int MonsterX = 6, MonsterY = 7;
    private const int TargetX = 4, TargetY = 1;

    [Fact]
    public void ItIsTheXAdjacentCellOnTheACTORsSide() {
        // CMBTAI.C:331-334 — approachX = target.gridX + (target.gridX < actor.gridX ? 1 : -1).
        // The monster is to the RIGHT of its target, so it approaches from the right.
        Assert.Equal((5, 1), CombatAi.ApproachCell(MonsterX, MonsterY, TargetX, TargetY));

        // And from the left, the mirror — not a fixed +1, which would pass the case above by luck.
        Assert.Equal((3, 1), CombatAi.ApproachCell(0, 7, TargetX, TargetY));
    }

    [Fact]
    public void NEVERTheTargetsOwnCell() {
        // The defect in one line: a target's cell is occupied by the target, so walking at it can
        // only ever stop short somewhere the rule did not choose.
        for (var ax = 0; ax < 8; ax++) {
            for (var ay = 0; ay < 13; ay++) {
                if (ax == TargetX && ay == TargetY) {
                    continue;
                }
                Assert.NotEqual((TargetX, TargetY),
                    CombatAi.ApproachCell(ax, ay, TargetX, TargetY, (x, y) => false));
            }
        }
    }

    [Fact]
    public void ABlockedXCellFallsBackToTheYAdjacentOne() {
        // CMBTAI.C:361-366. The fallback keeps the TARGET's x and moves in y, so it is the cell
        // above or below the target, not a diagonal.
        var blocked = new HashSet<(int, int)> { (5, 1) };
        Assert.Equal((4, 2),
            CombatAi.ApproachCell(MonsterX, MonsterY, TargetX, TargetY,
                (x, y) => blocked.Contains((x, y))));
    }

    [Fact]
    public void ACellTheACTORIsStandingOnCountsAsAvailable() {
        // It reads as blocked only because the actor occupies it. Without this the monster already
        // in position would walk away to the fallback cell and back again for ever.
        Assert.Equal((5, 1),
            CombatAi.ApproachCell(5, 1, TargetX, TargetY, (x, y) => true));
    }

    [Fact]
    public void WithNoGridItTakesTheXCellRatherThanInventingARule() {
        // A caller with no blocked test gets the preferred cell, not a silently different one.
        Assert.Equal((5, 1), CombatAi.ApproachCell(MonsterX, MonsterY, TargetX, TargetY, null));
    }
}
