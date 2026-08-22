namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>The melee-or-missile routine's choice.</summary>
public class MonsterActionChoiceTests {
    [Fact]
    public void AdjacentIsAlwaysMelee_NoRoll() {
        for (var roll = 0; roll < MonsterActionChoice.ChoiceDie; roll++) {
            Assert.Equal(MonsterActionChoice.Action.Melee, MonsterActionChoice.Choose(1, roll));
            Assert.Equal(MonsterActionChoice.Action.Melee, MonsterActionChoice.Choose(0, roll));
        }
    }

    [Fact]
    public void BeyondMeleeTheCLOSERTargetIsMoreLikelyToBeCastAt() {
        // *** The counter-intuitive rule. *** RND(10) >= distance, so a near target is cast at often
        // and a far one rarely - the opposite of "spells are the long-range option". Inverting the
        // comparison would have monsters sniping spells across the arena and meleeing nothing.
        Assert.Equal(8, MonsterActionChoice.CastChanceInTen(2));
        Assert.Equal(5, MonsterActionChoice.CastChanceInTen(5));
        Assert.Equal(1, MonsterActionChoice.CastChanceInTen(9));
    }

    [Fact]
    public void AtTenTilesOrMoreItCanNEVERCast() {
        // A d10 rolls 0..9, so it cannot reach 10. Every roll shoots.
        for (var roll = 0; roll < MonsterActionChoice.ChoiceDie; roll++) {
            Assert.Equal(MonsterActionChoice.Action.Ranged, MonsterActionChoice.Choose(10, roll));
            Assert.Equal(MonsterActionChoice.Action.Ranged, MonsterActionChoice.Choose(12, roll));
        }
        Assert.Equal(0, MonsterActionChoice.CastChanceInTen(10));
    }

    [Fact]
    public void TheRollBoundaryFallsWhereTheOriginalPutsIt() {
        // >= not >, so a roll equal to the distance casts.
        Assert.Equal(MonsterActionChoice.Action.Cast, MonsterActionChoice.Choose(distance: 5, roll: 5));
        Assert.Equal(MonsterActionChoice.Action.Ranged, MonsterActionChoice.Choose(distance: 5, roll: 4));
    }

    [Fact]
    public void TheConstantsAreTheOriginals() {
        Assert.Equal(4, MonsterActionChoice.CastTargetingType);
        Assert.Equal(8, MonsterActionChoice.QuarrelType);
        Assert.Equal((0x19, 0x31), MonsterActionChoice.MeleeDelayRange);
    }
}
