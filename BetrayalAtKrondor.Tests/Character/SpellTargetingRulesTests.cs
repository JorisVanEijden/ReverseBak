namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The cursor's aiming rules, and how they line up with the delivery groups the same targeting type
/// selects at the other end of a cast.
/// </summary>
public class SpellTargetingRulesTests {
    [Fact]
    public void EveryChargeOnlyTypeAimsAtGroundRatherThanAtAnybody() {
        // "Delivers nothing" and "is not aimed at anybody" turn out to be the same fact.
        foreach (int type in new[] { 5, 6, 8 }) {
            Assert.Equal(SpellCastTail.Delivery.ChargeOnly, SpellCastTail.DeliveryFor(type));
            Assert.True(SpellTargetingRules.ChargeOnlyTypesAimAtGround(type));
            Assert.NotEqual(SpellTargetingRules.Aim.LivingActor, SpellTargetingRules.AimOf(type));
        }
    }

    [Fact]
    public void TheHealDeliveryIsTheTypeThatDemandsAPartyMember() {
        Assert.Equal(SpellCastTail.Delivery.Type2Routine, SpellCastTail.DeliveryFor(2));
        Assert.True(SpellTargetingRules.PartyOnly(2));
        Assert.True(SpellTargetingRules.PartyOnly(3));
    }

    [Fact]
    public void AndTheDamagingTypesAimAtSomethingStillFighting() {
        foreach (int type in new[] { 0, 1, 4 }) {
            Assert.Equal(SpellCastTail.Delivery.DamageTarget, SpellCastTail.DeliveryFor(type));
            Assert.Equal(SpellTargetingRules.Aim.LivingActor, SpellTargetingRules.AimOf(type));
        }
    }

    [Fact]
    public void FinalRestCanOnlyBePointedAtSomethingAlreadyDown() {
        // The spell that kills outright finishes what is already down — a rule that lives only in
        // the cursor check, not in the spell record.
        Assert.True(SpellTargetingRules.RequiresADownedTarget(7));
        Assert.Equal(SpellTargetingRules.Aim.DownedActor, SpellTargetingRules.AimOf(7));
    }

    [Fact]
    public void AndNoOtherTypeAcceptsADownedTarget() {
        for (var type = 0; type <= 8; type++) {
            if (type == 7) {
                continue;
            }
            Assert.False(SpellTargetingRules.AcceptsIncapacitated(type));
        }
    }

    [Fact]
    public void TheGroundRuleAndTheCrystalRulePartitionTheFloor() {
        // Types 5 and 6 refuse a cell that has a crystal; type 8 requires one.
        Assert.False(SpellTargetingRules.GroundIsTargetable(blocked: false, hasCrystal: true));
        Assert.True(SpellTargetingRules.GroundIsTargetable(blocked: false, hasCrystal: false));
        Assert.Equal(SpellTargetingRules.Aim.Crystal, SpellTargetingRules.AimOf(8));
    }

    [Fact]
    public void ABlockedCellIsNeverClearGround() {
        Assert.False(SpellTargetingRules.GroundIsTargetable(blocked: true, hasCrystal: false));
    }

    [Fact]
    public void OnlyRedAndGreenCrystalsAreTargetable() {
        Assert.True(SpellTargetingRules.CrystalIsTargetable(isRedCrystal: true, isGreenCrystal: false));
        Assert.True(SpellTargetingRules.CrystalIsTargetable(isRedCrystal: false, isGreenCrystal: true));
        Assert.False(SpellTargetingRules.CrystalIsTargetable(isRedCrystal: false, isGreenCrystal: false));
    }

    [Fact]
    public void TheGroupingIsNotInNumericOrder() {
        // 0 and 1/4 share a rule that 2/3 do not, and 7 and 8 each stand alone.
        Assert.Equal(SpellTargetingRules.AimOf(0), SpellTargetingRules.AimOf(4));
        Assert.NotEqual(SpellTargetingRules.AimOf(1), SpellTargetingRules.AimOf(2));
        Assert.NotEqual(SpellTargetingRules.AimOf(6), SpellTargetingRules.AimOf(7));
        Assert.NotEqual(SpellTargetingRules.AimOf(7), SpellTargetingRules.AimOf(8));
    }
}
