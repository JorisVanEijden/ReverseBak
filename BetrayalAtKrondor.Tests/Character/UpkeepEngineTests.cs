namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using Xunit;

/// <summary>
/// What an hour and a day of game time do to a party member — <c>gstate_advance_time</c> and its
/// hourly tick.
/// </summary>
public class UpkeepEngineTests {
    private const int Locklear = 0;
    private const int Gorath = 5;
    private const int Travelling = 0;
    private const int CampRest = UpkeepEngine.PartialRestQuality; // heals to 80%
    private const int FullRest = 133;                             // the other shipped rest value

    private static ActorStat Stat(byte value, byte max) => new ActorStat { Base = value, Max = max };

    // ---- regeneration -----------------------------------------------------

    [Fact]
    public void WalkingAroundHealsNothing() {
        ActorStat health = Stat(30, 60);
        ActorStat stamina = Stat(0, 40);

        UpkeepEngine.ApplyHour(health, stamina, new ActorConditions(), Locklear, Travelling);

        Assert.Equal(30, health.Base);
        Assert.Equal(0, stamina.Base);
    }

    [Fact]
    public void RestingHealsAPointAnHour() {
        ActorStat health = Stat(30, 60);
        ActorStat stamina = Stat(0, 40);

        UpkeepEngine.ApplyHour(health, stamina, new ActorConditions(), Locklear, FullRest);

        Assert.Equal(31, health.Base);
    }

    [Fact]
    public void BeingUnderHealingDoublesWhatRestGivesBack() {
        ActorStat health = Stat(30, 60);
        ActorStat stamina = Stat(0, 40);
        var conditions = new ActorConditions();
        conditions[ActorCondition.Healing] = 50;

        UpkeepEngine.ApplyHour(health, stamina, conditions, Locklear, FullRest);

        // 1 doubled to 2, and Healing itself adds another +1 to regeneration.
        Assert.Equal(33, health.Base);
    }

    [Fact]
    public void RestingWhilePoisonedLosesGroundInsteadOfGaining() {
        ActorStat health = Stat(30, 60);
        ActorStat stamina = Stat(0, 40);
        var conditions = new ActorConditions();
        conditions[ActorCondition.Poisoned] = 20;

        UpkeepEngine.ApplyHour(health, stamina, conditions, Locklear, FullRest);

        // +1 from rest, -3 from the poison.
        Assert.Equal(28, health.Base);
    }

    [Fact]
    public void CampRestStopsAtEightyPercentOfThePool() {
        // 80% of (60+40) = 80, and the pool fills Health first.
        ActorStat health = Stat(60, 60);
        ActorStat stamina = Stat(20, 40);

        for (int hour = 0; hour < 10; hour++) {
            UpkeepEngine.ApplyHour(health, stamina, new ActorConditions(), Locklear, CampRest);
        }

        Assert.Equal(60, health.Base);
        Assert.Equal(20, stamina.Base);
    }

    [Fact]
    public void AFullRestGoesAllTheWayUp() {
        ActorStat health = Stat(60, 60);
        ActorStat stamina = Stat(20, 40);

        for (int hour = 0; hour < 30; hour++) {
            UpkeepEngine.ApplyHour(health, stamina, new ActorConditions(), Locklear, FullRest);
        }

        Assert.Equal(60, health.Base);
        Assert.Equal(40, stamina.Base);
    }

    // ---- afflictions over time -------------------------------------------

