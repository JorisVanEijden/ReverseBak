namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Spells;
using System;
using Xunit;

/// <summary>
/// Spell_CalcEffectMagnitude — the function that gives <see cref="SpellCalculation"/> its meaning.
/// The two rules keyed to a spell <i>number</i> rather than to any data field are the ones a port
/// loses silently, so both are pinned here.
/// </summary>
public class SpellEffectMagnitudeTests {
    private static Spell SpellWith(SpellCalculation calculation, int damage) =>
        new Spell("t") { Calculation = calculation, Damage = damage };

    [Fact]
    public void ANonCostRelatedSpellScalesWithNothing() {
        Spell spell = SpellWith(SpellCalculation.NonCostRelated, 40);

        Assert.Equal(0, SpellEffectMagnitude.Calculate(spell, 3, 20));
    }

    [Fact]
    public void AFixedAmountSpellIgnoresWhatTheCasterPaid() {
        Spell spell = SpellWith(SpellCalculation.FixedAmount, 1000); // Touch of Lims-Kragma

        Assert.Equal(1000, SpellEffectMagnitude.Calculate(spell, 15, 1));
        Assert.Equal(1000, SpellEffectMagnitude.Calculate(spell, 15, 20));
    }

    [Fact]
    public void SkyfireDoesNothingAtAllToATargetCarryingNoMetal() {
        // The shipped record deals 40. Against an unarmoured target that becomes zero — a real
        // tactical rule, and the one branch in this function that reads the target at all.
        Spell skyfire = SpellWith(SpellCalculation.FixedAmount, 40);

        Assert.Equal(0, SpellEffectMagnitude.Calculate(skyfire, SpellIds.Skyfire, 12,
            targetHasMetalGear: false));
        Assert.Equal(40, SpellEffectMagnitude.Calculate(skyfire, SpellIds.Skyfire, 12,
            targetHasMetalGear: true));
    }

    [Fact]
    public void TheMetalRuleAppliesToSkyfireAlone() {
        Spell other = SpellWith(SpellCalculation.FixedAmount, 40);

        Assert.Equal(40, SpellEffectMagnitude.Calculate(other, 15, 12, targetHasMetalGear: false));
    }

    [Theory]
    [InlineData(1, 7, 7)]     // Dannon's Delusions
    [InlineData(3, 5, 15)]    // Flamecast
    [InlineData(5, 4, 20)]    // Bane of Black Slayers
    public void APositiveEffectMultipliesTheInvestedCost(int damage, int cost, int expected) {
        Spell spell = SpellWith(SpellCalculation.CostTimesDamage, damage);

        Assert.Equal(expected, SpellEffectMagnitude.Calculate(spell, 9, cost));
    }

    [Fact]
    public void ANegativeEffectDividesInsteadOfMultiplying() {
        // Unreachable with shipped data — all eight CostTimesDamage records are positive — but it is
        // what the original does, and a mod could supply it.
        Spell spell = SpellWith(SpellCalculation.CostTimesDamage, -4);

        Assert.Equal(5, SpellEffectMagnitude.Calculate(spell, 9, 20));
    }

    [Fact]
    public void TheMostNegativeEffectIsClampedRatherThanNegated() {
        // Negating short.MinValue does not fit, so the original substitutes short.MaxValue.
        Spell spell = SpellWith(SpellCalculation.CostTimesDamage, short.MinValue);

        Assert.Equal(0, SpellEffectMagnitude.Calculate(spell, 9, 20));
    }

    [Fact]
    public void AZeroEffectDividesByZeroRatherThanInventingAnAnswer() {
        Spell spell = SpellWith(SpellCalculation.CostTimesDamage, 0);

        Assert.Throws<DivideByZeroException>(() => SpellEffectMagnitude.Calculate(spell, 9, 20));
    }

    [Fact]
    public void CombatGridElementMultipliesLikeAPositiveCostTimesDamage() {
        Spell wrathOfKillian = SpellWith(SpellCalculation.CombatGridElement, 1);

        Assert.Equal(14, SpellEffectMagnitude.Calculate(wrathOfKillian, 19, 14));
    }

    [Fact]
    public void CostTimesDurationContributesNothingHere() {
        // Its magnitude is duration-based and computed by the dispatcher; this function's answer for
        // it is zero, which is why the name reads as a contradiction.
        Spell steelfire = SpellWith(SpellCalculation.CostTimesDuration, 0);

        Assert.Equal(0, SpellEffectMagnitude.Calculate(steelfire, 25, 10));
    }

    [Fact]
    public void MadGodsRageOverridesTheCatalogueEntirely() {
        // The shipped record says FixedAmount 100. The code substitutes 1000 after the switch, so
        // the data value never reaches a target.
        Spell asShipped = SpellWith(SpellCalculation.FixedAmount, 100);

        Assert.Equal(SpellEffectMagnitude.MadGodsRageMagnitude,
            SpellEffectMagnitude.Calculate(asShipped, SpellIds.MadGodsRage, 20));
        Assert.NotEqual(100, SpellEffectMagnitude.Calculate(asShipped, SpellIds.MadGodsRage, 20));
    }

    [Fact]
    public void MadGodsRageOverridesEvenACalculationThatWouldHaveScaled() {
        // The override is applied after the switch, so it wins over any arithmetic.
        Spell rewritten = SpellWith(SpellCalculation.CostTimesDamage, 3);

        Assert.Equal(1000, SpellEffectMagnitude.Calculate(rewritten, SpellIds.MadGodsRage, 20));
    }
}
