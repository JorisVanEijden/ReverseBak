namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The overworld cast dispatcher: nine spells, three of them instantaneous, and everything else
/// silently ignored.
/// </summary>
public class FieldSpellsTests {
    [Fact]
    public void NineSpellsWorkOutsideAFight() {
        Assert.Equal(9, FieldSpells.All.Length);
        foreach (int id in FieldSpells.All) {
            Assert.True(FieldSpells.IsFieldSpell(id));
        }
    }

    [Fact]
    public void NonMartialDoesNotMeanFieldCastable() {
        // Dannon's Delusions and Nightfingers are non-martial combat spells; Eagle Wing, Aether
        // Bridge and Dawn of Truth are non-martial and not dispatched here at all.
        Assert.False(FieldSpells.NonMartialImpliesFieldCastable);
        Assert.False(FieldSpells.IsFieldSpell(SpellIds.DannonsDelusions));
        Assert.False(FieldSpells.IsFieldSpell(SpellIds.Nightfingers));
        Assert.False(FieldSpells.IsFieldSpell(10));
        Assert.False(FieldSpells.IsFieldSpell(24));
        Assert.False(FieldSpells.IsFieldSpell(33));
    }

    [Fact]
    public void AndNoCombatSpellIsInTheList() {
        Assert.False(FieldSpells.IsFieldSpell(SpellIds.Skyfire));
        Assert.False(FieldSpells.IsFieldSpell(SpellIds.MadGodsRage));
        Assert.False(FieldSpells.IsFieldSpell(SpellIds.FinalRest));
    }

    [Fact]
    public void ThreeOfTheNineTakeNoDurationAtAll() {
        Assert.True(FieldSpells.IsInstantaneous(FieldSpells.EyesOfIshap));
        Assert.True(FieldSpells.IsInstantaneous(FieldSpells.TheUnseen));
        Assert.True(FieldSpells.IsInstantaneous(FieldSpells.NacreCicatrix));
    }

    [Fact]
    public void AndTheOtherSixDo() {
        foreach (int id in FieldSpells.All) {
            if (FieldSpells.IsInstantaneous(id)) {
                continue;
            }
            Assert.True(FieldSpells.TakesDuration(id));
        }
    }

    [Fact]
    public void ASpellOutsideTheListTakesNoDurationBecauseItDoesNothing() {
        Assert.False(FieldSpells.TakesDuration(SpellIds.Skyfire));
    }

    [Fact]
    public void StarduskIsAFieldSpellDespiteItsCastingRestrictions() {
        // SpellIds already singles it out for where it may be cast; that is a different question
        // from whether the overworld dispatcher handles it.
        Assert.Equal(SpellIds.Stardusk, FieldSpells.Stardusk);
        Assert.True(FieldSpells.IsFieldSpell(FieldSpells.Stardusk));
    }

    [Fact]
    public void CandleGlowAgreesWithTheIdAlreadyRecorded() {
        Assert.Equal(SpellIds.CandleGlow, FieldSpells.CandleGlow);
    }

    [Fact]
    public void AnUnrecognisedSpellIsSilentlyIgnored() {
        Assert.True(FieldSpells.UnknownSpellDoesNothing);
        Assert.False(FieldSpells.IsFieldSpell(-1));
        Assert.False(FieldSpells.IsFieldSpell(44));
    }
    [Fact]
    public void ThePowerDoesNotAlwaysBuyTime() {
        // Dragon's Breath scales with the power; Scent of Sarig ignores it entirely.
        Assert.True(FieldSpells.PowerExtendsDuration(FieldSpells.DragonsBreath));
        Assert.False(FieldSpells.PowerExtendsDuration(FieldSpells.ScentOfSarig));
    }

    [Fact]
    public void SoTheSameInputsGiveVeryDifferentLifetimes() {
        Assert.Equal(10 * 20 * 30, FieldSpells.DurationTicks(10, 20, powerExtendsIt: true));
        Assert.Equal(10 * 30, FieldSpells.DurationTicks(10, 20, powerExtendsIt: false));
    }

    [Fact]
    public void TwoOfThemDriveTheWorldLighting() {
        // Same machinery to opposite ends: one darkens, one lightens.
        Assert.True(FieldSpells.DrivesWorldLighting(FieldSpells.DragonsBreath));
        Assert.True(FieldSpells.DrivesWorldLighting(FieldSpells.CandleGlow));
        Assert.False(FieldSpells.DrivesWorldLighting(FieldSpells.ScentOfSarig));
    }

