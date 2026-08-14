namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// Applying a spell's magnitude. The duration case and resistance both live here rather than in the
/// magnitude function, which is what makes that function's zero answer correct.
/// </summary>
public class SpellEffectApplicationTests {
    [Fact]
    public void TheDurationMagnitudeIsComputedHereNotByTheMagnitudeFunction() {
        // SpellEffectMagnitude answering 0 for this calculation is a division of labour, not a gap.
        var spell = new Spell("s") { Calculation = SpellCalculation.CostTimesDuration, Duration = 5 };

        Assert.Equal(0, SpellEffectMagnitude.Calculate(spell, spellId: 1, spellCost: 8));
        Assert.Equal(40, SpellEffectApplication.DurationMagnitude(cost: 8, duration: 5));
    }

    [Fact]
    public void ResistanceCancelsTheEffectRatherThanReducingIt() {
        // Not symmetrical with weakness: weakness doubles the cost, resistance skips the application.
        Assert.True(SpellEffectApplication.ResistanceSkipsEffect(targetResists: true));
        Assert.False(SpellEffectApplication.ResistanceSkipsEffect(targetResists: false));
    }

    [Fact]
    public void WeaknessAndResistanceAreDifferentMechanismsNotOpposites() {
        // Weakness scales the dial; resistance is a gate. Mirroring them would let a resistant
        // creature still take a reduced effect where the original gives it none.
        Assert.Equal(80, SpellCostModifiers.Effective(40, surcharged: false, targetIsWeak: true));
        Assert.True(SpellEffectApplication.ResistanceSkipsEffect(true));
    }

    [Fact]
    public void AGridSpellsStrengthIsCostTimesDurationToo() {
        // Even though nothing about it lasts for a duration. Both grid paths share the formula.
        Assert.Equal(SpellEffectApplication.DurationMagnitude(6, 7),
            SpellEffectApplication.GridElementStrength(6, 7));
    }

    [Fact]
    public void ANegativeDurationDividesInsteadOfMultiplying() {
        // The exact mirror of what a negative effect does on the cost-times-damage calculation —
        // the sign of a record field flips scaling up into scaling down, with no flag to say so.
        Assert.Equal(40, SpellEffectApplication.DurationMagnitude(cost: 8, duration: 5));
        Assert.Equal(2, SpellEffectApplication.DurationMagnitude(cost: 8, duration: -4));
        Assert.True(SpellEffectApplication.NegativeDurationDivides);
    }

    [Fact]
    public void TheMostNegativeDurationIsGuardedRatherThanNegated() {
        // Negating it overflows, so the original substitutes the largest positive instead.
        Assert.Equal(0,
            SpellEffectApplication.DurationMagnitude(100, SpellEffectApplication.MostNegativeDuration));
        Assert.Equal(0x7fff, SpellEffectApplication.OverflowGuard);
    }

    [Fact]
    public void AZeroDurationWouldFaultAndIsAnsweredWithNothing() {
        // Zero falls into the divide branch in the original, dividing by nothing. No shipped spell
        // pairs this calculation with a zero duration.
        Assert.True(SpellEffectApplication.ZeroDurationWouldFault);
        Assert.Equal(0, SpellEffectApplication.DurationMagnitude(cost: 8, duration: 0));
    }

    [Fact]
    public void AnEffectLastsOneTickLongerOnATargetWithoutTheStatusBit() {
        // A flat bonus applied after the arithmetic, keyed on the TARGET's state — easy to miss, and
        // it shifts every duration by one.
        Assert.Equal(6, SpellEffectApplication.AdjustDurationForTarget(5, targetHasStatusBit: false));
        Assert.Equal(5, SpellEffectApplication.AdjustDurationForTarget(5, targetHasStatusBit: true));
    }

    [Fact]
    public void TheRegisteredEffectCarriesTheSpellsColourAsItsFlag() {
        // A value that reads as presentation doing duty as effect data.
        Assert.True(SpellEffectApplication.EffectFlagIsTheSpellColour);
    }

    [Fact]
    public void TwoDeliveryCategoriesSwingInsteadOfCasting() {
        Assert.True(SpellEffectApplication.SwingsInsteadOfCasting(1));
        Assert.True(SpellEffectApplication.SwingsInsteadOfCasting(4));
        Assert.False(SpellEffectApplication.SwingsInsteadOfCasting(0));
        Assert.False(SpellEffectApplication.SwingsInsteadOfCasting(8));
    }

    [Fact]
    public void AndThoseAreExactlyTheOnesThatTeachTheCasterNothing() {
        foreach (int category in SpellEffectApplication.MeleeSwingCategories) {
            Assert.False(SpellEffectApplication.AwardsCastingSkill(category));
        }
        foreach (int category in SpellEffectApplication.RangedWindupCategories) {
            Assert.True(SpellEffectApplication.AwardsCastingSkill(category));
        }
    }

    [Fact]
    public void TheGridCategoriesTeachNothingEither() {
        Assert.False(SpellEffectApplication.AwardsCastingSkill(5));
        Assert.False(SpellEffectApplication.AwardsCastingSkill(6));
    }

    [Fact]
    public void EveryDeliveryCategoryIsAccountedFor() {
        // Nine categories, split across wind-up, swing and the two grid ones.
        Assert.Equal(9,
            SpellEffectApplication.RangedWindupCategories.Length
            + SpellEffectApplication.MeleeSwingCategories.Length + 2);
    }

    [Fact]
    public void TheSkyfireRecheckIsRecordedRatherThanModelled() {
        Assert.True(SpellEffectApplication.SkyfireIsRecheckedAtApplication);
    }
}
