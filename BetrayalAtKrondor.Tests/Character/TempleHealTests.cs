namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Data;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Temple pricing (charscreen_temple_heal_price) and the heal amount's three behaviours.
/// </summary>
public class TempleHealTests {
    private static ActorConditions With(params (ActorCondition Condition, int Rank)[] set) {
        var conditions = new ActorConditions();
        foreach ((ActorCondition condition, int rank) in set) {
            ConditionEngine.Apply(conditions, condition, rank);
        }
        return conditions;
    }

    [Fact]
    public void AHealthyCharacterCostsNothingAndReadsAsNeedingNothing() {
        var healthy = new ActorConditions();

        Assert.Equal(0, TempleHeal.Price(healthy, 100));
        Assert.False(TempleHeal.NeedsHealing(healthy));
    }

    [Theory]
    [InlineData(ActorCondition.Sick, 5, 5 * 4 + 10)]
    [InlineData(ActorCondition.Plagued, 5, 5 * 10 + 10)]
    [InlineData(ActorCondition.Poisoned, 5, 5 * 10 + 10)]
    [InlineData(ActorCondition.Drunk, 5, 5 * 3 + 10)]
    [InlineData(ActorCondition.Starving, 5, 5 * 2 + 10)]
    [InlineData(ActorCondition.NearDeath, 5, 5 * 30 + 10)]
    public void EachAfflictionIsChargedAtItsOwnRatePlusAFlatFee(
        ActorCondition condition, int rank, long expected) {
        Assert.Equal(expected, TempleHeal.Price(With((condition, rank)), 100));
    }

    [Fact]
    public void HealingIsNotChargedForAtAll() {
        // The one beneficial entry in the set; the original skips it before the flat fee, so a
        // character whose only condition is Healing still prices at zero.
        ActorConditions onlyHealing = With((ActorCondition.Healing, 50));

        Assert.Equal(0, TempleHeal.Price(onlyHealing, 100));
        Assert.False(TempleHeal.NeedsHealing(onlyHealing));
    }

    [Fact]
    public void HealingDoesNotAddItsFlatFeeAlongsideOtherAfflictions() {
        ActorConditions both = With((ActorCondition.Sick, 5), (ActorCondition.Healing, 50));

        Assert.Equal(TempleHeal.Price(With((ActorCondition.Sick, 5)), 100),
            TempleHeal.Price(both, 100));
    }

    [Fact]
    public void AfflictionsAccumulateEachWithItsOwnFee() {
        ActorConditions ill = With((ActorCondition.Sick, 2), (ActorCondition.Poisoned, 3));

        Assert.Equal((2 * 4 + 10) + (3 * 10 + 10), TempleHeal.Price(ill, 100));
    }

    [Fact]
    public void GoingNearDeathWipesEveryOtherChargeFromTheBill() {
        // Not a pricing rule — raising Near-death clears every other affliction outright
        // (ConditionEngine's "collapsed"), so the bill for someone who just went down is exactly
        // the near-death charge however ill they were a moment earlier.
        ActorConditions ill = With(
            (ActorCondition.Sick, 90), (ActorCondition.Poisoned, 90), (ActorCondition.NearDeath, 3));

        Assert.Equal(3 * 30 + 10, TempleHeal.Price(ill, 100));
    }

    [Fact]
    public void NearDeathDominatesThePrice() {
        long nearDeath = TempleHeal.Price(With((ActorCondition.NearDeath, 10)), 100);
        long starving = TempleHeal.Price(With((ActorCondition.Starving, 10)), 100);

        Assert.True(nearDeath > starving * 5, $"near-death {nearDeath} vs starving {starving}");
    }

    [Fact]
    public void TheTemplePercentageScalesTheWholeBillOnce() {
        ActorConditions ill = With((ActorCondition.Sick, 3), (ActorCondition.Poisoned, 3));
        long face = TempleHeal.Price(ill, 100);

        Assert.Equal(face / 2, TempleHeal.Price(ill, 50));
        Assert.Equal(face * 2, TempleHeal.Price(ill, 200));
        Assert.Equal(0, TempleHeal.Price(ill, 0));
    }

    [Fact]
    public void TheScalingTruncatesOnceAtTheEndNotPerCondition() {
        // Two conditions at 25 each: per-condition rounding at 33% would lose more than one
        // truncation of the total does.
        ActorConditions ill = With((ActorCondition.Sick, 1), (ActorCondition.Drunk, 1));
        long face = TempleHeal.Price(ill, 100);   // (4+10) + (3+10) = 27

        Assert.Equal(27, face);
        Assert.Equal(27 * 33 / 100, TempleHeal.Price(ill, 33));   // 8, not 4+4=8 by luck — pinned
    }

    [Fact]
    public void AHealAboveOneHundredNeitherCuresNorGivesBack() {
        // The original gates the cure on == 100 and the give-back on < 100, so an amount above 100
        // gets neither and is the most generous input of the three.
        ActorStat[] stats = FullStats();
        stats[(int)ActorAttribute.Health].Base = 0;
        stats[(int)ActorAttribute.Stamina].Base = 0;
        ActorConditions afflicted = With((ActorCondition.Sick, 40));

        CharacterHeal.Apply(stats, afflicted, 150);

        int pool = stats[(int)ActorAttribute.Health].Base + stats[(int)ActorAttribute.Stamina].Base;
        Assert.Equal(60, pool);                                  // filled outright, nothing taken back
        Assert.Equal(40, afflicted[ActorCondition.Sick]);        // and not cured
    }

    [Fact]
    public void AHealBelowOneHundredLandsAtEightyPercent() {
        ActorStat[] stats = FullStats();
        stats[(int)ActorAttribute.Health].Base = 0;
        stats[(int)ActorAttribute.Stamina].Base = 0;

        CharacterHeal.Apply(stats, null, 50);

        int pool = stats[(int)ActorAttribute.Health].Base + stats[(int)ActorAttribute.Stamina].Base;
        Assert.Equal(48, pool);   // 80% of 60
    }

    [Fact]
    public void OnlyExactlyOneHundredCures() {
        ActorConditions afflicted = With((ActorCondition.Sick, 40));

        CharacterHeal.Apply(FullStats(), afflicted, 100);

        Assert.Equal(0, afflicted[ActorCondition.Sick]);
    }

    [Fact]
    public void ThePartyHealCoversEveryMemberGiven() {
        var members = new List<(ActorStat[], ActorConditions)>();
        for (var i = 0; i < 3; i++) {
            ActorStat[] stats = FullStats();
            stats[(int)ActorAttribute.Health].Base = 1;
            stats[(int)ActorAttribute.Stamina].Base = 1;
            members.Add((stats, With((ActorCondition.Sick, 20))));
        }

        Assert.True(CharacterHeal.ApplyToParty(members, 100));

        foreach ((ActorStat[] stats, ActorConditions conditions) in members) {
            Assert.Equal(60,
                stats[(int)ActorAttribute.Health].Base + stats[(int)ActorAttribute.Stamina].Base);
            Assert.Equal(0, conditions[ActorCondition.Sick]);
        }
    }

    private static ActorStat[] FullStats() {
        var stats = new ActorStat[ActorAttributeValues.Count];
        for (var i = 0; i < stats.Length; i++) {
            stats[i] = new ActorStat { Base = 30, Max = 30 };
        }
        return stats;
    }
}
