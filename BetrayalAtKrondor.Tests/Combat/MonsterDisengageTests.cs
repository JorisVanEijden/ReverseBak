namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The pre-check that runs before a caster's action row: a monster in melee contact retreats
/// instead of casting.
/// </summary>
public class MonsterDisengageTests {
    [Fact]
    public void AnEnemyInContactStopsTheCasterCasting() {
        // A real tactic against caster monsters, and nothing in the pattern table hints at it.
        Assert.True(MonsterSpellcasting.MustDisengageBeforeCasting(1));
        Assert.True(MonsterSpellcasting.MustDisengageBeforeCasting(0));
    }

    [Fact]
    public void AnEnemyOneCellFurtherOutDoesNot() {
        Assert.False(MonsterSpellcasting.MustDisengageBeforeCasting(2));
        Assert.False(MonsterSpellcasting.MustDisengageBeforeCasting(9));
    }

    [Fact]
    public void AStrictlySaferCellAlwaysWins() {
        Assert.True(MonsterSpellcasting.RetreatCellIsBetter(candidateDistance: 5, bestDistance: 4,
            tieRoll: 99));
    }

    [Fact]
    public void ACloserCellNeverDoes() {
        Assert.False(MonsterSpellcasting.RetreatCellIsBetter(candidateDistance: 3, bestDistance: 4,
            tieRoll: 0));
    }

    [Fact]
    public void AnEqualCellSwitchesOnFiftyOnePercentNotAcoinFlip() {
        Assert.True(MonsterSpellcasting.RetreatCellIsBetter(candidateDistance: 4, bestDistance: 4,
            tieRoll: 50));
        Assert.False(MonsterSpellcasting.RetreatCellIsBetter(candidateDistance: 4, bestDistance: 4,
            tieRoll: 51));
    }

    [Fact]
    public void ACorneredCasterFallsThroughToTheMovementAi() {
        Assert.True(MonsterSpellcasting.DefersToMovementAi(foundSomewhereBetter: false, roll: 99));
    }

    [Fact]
    public void AndSoDoesOneInSevenThatFoundSomewhereToGo() {
        Assert.True(MonsterSpellcasting.DefersToMovementAi(foundSomewhereBetter: true, roll: 14));
        Assert.False(MonsterSpellcasting.DefersToMovementAi(foundSomewhereBetter: true, roll: 15));
    }

    [Fact]
    public void AnEngagedCasterSpendsItsTurnEitherWay() {
        // The pre-check's return value comes from the engagement test alone, so the caller records
        // "acted" whether the retreat happened or not.
        Assert.True(MonsterSpellcasting.DisengageReturnsEngagementNotSuccess);
    }
}
