namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using Xunit;

/// <summary>
/// Afflictions: how a rank changes, what it costs the actor at read time, and the Near-death
/// collapse. Ported from <c>stat_combatant_apply_delta</c> and the <c>g_aConditionInfo</c> table.
/// </summary>
public class ConditionEngineTests {
    private static ActorStat Stat(byte value, byte max) => new ActorStat { Base = value, Max = max };

    // ---- ranks ------------------------------------------------------------

    [Fact]
    public void ARankIsAnIntensityThatClampsToNought_AndToAHundred() {
        var conditions = new ActorConditions();

        ConditionEngine.Apply(conditions, ActorCondition.Poisoned, 150);
        Assert.Equal(100, conditions[ActorCondition.Poisoned]);

        ConditionEngine.Apply(conditions, ActorCondition.Poisoned, -400);
        Assert.Equal(0, conditions[ActorCondition.Poisoned]);
    }

    [Fact]
    public void PickingUpAnAfflictionAndShakingItOffAreBothReported() {
        var conditions = new ActorConditions();

        ConditionEngine.ConditionChange caught =
            ConditionEngine.Apply(conditions, ActorCondition.Sick, 30);
        Assert.True(caught.Appeared);
        Assert.False(caught.Cleared);
        Assert.True(caught.RaisesEvent);

        ConditionEngine.ConditionChange worse =
            ConditionEngine.Apply(conditions, ActorCondition.Sick, 10);
        Assert.False(worse.Appeared);
        Assert.False(worse.RaisesEvent);

        ConditionEngine.ConditionChange gone =
            ConditionEngine.Apply(conditions, ActorCondition.Sick, -40);
        Assert.True(gone.Cleared);
        Assert.True(gone.RaisesEvent);
    }

