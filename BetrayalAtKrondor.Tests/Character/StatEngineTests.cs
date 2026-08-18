namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using Xunit;

/// <summary>
/// The attribute engine (<c>stat_combatant_modify</c> / <c>stat_actor_get</c>). The cases below
/// pin the behaviours the rest of the character systems will lean on: the experience accumulator
/// that makes skill use add up, the clamps, and the health scaling that quietly drags every skill
/// down when an actor is hurt.
/// </summary>
public class StatEngineTests {
    private static ActorStat Stat(byte value, byte max, byte experience = 0, sbyte modifier = 0) =>
        new ActorStat { Base = value, Max = max, Experience = experience, Modifier = modifier };

    // ---- changing a value -------------------------------------------------

    [Fact]
    public void AStatTheActorDoesNotHaveIsInert() {
        ActorStat absent = Stat(0, max: 0);

        StatEngine.StatChange result = StatEngine.Modify(absent, ActorAttribute.Barding, 10 * 256);

        Assert.False(result.Changed);
        Assert.Equal(0, absent.Base);
        Assert.Equal(0, absent.Experience);
    }

    [Fact]
    public void WholePointsArriveInUnitsOf256() {
        ActorStat speed = Stat(10, 100);

        StatEngine.Modify(speed, ActorAttribute.Speed, 3 * 256);

        Assert.Equal(13, speed.Base);
    }

    [Fact]
    public void ChangesTooSmallToMoveTheValueAreBankedAsExperience() {
        ActorStat speed = Stat(10, 100);

        StatEngine.Modify(speed, ActorAttribute.Speed, 128);

        Assert.Equal(10, speed.Base);
        Assert.Equal(128, speed.Experience);
    }

    [Fact]
    public void BankedExperienceEventuallyBuysAWholePoint() {
        ActorStat speed = Stat(10, 100);

        StatEngine.Modify(speed, ActorAttribute.Speed, 128);
        StatEngine.StatChange second = StatEngine.Modify(speed, ActorAttribute.Speed, 128);

        Assert.Equal(11, speed.Base);
        Assert.Equal(0, speed.Experience);
        Assert.True(second.Increased);
    }

    [Fact]
    public void SkillUseWithNoDeltaAdvancesByThePerSkillRate() {
        // Haggling's rate at value 0 is 32 (RatioBase[12]); 256/32 = 8 uses buy the first point.
        ActorStat haggling = Stat(0, 100);

        for (int use = 0; use < 7; use++) {
            StatEngine.Modify(haggling, ActorAttribute.Haggling, 0, StatChangeMode.SkillUse);
        }
        Assert.Equal(0, haggling.Base);
        Assert.Equal(224, haggling.Experience);

        StatEngine.Modify(haggling, ActorAttribute.Haggling, 0, StatChangeMode.SkillUse);
        Assert.Equal(1, haggling.Base);
    }

    [Fact]
    public void SkillUseGetsSlowerAsTheSkillGetsBetter() {
        // The rate slides from RatioBase (value 0) to RatioMax (value 100).
        ActorStat novice = Stat(0, 100);
        ActorStat expert = Stat(100, 100);

        StatEngine.Modify(novice, ActorAttribute.Haggling, 0, StatChangeMode.SkillUse);
        StatEngine.Modify(expert, ActorAttribute.Haggling, 0, StatChangeMode.SkillUse);

        Assert.True(novice.Experience > expert.Experience,
            "a beginner should gain more per use than someone already at 100");
        Assert.Equal(32, novice.Experience); // RatioBase[12]
        Assert.Equal(4, expert.Experience);  // RatioMax[12]
    }

    [Fact]
    public void SpeedCannotSitAtZeroEvenBeforeItIsChanged() {
        // Its stored floor is 1, and the floor is applied on every change — so a Speed of 0 is an
        // impossible state the engine corrects the moment it is touched, not a value to bank from.
        ActorStat speed = Stat(0, 100);

        StatEngine.Modify(speed, ActorAttribute.Speed, 0, StatChangeMode.SkillUse);

        Assert.Equal(1, speed.Base);
    }

    [Fact]
    public void PercentOfCurrentScalesByWhatIsAlreadyThere() {
        ActorStat speed = Stat(50, 100);

        // 50 * (20*256) / 100 = 2560 => 10 whole points.
        StatEngine.Modify(speed, ActorAttribute.Speed, 20 * 256, StatChangeMode.PercentOfCurrent);

        Assert.Equal(60, speed.Base);
    }

