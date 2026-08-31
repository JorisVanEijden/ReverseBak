namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Clicking your own body during a fight — <c>check_defend</c> (COMBAT.C:2380).
/// </summary>
public class SelfClickCommandTests {
    [Fact]
    public void AHurtCharacterRESTSOnALeftClickWhereAHealthyOneDEFENDS() {
        // The same button that attacks an enemy recovers you when you are hurt, so neither command
        // is reachable only from its menu button.
        Assert.True(DefendAction.LeftClickDefends(pool: 80, maxPool: 100));
        Assert.True(DefendAction.LeftClickDefends(pool: 100, maxPool: 100));
        Assert.False(DefendAction.LeftClickDefends(pool: 79, maxPool: 100));
        Assert.False(DefendAction.LeftClickDefends(pool: 0, maxPool: 100));
    }

    [Fact]
    public void TheThresholdIsOverBOTHPools_NotHealthAlone() {
        // combat_actor_stat_percent's with_modifier arm sums health and stamina on BOTH sides, and
        // the two readings disagree across the boundary: health 70/100 alone is 70% and would rest,
        // but with a full 50/50 stamina the pool is 120/150 = 80% and defends.
        Assert.False(DefendAction.LeftClickDefends(pool: 70, maxPool: 100));
        Assert.True(DefendAction.LeftClickDefends(pool: 70 + 50, maxPool: 100 + 50));
        Assert.Equal(0x50, DefendAction.RestBelowPoolPercent);
    }

    [Fact]
    public void ACombatantWithNoStatBlockDefendsRatherThanHealingForFree() {
        Assert.True(DefendAction.LeftClickDefends(pool: 0, maxPool: 0));
    }
}

/// <summary>
/// Which neighbours count as adjacent for melee — <c>combatgrid_actors_ortho_adj</c>.
/// </summary>
public class OrthogonalAdjacencyTests {
    [Fact]
    public void ADiagonalNeighbourIsNOTAdjacent() {
        // One Chebyshev step away and still not adjacent: the swing refuses a diagonal target and
        // the thrust walks a step before striking one. Reusing ChebyshevDistance == 1 here would
        // let a swing hit a diagonal, which the original never does.
        Assert.True(CombatGrid.OrthogonallyAdjacent(4, 4, 5, 4));
        Assert.True(CombatGrid.OrthogonallyAdjacent(4, 4, 4, 3));
        Assert.False(CombatGrid.OrthogonallyAdjacent(4, 4, 5, 5));
        Assert.Equal(1, CombatGrid.ChebyshevDistance(4, 4, 5, 5));
        Assert.False(CombatGrid.OrthogonallyAdjacent(4, 4, 4, 4));
        Assert.False(CombatGrid.OrthogonallyAdjacent(4, 4, 6, 4));
    }
}