    [Fact]
    public void ANoOpChangeDoesNothingAtAll() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Sick] = 20;

        ConditionEngine.ConditionChange result =
            ConditionEngine.Apply(conditions, ActorCondition.Sick, 0);

        Assert.Equal(20, result.Rank);
        Assert.False(result.Appeared);
        Assert.False(result.RaisesEvent);
    }

    [Theory]
    [InlineData(ActorCondition.Drunk)]
    [InlineData(ActorCondition.Healing)]
    public void DrunkAndHealingAreNeverAnnounced(ActorCondition condition) {
        var conditions = new ActorConditions();

        ConditionEngine.ConditionChange result = ConditionEngine.Apply(conditions, condition, 40);

        Assert.Equal(40, result.Rank);
        Assert.True(result.Appeared);
        Assert.False(result.RaisesEvent);
    }

    [Fact]
    public void NearDeathIsAnnouncedOutOfCombatButNotDuringIt() {
        var inField = new ActorConditions();
        var inFight = new ActorConditions();

        Assert.True(ConditionEngine.Apply(inField, ActorCondition.NearDeath, 50).RaisesEvent);
        Assert.False(ConditionEngine.Apply(inFight, ActorCondition.NearDeath, 50, inCombat: true)
            .RaisesEvent);
    }

    // ---- the near-death collapse -----------------------------------------

    [Fact]
    public void CollapsingIntoNearDeathWipesEveryOtherAffliction() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Sick] = 40;
        conditions[ActorCondition.Poisoned] = 80;
        conditions[ActorCondition.Drunk] = 20;

        ConditionEngine.ConditionChange result =
            ConditionEngine.Apply(conditions, ActorCondition.NearDeath, 60);

        Assert.True(result.Collapsed);
        Assert.Equal(60, conditions[ActorCondition.NearDeath]);
        Assert.Equal(0, conditions[ActorCondition.Sick]);
        Assert.Equal(0, conditions[ActorCondition.Poisoned]);
        Assert.Equal(0, conditions[ActorCondition.Drunk]);
    }

    [Fact]
    public void CollapsingLeavesTheActorAliveOnASliverOfHealth() {
        var conditions = new ActorConditions();
        ActorStat health = Stat(40, 60);
        ActorStat stamina = Stat(30, 40);

        ConditionEngine.Apply(conditions, ActorCondition.NearDeath, 50, health, stamina);

        // Refilled against the near-death cap: ((100-50)*30)/100 + 1 = 16.
        Assert.Equal(16, health.Base);
        Assert.Equal(0, stamina.Base);
    }

    [Fact]
    public void TheWorseTheNearDeathRankTheLessTheActorComesBackWith() {
        var mild = new ActorConditions();
        var severe = new ActorConditions();
        ActorStat mildHealth = Stat(0, 60), mildStamina = Stat(0, 40);
        ActorStat severeHealth = Stat(0, 60), severeStamina = Stat(0, 40);

        ConditionEngine.Apply(mild, ActorCondition.NearDeath, 10, mildHealth, mildStamina);
        ConditionEngine.Apply(severe, ActorCondition.NearDeath, 90, severeHealth, severeStamina);

        Assert.True(mildHealth.Base > severeHealth.Base);
        Assert.Equal(28, mildHealth.Base);   // ((100-10)*30)/100 + 1
        Assert.Equal(4, severeHealth.Base);  // ((100-90)*30)/100 + 1
    }

    [Fact]
    public void RecoveringFromNearDeathDoesNotWipeAnything() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.NearDeath] = 50;
        conditions[ActorCondition.Sick] = 30;

        ConditionEngine.ConditionChange result =
            ConditionEngine.Apply(conditions, ActorCondition.NearDeath, -20);

        Assert.False(result.Collapsed);
        Assert.Equal(30, conditions[ActorCondition.Sick]);
        Assert.Equal(30, conditions[ActorCondition.NearDeath]);
    }

    // ---- what afflictions cost you ---------------------------------------

    [Theory]
    [InlineData(ActorAttribute.Stamina)]
    [InlineData(ActorAttribute.Defense)]
    [InlineData(ActorAttribute.AccuracyMelee)]
    [InlineData(ActorAttribute.AccuracyCasting)]
    [InlineData(ActorAttribute.Stealth)]
    [InlineData(ActorAttribute.Haggling)]
    public void BeingBlindDrunkCutsCombatAndCraftAttributesToFortyPercent(ActorAttribute attribute) {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Drunk] = 100;

        Assert.Equal(40, ConditionEngine.ApplyAttributePenalties(100, attribute, conditions));
    }

    [Theory]
    [InlineData(ActorAttribute.Health)]
    [InlineData(ActorAttribute.Speed)]
    [InlineData(ActorAttribute.Strength)]
    public void DrinkDoesNotTouchHealthSpeedOrStrength(ActorAttribute attribute) {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Drunk] = 100;

        Assert.Equal(100, ConditionEngine.ApplyAttributePenalties(100, attribute, conditions));
    }

    [Fact]
    public void TheDrinkPenaltyScalesWithHowDrunkTheActorIs() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Drunk] = 50;

        // (-60 * 50 / 100) + 100 = 70%.
        Assert.Equal(70, ConditionEngine.ApplyAttributePenalties(100, ActorAttribute.Stealth, conditions));
    }

    [Fact]
    public void TheOtherAfflictionsCostNothingAtReadTime() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Sick] = 100;
        conditions[ActorCondition.Plagued] = 100;
        conditions[ActorCondition.Poisoned] = 100;
        conditions[ActorCondition.Starving] = 100;

        // They bite through the hourly upkeep instead — see HourlyDelta / RegenBonus.
        Assert.Equal(100, ConditionEngine.ApplyAttributePenalties(100, ActorAttribute.Stealth, conditions));
    }

    // ---- drift over time --------------------------------------------------

    [Fact]
    public void DiseasesWorsenOnTheirOwnAndDrinkWearsOff() {
        var conditions = new ActorConditions();

        Assert.Equal(1, ConditionEngine.HourlyDelta(ActorCondition.Sick, conditions));
        Assert.Equal(1, ConditionEngine.HourlyDelta(ActorCondition.Plagued, conditions));
        Assert.Equal(1, ConditionEngine.HourlyDelta(ActorCondition.Poisoned, conditions));
        Assert.Equal(-2, ConditionEngine.HourlyDelta(ActorCondition.Drunk, conditions));
        Assert.Equal(-3, ConditionEngine.HourlyDelta(ActorCondition.Healing, conditions));
        Assert.Equal(0, ConditionEngine.HourlyDelta(ActorCondition.Starving, conditions));
    }

    [Fact]
    public void BeingUnderHealingTurnsTheDiseasesAround() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Healing] = 50;

        Assert.Equal(-2, ConditionEngine.HourlyDelta(ActorCondition.Sick, conditions));
        Assert.Equal(-1, ConditionEngine.HourlyDelta(ActorCondition.Plagued, conditions));
        Assert.Equal(-1, ConditionEngine.HourlyDelta(ActorCondition.Poisoned, conditions));
        Assert.Equal(-4, ConditionEngine.HourlyDelta(ActorCondition.Drunk, conditions));
    }

    [Fact]
    public void HealingDoesNotAccelerateStarvationOrItself() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Healing] = 50;

        Assert.Equal(0, ConditionEngine.HourlyDelta(ActorCondition.Starving, conditions));
        Assert.Equal(-3, ConditionEngine.HourlyDelta(ActorCondition.Healing, conditions));
    }

    [Fact]
    public void AfflictionsPullHourlyRegenerationDown_AndHealingPushesItUp() {
        var poisoned = new ActorConditions();
        poisoned[ActorCondition.Poisoned] = 10;

        var mending = new ActorConditions();
        mending[ActorCondition.Healing] = 10;

        var both = new ActorConditions();
        both[ActorCondition.Poisoned] = 10;
        both[ActorCondition.Healing] = 10;

        Assert.Equal(-3, ConditionEngine.RegenBonus(poisoned));
        Assert.Equal(1, ConditionEngine.RegenBonus(mending));
        Assert.Equal(-2, ConditionEngine.RegenBonus(both));
        Assert.Equal(0, ConditionEngine.RegenBonus(new ActorConditions()));
    }

    // ---- composing with the attribute engine ------------------------------

    [Fact]
    public void ConditionsPlugIntoAnAttributeReadAsItsEffectsHook() {
        var conditions = new ActorConditions();
        conditions[ActorCondition.Drunk] = 100;
        ActorStat health = Stat(100, 100);
        ActorStat stealth = Stat(80, 100);

        int sober = StatEngine.Get(stealth, ActorAttribute.Stealth, health);
        int drunk = StatEngine.Get(stealth, ActorAttribute.Stealth, health,
            applyPartyEffects: v => ConditionEngine.ApplyAttributePenalties(v, ActorAttribute.Stealth, conditions));

        Assert.Equal(80, sober);
        Assert.Equal(32, drunk); // 80 -> 40%
    }
}
