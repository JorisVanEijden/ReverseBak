namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// What a cast does after the arithmetic. Several spells throw their own magnitude away here, three
/// targeting types never deal damage at all, and ending early costs nothing.
/// </summary>
public class SpellCastTailTests {
    [Fact]
    public void SkyfireAgainstAnUnarmouredTargetIsNotACastAtAll() {
        // Not merely zero damage — the continue flag is cleared, so nothing downstream runs.
        Assert.True(SpellCastTail.SkyfireEndsTheCast(SpellIds.Skyfire, targetUsesMetal: false));
        Assert.False(SpellCastTail.SkyfireEndsTheCast(SpellIds.Skyfire, targetUsesMetal: true));
    }

    [Fact]
    public void AndNoOtherSpellCaresAboutMetal() {
        Assert.False(SpellCastTail.SkyfireEndsTheCast(SpellIds.Flamecast, targetUsesMetal: false));
    }

    [Fact]
    public void TouchOfLimsKragmaStopsWhenTheCasterIsAlreadyAdjacent() {
        // Reads backwards for a "touch" spell, but the branch is unambiguous: below 2 cells it
        // clears the continue flag, at 2 or more it walks the caster in and carries on.
        Assert.True(SpellCastTail.LimsKragmaEndsTheCast(0));
        Assert.True(SpellCastTail.LimsKragmaEndsTheCast(1));
        Assert.False(SpellCastTail.LimsKragmaEndsTheCast(2));
        Assert.False(SpellCastTail.LimsKragmaEndsTheCast(9));
    }

    [Fact]
    public void TwoSpellsAreEntirelyTheirOwnHandler() {
        Assert.True(SpellCastTail.HandlerEndsTheCast(SpellIds.WindsOfEortis));
        Assert.True(SpellCastTail.HandlerEndsTheCast(SpellIds.MadGodsRage));
        Assert.False(SpellCastTail.HandlerEndsTheCast(SpellIds.Firestorm));
    }

    [Fact]
    public void DannonsDelusionsAndFirestormDiscardTheirOwnDamage() {
        // Both carry a CostTimesDamage calculation, so a port that stops at the arithmetic gives
        // them damage the original zeroes before it can ever be applied.
        Assert.True(SpellCastTail.ZeroesItsOwnMagnitude(SpellIds.DannonsDelusions));
        Assert.True(SpellCastTail.ZeroesItsOwnMagnitude(SpellIds.Firestorm));
        Assert.False(SpellCastTail.ZeroesItsOwnMagnitude(SpellIds.Flamecast));
    }

    [Fact]
    public void SixSpellsHaveAPostAnimationHookAndTheRestHaveNone() {
        Assert.Equal(SpellCastTail.PostAnimationHook.MagnitudeFromGlobal,
            SpellCastTail.HookFor(SpellIds.UnfortunateFlux));
        Assert.Equal(SpellCastTail.PostAnimationHook.DelegateToFlamecast,
            SpellCastTail.HookFor(SpellIds.Flamecast));
        Assert.Equal(SpellCastTail.PostAnimationHook.KillOutright,
            SpellCastTail.HookFor(SpellIds.FinalRest));
        Assert.Equal(SpellCastTail.PostAnimationHook.RegisterGriefOfAThousandNights,
            SpellCastTail.HookFor(SpellIds.FettersOfRime));
        Assert.Equal(SpellCastTail.PostAnimationHook.None, SpellCastTail.HookFor(SpellIds.Skyfire));
        Assert.Equal(SpellCastTail.PostAnimationHook.None, SpellCastTail.HookFor(SpellIds.Stardusk));
    }

    [Fact]
    public void FinalRestKillsRatherThanDamages() {
        // Its record carries no damage at all; the kill is in the hook, not in the arithmetic.
        Assert.Equal(SpellCastTail.PostAnimationHook.KillOutright,
            SpellCastTail.HookFor(SpellIds.FinalRest));
        Assert.NotEqual(SpellCastTail.PostAnimationHook.ZeroTheMagnitude,
            SpellCastTail.HookFor(SpellIds.FinalRest));
    }

