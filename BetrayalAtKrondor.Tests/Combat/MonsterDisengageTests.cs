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
        Assert.True(MonsterSpellcasting.DefersToMovementAi(foundSomewhereBetter: false, roll: 99, mayAttack: true));
    }

    [Fact]
    public void AndSoDoesOneInSevenThatFoundSomewhereToGo() {
        Assert.True(MonsterSpellcasting.DefersToMovementAi(foundSomewhereBetter: true, roll: 14, mayAttack: true));
        Assert.False(MonsterSpellcasting.DefersToMovementAi(foundSomewhereBetter: true, roll: 15, mayAttack: true));
    }

    [Fact]
    public void WithoutMayAttackTheActorWalksEvenWithNowhereBetter() {
        // The third argument is not decoration: `... && may_attack` (CBTAITRN.C:73-83). The one
        // caller that passes zero is combataipath_select_action's own low-stamina fallback, and
        // without the term it would recurse straight back into itself.
        Assert.False(MonsterSpellcasting.DefersToMovementAi(
            foundSomewhereBetter: false, roll: 99, mayAttack: false));
        Assert.False(MonsterSpellcasting.DefersToMovementAi(
            foundSomewhereBetter: true, roll: 0, mayAttack: false));
    }

    [Fact]
    public void AnEngagedCasterSpendsItsTurnEitherWay() {
        // The pre-check's return value comes from the engagement test alone, so the caller records
        // "acted" whether the retreat happened or not.
        Assert.True(MonsterSpellcasting.DisengageReturnsEngagementNotSuccess);
    }
}
