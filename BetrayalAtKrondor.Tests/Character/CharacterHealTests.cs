namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using Xunit;

/// <summary>
/// The dialog/chapter heal (stat_combatant_heal). The case worth pinning is that its "amount" is not
/// an amount: 100 is a full restore that also cures everything, and every other value lands in the
/// same place.
/// </summary>
public class CharacterHealTests {
    private static ActorStat[] Hurt(byte health = 5, byte stamina = 5, byte max = 50) {
        var stats = new ActorStat[16];
        for (var i = 0; i < stats.Length; i++) {
            stats[i] = new ActorStat { Base = 0, Max = max };
        }
        stats[(int)ActorAttribute.Health] = new ActorStat { Base = health, Max = max };
        stats[(int)ActorAttribute.Stamina] = new ActorStat { Base = stamina, Max = max };
        return stats;
    }

    private static int Pool(ActorStat[] s) =>
        s[(int)ActorAttribute.Health].Base + s[(int)ActorAttribute.Stamina].Base;

    private static int MaxPool(ActorStat[] s) =>
        s[(int)ActorAttribute.Health].Max + s[(int)ActorAttribute.Stamina].Max;

    [Fact]
    public void AFullHealFillsThePool() {
        ActorStat[] stats = Hurt();

        bool full = CharacterHeal.Apply(stats, new ActorConditions(), CharacterHeal.FullHealAmount);

        Assert.True(full);
        Assert.Equal(MaxPool(stats), Pool(stats));
    }

    [Fact]
    public void AndCuresEverySingleAffliction() {
        var conditions = new ActorConditions();
        for (var i = 0; i < ActorConditions.Count; i++) {
            conditions[(ActorCondition)i] = 40;
        }

        CharacterHeal.Apply(Hurt(), conditions, CharacterHeal.FullHealAmount);

        for (var i = 0; i < ActorConditions.Count; i++) {
            Assert.Equal(0, conditions[(ActorCondition)i]);
        }
    }

    [Fact]
    public void APartialHealStopsShortOfFull() {
        ActorStat[] stats = Hurt();

        bool full = CharacterHeal.Apply(stats, new ActorConditions(), 50);

        Assert.False(full);
        Assert.True(Pool(stats) < MaxPool(stats), "a partial heal must not fill the pool");
        Assert.True(Pool(stats) > 10, "but it should still heal a great deal");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(99)]
    public void EveryNonHundredAmountLandsInTheSamePlace(int amount) {
        // The amount is a flag, not a magnitude: fill, then hand back a fifth. So 1 and 99 heal
        // identically, which is the whole reason this is not "add N hit points".
        ActorStat[] stats = Hurt();

        CharacterHeal.Apply(stats, new ActorConditions(), amount);

        Assert.Equal(MaxPool(stats) * CharacterHeal.PartialHealPercent / 100, Pool(stats));
    }

    [Fact]
    public void APartialHealLeavesAfflictionsAlone() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Poisoned] = 30;

        CharacterHeal.Apply(Hurt(), conditions, 50);

        Assert.Equal(30, conditions[ActorCondition.Poisoned]);
    }

    [Fact]
    public void ACollapsedCharacterIsStillCappedAtTheirSliver() {
        // Near-death caps the pool however generous the heal, unless the heal is the full one that
        // clears Near-death first.
        var conditions = new ActorConditions();
        conditions[ActorCondition.NearDeath] = 80;
        ActorStat[] stats = Hurt(health: 1, stamina: 0);

        CharacterHeal.Apply(stats, conditions, 50);

        Assert.True(Pool(stats) < MaxPool(stats) / 2,
            $"near-death should cap the heal, pool was {Pool(stats)}");
    }
}