    [Fact]
    public void ThreeTargetingTypesPayForACastThatDeliversNothing() {
        Assert.Equal(SpellCastTail.Delivery.ChargeOnly, SpellCastTail.DeliveryFor(5));
        Assert.Equal(SpellCastTail.Delivery.ChargeOnly, SpellCastTail.DeliveryFor(6));
        Assert.Equal(SpellCastTail.Delivery.ChargeOnly, SpellCastTail.DeliveryFor(8));
    }

    [Fact]
    public void TypeTwoGoesSomewhereElseEntirely() {
        Assert.Equal(SpellCastTail.Delivery.Type2Routine, SpellCastTail.DeliveryFor(2));
    }

    [Fact]
    public void EverythingElseDamages() {
        foreach (int type in new[] { 0, 1, 3, 4, 7 }) {
            Assert.Equal(SpellCastTail.Delivery.DamageTarget, SpellCastTail.DeliveryFor(type));
        }
    }

    [Fact]
    public void TheDamagePathHasThreeIndependentWaysToDealNothing() {
        Assert.True(SpellCastTail.DealsDamage(animationReported: true, 12, targetResists: false));
        Assert.False(SpellCastTail.DealsDamage(animationReported: false, 12, targetResists: false));
        Assert.False(SpellCastTail.DealsDamage(animationReported: true, 0, targetResists: false));
        Assert.False(SpellCastTail.DealsDamage(animationReported: true, 12, targetResists: true));
    }

    [Fact]
    public void TheWeaknessDoublingIsUndoneExactly() {
        // Doubling always leaves an even number, so halving recovers the pre-doubled cost with no
        // rounding — the multiplier reaches the magnitude and nothing else.
        int surcharged = SpellCostModifiers.Effective(41, surcharged: true, targetIsWeak: false);
        int doubled = SpellCostModifiers.Effective(41, surcharged: true, targetIsWeak: true);
        Assert.Equal(surcharged, SpellCastTail.UndoWeakness(doubled));
    }

    [Fact]
    public void TheBillIsTheCostThePlayerChoseNotTheCostTheSpellUsed() {
        // 40 chosen, 60 after the surcharge: the spell scales from 60 but the caster pays 40.
        Assert.Equal(40, SpellCastTail.AmountBilled(originalCost: 40, runningCost: 60, targetingType: 0));
    }

    [Fact]
    public void ExceptForTypeTwoWhichBillsTheSurcharge() {
        Assert.Equal(60, SpellCastTail.AmountBilled(originalCost: 40, runningCost: 60, targetingType: 2));
    }

    [Fact]
    public void ANegativeCostIsCastForFree() {
        Assert.False(SpellCastTail.CasterPays(costWasNegated: true, targetingType: 0));
        Assert.True(SpellCastTail.CasterPays(costWasNegated: false, targetingType: 0));
    }

    [Fact]
    public void ButTypeTwoChargesAnyway() {
        // It never consults the negated flag, so the one delivery that bills the surcharge is also
        // the one that ignores the free-cast exemption.
        Assert.True(SpellCastTail.CasterPays(costWasNegated: true, targetingType: 2));
    }

    [Fact]
    public void TheCrystalStaffDrainIsAChapterEightRule() {
        Assert.True(SpellCastTail.DrainsCrystalStaff(8, casterHasEquippedCrystalStaff: true));
        Assert.False(SpellCastTail.DrainsCrystalStaff(7, casterHasEquippedCrystalStaff: true));
        Assert.False(SpellCastTail.DrainsCrystalStaff(8, casterHasEquippedCrystalStaff: false));
    }

    [Fact]
    public void AndItFloorsRatherThanWrapping() {
        // The variable byte is unsigned; subtracting past zero would wrap it to a near-full staff.
        Assert.Equal(0, SpellCastTail.DrainStaff(charge: 5, cost: 20));
        Assert.Equal(10, SpellCastTail.DrainStaff(charge: 30, cost: 20));
    }

    [Fact]
    public void ThoughtsLikeCloudsBlocksTheTypeTwoDelivery() {
        Assert.True(SpellCastTail.Type2IsBlocked(casterHasThoughtsLikeClouds: true));
        Assert.False(SpellCastTail.Type2IsBlocked(casterHasThoughtsLikeClouds: false));
    }
}