    [Fact]
    public void PercentOfRemainingScalesByTheHeadroomLeft() {
        ActorStat speed = Stat(90, 100);

        // (100-90) * 256 = 2560 => 10 whole points.
        StatEngine.Modify(speed, ActorAttribute.Speed, 256, StatChangeMode.PercentOfRemaining);

        Assert.Equal(100, speed.Base);
    }

    [Fact]
    public void AValueIsClampedToItsStoredCeiling() {
        ActorStat haggling = Stat(95, 250);

        StatEngine.Modify(haggling, ActorAttribute.Haggling, 50 * 256);

        Assert.Equal(100, haggling.Base); // StoredMax for the skills is 100
    }

    [Fact]
    public void SpeedAndStrengthNeverFallBelowOne() {
        ActorStat strength = Stat(3, 100);

        StatEngine.Modify(strength, ActorAttribute.Strength, -50 * 256);

        Assert.Equal(1, strength.Base);
    }

    [Fact]
    public void ADecrementBiggerThanTheValuePinsToZeroRatherThanWrapping() {
        ActorStat stealth = Stat(5, 100);

        StatEngine.Modify(stealth, ActorAttribute.Stealth, -80 * 256);

        Assert.Equal(0, stealth.Base);
    }

    [Fact]
    public void RaisingAValuePastItsMaximumRaisesTheMaximumToo() {
        ActorStat scouting = Stat(20, 20);

        StatEngine.Modify(scouting, ActorAttribute.Scouting, 5 * 256);

        Assert.Equal(25, scouting.Base);
        Assert.Equal(25, scouting.Max);
    }

    [Fact]
    public void AnySkillChangeSignalsImprovement_ButOnlyAGainDoesForHealth() {
        ActorStat skill = Stat(50, 100);
        ActorStat health = Stat(50, 100);

        StatEngine.StatChange skillLoss = StatEngine.Modify(skill, ActorAttribute.Stealth, -256);
        StatEngine.StatChange healthLoss = StatEngine.Modify(health, ActorAttribute.Health, -256);
        StatEngine.StatChange healthGain = StatEngine.Modify(health, ActorAttribute.Health, 256);

        Assert.True(skillLoss.SignalsImprovement, "a skill moving at all is worth reporting");
        Assert.False(healthLoss.SignalsImprovement, "losing health is not an improvement");
        Assert.True(healthGain.SignalsImprovement);
    }

    // ---- reading a value --------------------------------------------------

    [Fact]
    public void StoredAndMaximumReadsBypassEverything() {
        ActorStat speed = Stat(40, 90, modifier: 20);

        Assert.Equal(40, StatEngine.Get(speed, ActorAttribute.Speed, speed, StatReadMode.Stored));
        Assert.Equal(90, StatEngine.Get(speed, ActorAttribute.Speed, speed, StatReadMode.Maximum));
    }

    [Fact]
    public void TheEquipmentModifierIsAddedToAReading() {
        ActorStat health = Stat(100, 100);
        ActorStat speed = Stat(40, 100, modifier: 15);

        Assert.Equal(55, StatEngine.Get(speed, ActorAttribute.Speed, health, StatReadMode.Unscaled));
    }

    [Fact]
    public void ANegativeModifierCannotPushAReadingBelowZero() {
        ActorStat health = Stat(100, 100);
        ActorStat stealth = Stat(5, 100, modifier: -60);

        Assert.Equal(0, StatEngine.Get(stealth, ActorAttribute.Stealth, health, StatReadMode.Unscaled));
    }

    [Fact]
    public void AtFullHealthAReadingIsNotDraggedDown() {
        ActorStat health = Stat(100, 100);
        ActorStat speed = Stat(80, 100);

        Assert.Equal(80, StatEngine.Get(speed, ActorAttribute.Speed, health));
    }

    [Fact]
    public void BeingHurtDragsCombatSkillsDownInProportion() {
        ActorStat health = Stat(50, 100); // half dead
        ActorStat speed = Stat(80, 100);

        Assert.Equal(40, StatEngine.Get(speed, ActorAttribute.Speed, health));
        Assert.Equal(80, StatEngine.Get(speed, ActorAttribute.Speed, health, StatReadMode.Unscaled));
    }

