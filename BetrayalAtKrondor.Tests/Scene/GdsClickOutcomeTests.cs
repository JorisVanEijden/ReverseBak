namespace BetrayalAtKrondor.Tests.Scene;

using GameData.Resources.Scene;
using Xunit;

/// <summary>
/// What a location hotspot's dialog result does to the scene, and the barding credit that travels
/// through a global to get there.
/// </summary>
public class GdsClickOutcomeTests {
    [Fact]
    public void FiveResultsAreTranslatedAndTheMappingIsArbitrary() {
        // Not the identity, not a negation, not ordered — there is nothing to derive it from.
        Assert.Equal(0, GdsSceneRules.OutcomeFor(-1, currentOutcome: 99));
        Assert.Equal(5, GdsSceneRules.OutcomeFor(-2, currentOutcome: 99));
        Assert.Equal(7, GdsSceneRules.OutcomeFor(-3, currentOutcome: 99));
        Assert.Equal(3, GdsSceneRules.OutcomeFor(-4, currentOutcome: 99));
        Assert.Equal(10, GdsSceneRules.OutcomeFor(-5, currentOutcome: 99));
    }

    [Fact]
    public void EverythingElseLeavesTheSceneAlone() {
        // How a dialog that merely said something falls through.
        Assert.Equal(99, GdsSceneRules.OutcomeFor(0, currentOutcome: 99));
        Assert.Equal(99, GdsSceneRules.OutcomeFor(4, currentOutcome: 99));
        Assert.Equal(99, GdsSceneRules.OutcomeFor(-6, currentOutcome: 99));
    }

    [Fact]
    public void OnlyOneOutcomeInvalidatesThePalette() {
        Assert.True(GdsSceneRules.InvalidatesPalette(-2));
        foreach (int result in new[] { -1, -3, -4, -5 }) {
            Assert.False(GdsSceneRules.InvalidatesPalette(result));
        }
    }

    [Fact]
    public void BardingEarningsStopAccumulatingAtTwoHundredAndFifty() {
        Assert.Equal(200, GdsSceneRules.BankedBardingReward(200));
        Assert.Equal(250, GdsSceneRules.BankedBardingReward(250));
        Assert.Equal(250, GdsSceneRules.BankedBardingReward(9000));
    }

    [Fact]
    public void AndTheCreditIsPerLocationNotPerParty() {
        // The global is scratch for one visit; carrying it would pay a second innkeeper for the
        // same performance.
        Assert.True(GdsSceneRules.BardingRewardGlobalIsScratch);
    }
}