    [Fact]
    public void CandleGlowAboveGroundIsACompleteNoOp() {
        // It returns before the sound, the text, the timers and the cost — silent and free.
        Assert.True(FieldSpells.RequiresUnderground(FieldSpells.CandleGlow));
        Assert.False(FieldSpells.ChargesEvenWithNoEffect(FieldSpells.CandleGlow));
    }

    [Fact]
    public void ButEveryOtherTimedSpellChargesEvenWithNoEffect() {
        // The cost sits outside the branch that sets the timers.
        Assert.True(FieldSpells.ChargesEvenWithNoEffect(FieldSpells.DragonsBreath));
        Assert.True(FieldSpells.ChargesEvenWithNoEffect(FieldSpells.ScentOfSarig));
        Assert.Equal(0, FieldSpells.DurationTicks(0, 20, powerExtendsIt: true));
    }

    [Fact]
    public void EyesOfIshapIsTenPercentPerPointOfPower() {
        Assert.True(FieldSpells.LocatorSucceeds(rollUnder100: 30, cost: 3));
        Assert.False(FieldSpells.LocatorSucceeds(rollUnder100: 31, cost: 3));
    }

    [Fact]
    public void AndTheThresholdItselfSucceeds() {
        // Inclusive comparison, so a roll equal to cost * 10 lands.
        Assert.True(FieldSpells.LocatorSucceeds(rollUnder100: 50, cost: 5));
    }

    [Fact]
    public void AFailedLocatorStillCostsFullPrice() {
        // The cost is applied before the roll is taken.
        Assert.True(FieldSpells.IsLocatorRoll(FieldSpells.EyesOfIshap));
        Assert.True(FieldSpells.ChargesEvenWithNoEffect(FieldSpells.EyesOfIshap));
    }
    [Fact]
    public void AllThreeNoDurationSpellsAreLocators() {
        foreach (int id in new[] { FieldSpells.EyesOfIshap, FieldSpells.TheUnseen,
                     FieldSpells.NacreCicatrix }) {
            Assert.True(FieldSpells.IsLocatorRoll(id));
        }
        Assert.False(FieldSpells.IsLocatorRoll(FieldSpells.DragonsBreath));
    }

    [Fact]
    public void NacreCicatrixSubtractsFourBeforeScaling() {
        Assert.Equal(4, FieldSpells.LocatorCostOffset(FieldSpells.NacreCicatrix));
        Assert.Equal(0, FieldSpells.LocatorCostOffset(FieldSpells.EyesOfIshap));
    }

    [Fact]
    public void AndTheOffsetIsDeliberateBecauseAllThreeReachExactlyOneHundred() {
        // Eyes of Ishap and The Unseen cost 1..10; Nacre Cicatrix costs 5..14.
        Assert.Equal(100, FieldSpells.LocatorChancePercent(FieldSpells.EyesOfIshap, cost: 10));
        Assert.Equal(100, FieldSpells.LocatorChancePercent(FieldSpells.TheUnseen, cost: 10));
        Assert.Equal(100, FieldSpells.LocatorChancePercent(FieldSpells.NacreCicatrix, cost: 14));
    }

    [Fact]
    public void AtTheirMinimumCostsTheOddsAreDeliberatelyPoor() {
        Assert.Equal(10, FieldSpells.LocatorChancePercent(FieldSpells.EyesOfIshap, cost: 1));
        Assert.Equal(10, FieldSpells.LocatorChancePercent(FieldSpells.NacreCicatrix, cost: 5));
    }

    [Fact]
    public void TheLocatorRollIsInclusive() {
        Assert.True(FieldSpells.LocatorSucceeds(FieldSpells.NacreCicatrix, rollUnder100: 10,
            cost: 5));
        Assert.False(FieldSpells.LocatorSucceeds(FieldSpells.NacreCicatrix, rollUnder100: 11,
            cost: 5));
    }

    [Fact]
    public void AndAFailureCostsFullPrice() {
        Assert.True(FieldSpells.LocatorChargesBeforeRolling);
    }

    [Fact]
    public void UnionIgnoresThePowerJustLikeScentOfSarig() {
        Assert.False(FieldSpells.PowerExtendsDuration(FieldSpells.Union));
        Assert.False(FieldSpells.DrivesWorldLighting(FieldSpells.Union));
    }
}