    [Fact]
    public void CraftSkillsAreOnlyHalfAsSensitiveToInjury() {
        ActorStat health = Stat(50, 100);
        ActorStat armorCraft = Stat(80, 100);

        // Half-weighted: 80 -> 60, where a combat skill would have gone to 40.
        Assert.Equal(60, StatEngine.Get(armorCraft, ActorAttribute.ArmorCraft, health));
    }

    [Fact]
    public void AStatTheActorDoesNotHaveReadsAsZero() {
        ActorStat health = Stat(100, 100);
        ActorStat absent = Stat(0, max: 0);

        Assert.Equal(0, StatEngine.Get(absent, ActorAttribute.Barding, health));
    }

    // ---- the health/stamina pool -----------------------------------------

    [Fact]
    public void DamageDrainsStaminaBeforeHealth() {
        ActorStat health = Stat(60, 60);
        ActorStat stamina = Stat(40, 40);

        StatEngine.ModifyHealthPool(health, stamina, -20 * 256, healTargetPercent: 100, out bool collapsed);

        Assert.Equal(60, health.Base);
        Assert.Equal(20, stamina.Base);
        Assert.False(collapsed);
    }

    [Fact]
    public void OnceStaminaIsGoneDamageEatsIntoHealth() {
        ActorStat health = Stat(60, 60);
        ActorStat stamina = Stat(40, 40);

        StatEngine.ModifyHealthPool(health, stamina, -70 * 256, healTargetPercent: 100, out _);

        Assert.Equal(30, health.Base);
        Assert.Equal(0, stamina.Base);
    }

    [Fact]
    public void DrainingThePoolToNothingReportsACollapse() {
        ActorStat health = Stat(10, 60);
        ActorStat stamina = Stat(0, 40);

        int remaining = StatEngine.ModifyHealthPool(health, stamina, -30 * 256,
            healTargetPercent: 100, out bool collapsed);

        Assert.Equal(0, remaining);
        Assert.Equal(0, health.Base);
        Assert.True(collapsed, "the caller needs this to apply Near-death");
    }

    [Fact]
    public void HealingStopsAtTheRequestedPercentageOfTheCombinedMaximum() {
        ActorStat health = Stat(10, 60);
        ActorStat stamina = Stat(0, 40);

        // 50% of (60+40) = 50.
        StatEngine.ModifyHealthPool(health, stamina, 90 * 256, healTargetPercent: 50, out _);

        Assert.Equal(50, health.Base);
        Assert.Equal(0, stamina.Base);
    }

    [Fact]
    public void HealingDoesNothingWhenThePoolIsAlreadyAboveTheTarget() {
        ActorStat health = Stat(60, 60);
        ActorStat stamina = Stat(30, 40);

        StatEngine.ModifyHealthPool(health, stamina, 50 * 256, healTargetPercent: 50, out _);

        Assert.Equal(60, health.Base);
        Assert.Equal(30, stamina.Base);
    }

    [Fact]
    public void NearDeathCapsHealingToASliver() {
        ActorStat health = Stat(1, 60);
        ActorStat stamina = Stat(0, 40);

        // rank 50 => ((100-50)*30)/100 + 1 = 16, regardless of the requested percentage.
        StatEngine.ModifyHealthPool(health, stamina, 90 * 256, healTargetPercent: 100,
            out _, nearDeathRank: 50);

        Assert.Equal(16, health.Base);
    }

    // ---- the combined pool ----------------------------------------------------------------------

    [Fact]
    public void ThePoolIsBothPairsTogether() {
        var health = new ActorStat { Base = 12, Max = 20 };
        var stamina = new ActorStat { Base = 5, Max = 15 };

        // HealthStaminaCombo has no stored slot: it is this sum, which is what ModifyHealthPool
        // writes back into. Reading it as health alone under-counts every wounded member.
        Assert.Equal(17, StatEngine.HealthPool(health, stamina));
        Assert.Equal(35, StatEngine.HealthPoolMaximum(health, stamina));
        Assert.Equal(18, StatEngine.HealthPoolDeficit(health, stamina));
    }

    [Fact]
    public void APoolOverItsMaximumIsNotADebt() {
        var health = new ActorStat { Base = 30, Max = 20 };
        var stamina = new ActorStat { Base = 20, Max = 15 };

        // A temple bills one royal per missing point, so a negative deficit would PAY the party.
        Assert.Equal(0, StatEngine.HealthPoolDeficit(health, stamina));
    }
}
