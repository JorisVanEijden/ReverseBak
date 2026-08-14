namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// What happens to a spell's cost before anything is computed from it. The cost is the effect dial,
/// so these are damage rules wearing a price tag.
/// </summary>
public class SpellCostModifiersTests {
    [Fact]
    public void AnUnmodifiedCostPassesThroughUntouched() {
        Assert.Equal(40, SpellCostModifiers.Effective(40, surcharged: false, targetIsWeak: false));
    }

    [Fact]
    public void TheSurchargeIsHalfAgain() {
        Assert.Equal(60, SpellCostModifiers.Effective(40, surcharged: true, targetIsWeak: false));
    }

    [Fact]
    public void AWeakTargetDoublesTheDial() {
        Assert.Equal(80, SpellCostModifiers.Effective(40, surcharged: false, targetIsWeak: true));
    }

    [Fact]
    public void TheOrderDoesNotCommute() {
        // Surcharge from the original, then sign, then doubling: |c + c/2| * 2, not |c| * 2 + c/2.
        Assert.Equal(120, SpellCostModifiers.Effective(40, surcharged: true, targetIsWeak: true));
        Assert.NotEqual((40 * 2) + 20,
            SpellCostModifiers.Effective(40, surcharged: true, targetIsWeak: true));
    }

    [Fact]
    public void ANegativeCostIsASignPlusAMagnitude() {
        // Everything downstream works with a positive number; feeding the negative straight into the
        // magnitude would invert every scaled effect it touches.
        Assert.True(SpellCostModifiers.IsNegated(-40));
        Assert.False(SpellCostModifiers.IsNegated(40));
        Assert.Equal(40, SpellCostModifiers.Effective(-40, surcharged: false, targetIsWeak: false));
    }

    [Fact]
    public void TheSurchargeIsTakenBeforeTheSignIsStripped() {
        // -40 surcharged is -60, which becomes 60 — not 40 + 20.
        Assert.Equal(60, SpellCostModifiers.Effective(-40, surcharged: true, targetIsWeak: false));
    }

    [Fact]
    public void TheSurchargeComesFromTheOriginalNotTheRunningValue() {
        // So it is exactly +50% and never compounds with the doubling.
        Assert.Equal(20, SpellCostModifiers.Surcharge(40));
        Assert.Equal(20, SpellCostModifiers.Surcharge(41));
    }

    [Fact]
    public void OneTargetingTypeThrowsItsTargetAway() {
        // Discarded outright, so every later step behaves as if the cast were untargeted — including
        // the weakness check.
        Assert.True(SpellCostModifiers.DiscardsTarget(8));
        Assert.False(SpellCostModifiers.DiscardsTarget(0));
        Assert.False(SpellCostModifiers.DiscardsTarget(5));
    }

    [Fact]
    public void ZeroStaysZeroWhateverIsAppliedToIt() {
        Assert.Equal(0, SpellCostModifiers.Effective(0, surcharged: true, targetIsWeak: true));
    }

    [Fact]
    public void TheSpellTableRecordIsTwentyTwoBytes() {
        Assert.Equal(22, SpellCostModifiers.SpellRecordSize);
    }
}
