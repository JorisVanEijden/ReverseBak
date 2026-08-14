namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// Whether a cast connects. Almost nothing can miss, and the exemptions are not in the spell data.
/// </summary>
public class SpellHitResolutionTests {
    [Fact]
    public void AnOffensiveCastAtARealTargetCanMiss() {
        Assert.True(SpellHitResolution.CanMiss(0, costWasNegated: false, hasTarget: true));
    }

    [Fact]
    public void EveryOtherTargetingTypeAutoApplies() {
        // Self-spells, buffs and grid effects simply land.
        for (var type = 1; type <= 8; type++) {
            Assert.False(SpellHitResolution.CanMiss(type, costWasNegated: false, hasTarget: true));
        }
    }

    [Fact]
    public void NoTargetMeansNoRoll() {
        Assert.False(SpellHitResolution.CanMiss(0, costWasNegated: false, hasTarget: false));
    }

    [Fact]
    public void ANegativeCostTakesTheCastOffTheToHitPathEntirely() {
        // The same sign the prologue strips also exempts the cast from rolling — nothing in the
        // spell record says so.
        Assert.False(SpellHitResolution.CanMiss(0, costWasNegated: true, hasTarget: true));
    }

    [Fact]
    public void ACastThatDoesNotRollCountsAsAHit() {
        // Set to 1 rather than left unset, so nothing downstream distinguishes "did not roll" from
        // "rolled well".
        Assert.True(SpellHitResolution.AutomaticResult);
    }

    [Fact]
    public void SpellAccuracyIsTheRangedFormulaWithNoAmmunition() {
        Assert.True(SpellHitResolution.UsesRangedAccuracyFormula);
        Assert.Equal(0, SpellHitResolution.AmmunitionBonus);
    }
}