    [Fact]
    public void DiseasesGetWorseOnTheirOwnAsTimePasses() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Plagued] = 10;
        ActorStat health = Stat(50, 60), stamina = Stat(0, 40);

        UpkeepEngine.ApplyHour(health, stamina, conditions, Locklear, Travelling);

        Assert.Equal(11, conditions[ActorCondition.Plagued]);
    }

    [Fact]
    public void RestingIsItselfATreatmentForBeingSick() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Sick] = 20;
        ActorStat health = Stat(50, 60), stamina = Stat(0, 40);

        UpkeepEngine.ApplyHour(health, stamina, conditions, Locklear, FullRest);

        // -3 for resting, +1 for the illness's own drift.
        Assert.Equal(18, conditions[ActorCondition.Sick]);
    }

    [Fact]
    public void DrinkWearsOffWhetherYouRestOrNot() {
        var walking = new ActorConditions();
        walking[ActorCondition.Drunk] = 30;
        ActorStat health = Stat(50, 60), stamina = Stat(0, 40);

        UpkeepEngine.ApplyHour(health, stamina, walking, Locklear, Travelling);

        Assert.Equal(28, walking[ActorCondition.Drunk]);
    }

    // ---- exhaustion -------------------------------------------------------

    [Fact]
    public void StayingUpIsFreeForSeventeenHours() {
        Assert.Equal(ExhaustionLevel.Rested, UpkeepEngine.ExhaustionAfter(0));
        Assert.Equal(ExhaustionLevel.Rested,
            UpkeepEngine.ExhaustionAfter(UpkeepEngine.ExhaustionWarningTicks - 1));
    }

    [Fact]
    public void AtSeventeenHoursThePartyIsToldTheyAreTired() {
        Assert.Equal(ExhaustionLevel.Tired,
            UpkeepEngine.ExhaustionAfter(UpkeepEngine.ExhaustionWarningTicks));
        Assert.Equal(ExhaustionLevel.Tired,
            UpkeepEngine.ExhaustionAfter(UpkeepEngine.ExhaustionDrainTicks - 1));
    }

    [Fact]
    public void PastEighteenHoursStayingUpStartsCostingHealth() {
        Assert.Equal(ExhaustionLevel.Draining,
            UpkeepEngine.ExhaustionAfter(UpkeepEngine.ExhaustionDrainTicks));
    }

    [Fact]
    public void ExhaustionWearsCharactersDownAtDifferentRates() {
        ActorStat locklearHealth = Stat(50, 60), locklearStamina = Stat(0, 40);
        ActorStat gorathHealth = Stat(50, 60), gorathStamina = Stat(0, 40);

        UpkeepEngine.ApplyExhaustion(locklearHealth, locklearStamina, Locklear);
        UpkeepEngine.ApplyExhaustion(gorathHealth, gorathStamina, Gorath);

        Assert.Equal(48, locklearHealth.Base); // -2
        Assert.Equal(47, gorathHealth.Base);   // -3, Gorath tires fastest
    }

    [Fact]
    public void AnActorWornDownToNothingByExhaustionIsReported() {
        ActorStat health = Stat(1, 60);
        ActorStat stamina = Stat(0, 40);

        bool stillStanding = UpkeepEngine.ApplyExhaustion(health, stamina, Locklear);

        Assert.False(stillStanding);
        Assert.Equal(0, health.Base);
    }

    // ---- the daily passes -------------------------------------------------

    [Fact]
    public void NearDeathLiftsALittleEachDay() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.NearDeath] = 100;

        UpkeepEngine.ApplyDailyNearDeathRecovery(conditions);

        Assert.Equal(99, conditions[ActorCondition.NearDeath]); // (100-100)/10 - 1
    }

    [Fact]
    public void TheFurtherFromDeathTheFasterTheRecovery() {
        var deep = new ActorConditions();
        deep[ActorCondition.NearDeath] = 90;
        var shallow = new ActorConditions();
        shallow[ActorCondition.NearDeath] = 20;

        UpkeepEngine.ApplyDailyNearDeathRecovery(deep);
        UpkeepEngine.ApplyDailyNearDeathRecovery(shallow);

        Assert.Equal(88, deep[ActorCondition.NearDeath]);    // -2
        Assert.Equal(11, shallow[ActorCondition.NearDeath]); // -9
    }

    [Fact]
    public void HealingDoublesTheDailyClimbOutOfNearDeath() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.NearDeath] = 50;
        conditions[ActorCondition.Healing] = 40;

        UpkeepEngine.ApplyDailyNearDeathRecovery(conditions);

        Assert.Equal(38, conditions[ActorCondition.NearDeath]); // -6 doubled
    }

    [Fact]
    public void AHealthyActorHasNothingToRecoverFrom() {
        var conditions = new ActorConditions();

        UpkeepEngine.ApplyDailyNearDeathRecovery(conditions);

        Assert.Equal(0, conditions[ActorCondition.NearDeath]);
    }

    [Fact]
    public void MaximumsCreepUpEveryThirtyDays() {
        Assert.True(UpkeepEngine.IsGrowthDay(0));
        Assert.True(UpkeepEngine.IsGrowthDay(30));
        Assert.True(UpkeepEngine.IsGrowthDay(60));
        Assert.False(UpkeepEngine.IsGrowthDay(29));
        Assert.False(UpkeepEngine.IsGrowthDay(31));
    }

    [Fact]
    public void GrowthRaisesTheCeilingNotTheCurrentValue() {
        ActorStat health = Stat(30, 60);
        ActorStat stamina = Stat(10, 40);

        Assert.True(UpkeepEngine.ApplyPeriodicGrowth(health, stamina));

        Assert.Equal(61, health.Max);
        Assert.Equal(41, stamina.Max);
        Assert.Equal(30, health.Base);
        Assert.Equal(10, stamina.Base);
    }

    [Fact]
    public void GrowthStopsAtTheCeiling() {
        ActorStat health = Stat(250, 250);
        ActorStat stamina = Stat(250, 250);

        Assert.False(UpkeepEngine.ApplyPeriodicGrowth(health, stamina));

        Assert.Equal(250, health.Max);
        Assert.Equal(250, stamina.Max);
    }
}
