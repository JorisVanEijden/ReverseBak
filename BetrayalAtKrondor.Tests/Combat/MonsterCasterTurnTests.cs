namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>A casting monster's turn.</summary>
public class MonsterCasterTurnTests {
    [Fact]
    public void ABetterCasterAcceptsARiskierShot() {
        // *** 4 - Casting/25. *** An expert targets someone standing among their allies; a novice
        // demands room. Treating skill as needing MORE clearance makes skilled casters the timid ones.
        Assert.Equal(4, MonsterCasterTurn.ClearanceFor(0));
        Assert.Equal(2, MonsterCasterTurn.ClearanceFor(50));
        Assert.Equal(0, MonsterCasterTurn.ClearanceFor(100));
    }

    [Fact]
    public void TargetingRunsTwice_TheSecondTimeWithNoClearance() {
        // Giving up after one pass would leave casters idle in crowded fights.
        Assert.Equal(0, MonsterCasterTurn.RetryClearance);
        Assert.True(MonsterCasterTurn.ClearanceFor(0) > MonsterCasterTurn.RetryClearance);
    }

    [Fact]
    public void TheHealthGateUsesASPECIFICThreshold_NotAnyOfThem() {
        // *** Same table as the rest recovery, used differently. *** RestAction asks whether health
        // clears ANY entry, which reduces to "alive" because six are zero. Here the original names
        // index 0 (and 1 on the retry), both of which are 10 - so a caster needs health above TEN.
        int[] ladder = CombatCapability.ShippedHealthThresholds;

        Assert.False(MonsterCasterTurn.HealthAllowsCasting(10, ladder,
            MonsterCasterTurn.FirstPassThresholdIndex));
        Assert.True(MonsterCasterTurn.HealthAllowsCasting(11, ladder,
            MonsterCasterTurn.FirstPassThresholdIndex));

        // ...while the same health of 5 is fine for resting.
        Assert.True(CombatCapability.ClearsAnyThreshold(5, ladder));
        Assert.False(MonsterCasterTurn.HealthAllowsCasting(5, ladder,
            MonsterCasterTurn.FirstPassThresholdIndex));
    }

    [Fact]
    public void BothPassesUseATenThreshold() {
        int[] ladder = CombatCapability.ShippedHealthThresholds;
        Assert.Equal(10, ladder[MonsterCasterTurn.FirstPassThresholdIndex]);
        Assert.Equal(10, ladder[MonsterCasterTurn.RetryThresholdIndex]);
    }

    [Fact]
    public void OnlyTheFirstPassNeedsLineOfSight() {
        // The retry goes straight to resolve_attack_attempt with no path test, so the fallback can
        // act through cover the first pass would have refused.
        Assert.True(MonsterCasterTurn.FirstPassNeedsLineOfSight);
        Assert.False(MonsterCasterTurn.RetryNeedsLineOfSight);
    }

    [Fact]
    public void AnAbsentLadderRefusesRatherThanThrowing() {
        Assert.False(MonsterCasterTurn.HealthAllowsCasting(99, null, 0));
    }
}
