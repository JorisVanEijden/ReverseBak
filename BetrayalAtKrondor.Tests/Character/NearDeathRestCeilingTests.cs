namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using Xunit;

/// <summary>
/// What an hour of rest does to a Near-death member — the ceiling that makes "Camp until Healed"
/// unfinishable until the rank decays (TASK-605).
/// </summary>
/// <remarks>
/// <c>STAT.C:206-211</c>, inside <c>stat_combatant_modify</c>'s stat-<c>0x10</c> branch, REPLACES
/// the heal target rather than scaling it:
/// <code>
///     target = (mode * uMaxSum) / 100;
///     if (charSlot != 0) { rank = ranks[rowIdx][6];
///                          if (rank != 0) target = ((100 - rank) * 0x1e) / 100 + 1; }
/// </code>
/// So the 80% ceiling a camp rest normally heals to does not apply to a Near-death member at all.
/// <see cref="StatEngine.ModifyHealthPool"/> implements it, but takes the rank as a DEFAULTED
/// argument, and <see cref="UpkeepEngine.ApplyHour"/> did not pass it — so rest healing ignored
/// Near-death entirely and took two members from a pool of 10 to the full 80% in 90 hours.
/// </remarks>
public class NearDeathRestCeilingTests {
    private const int Hp = (int)ActorAttribute.Health;
    private const int Sta = (int)ActorAttribute.Stamina;

    /// <summary>Gorath as he stands in dir.G01/SAVE98: 10/60 health, 0/65 stamina.</summary>
    private static (ActorStat Health, ActorStat Stamina, ActorConditions Conditions) Gorath(
        int nearDeathRank) {
        var health = new ActorStat { Base = 10, Effective = 10, Max = 60 };
        var stamina = new ActorStat { Base = 0, Effective = 0, Max = 65 };
        var conditions = new ActorConditions();
        if (nearDeathRank != 0) {
            ConditionEngine.Apply(conditions, ActorCondition.NearDeath, nearDeathRank);
        }
        return (health, stamina, conditions);
    }

    private static int RestOneHour(
        ActorStat health, ActorStat stamina, ActorConditions conditions, int characterIndex = 1) {
        UpkeepEngine.ApplyHour(health, stamina, conditions, characterIndex,
            UpkeepEngine.PartialRestQuality);
        return health.Base + stamina.Base;
    }

    [Fact]
    public void AtNearDeath100AnHourOfRestCannotLiftThePoolAtAll() {
        // target = (100 - 100) * 30 / 100 + 1 = 1, and the pool is already 10, so `sum < target`
        // is false and nothing is added. The member does not heal while the rank stands.
        (ActorStat health, ActorStat stamina, ActorConditions conditions) = Gorath(100);

        Assert.Equal(10, RestOneHour(health, stamina, conditions));
    }

    [Fact]
    public void AWholeDayOfRestAtNearDeath100StillLiftsNothing() {
        // The control for "maybe it is just slow": 24 hours, same answer. This is the reading that
        // makes a Near-death party unable to finish a Camp until Healed.
        (ActorStat health, ActorStat stamina, ActorConditions conditions) = Gorath(100);
        for (var hour = 0; hour < 24; hour++) {
            RestOneHour(health, stamina, conditions);
        }

        Assert.Equal(10, health.Base + stamina.Base);
    }

    [Fact]
    public void WithoutNearDeathTheSameHourHeals() {
        // The positive control. Without it every assertion above would also pass against a rest
        // that heals nobody, which is a different bug wearing the same numbers.
        (ActorStat health, ActorStat stamina, ActorConditions conditions) = Gorath(0);

        Assert.Equal(11, RestOneHour(health, stamina, conditions));
    }

    [Fact]
    public void TheCeilingRISESAsTheRankDecays() {
        // ((100 - 40) * 30) / 100 + 1 = 19, so a member two thirds of the way back can be rested to
        // 19 and no further — the ceiling tracks the rank rather than switching off with it.
        (ActorStat health, ActorStat stamina, ActorConditions conditions) = Gorath(40);
        for (var hour = 0; hour < 48; hour++) {
            RestOneHour(health, stamina, conditions);
        }

        Assert.Equal(19, health.Base + stamina.Base);
    }
}
