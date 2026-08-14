namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The cast screen's sticky caster/school pair. The fallback when the remembered caster is no
/// longer a caster is the rule a port drops.
/// </summary>
public class CastMenuSelectionTests {
    private static readonly bool[] OwynOnly = { false, true, false };
    private static readonly bool[] TwoCasters = { false, true, true };
    private static readonly bool[] NoCasters = { false, false, false };

    [Fact]
    public void TheScreenReopensOnTheRememberedCaster() {
        Assert.Equal(2, CastMenuSelection.ResolveCasterSlot(2, TwoCasters));
    }

    [Fact]
    public void ARememberedSlotThatCanNoLongerCastFallsBackToTheFirstWhoCan() {
        // The party is reordered and swapped between chapters, so the saved slot may now hold a
        // non-caster.
        Assert.Equal(1, CastMenuSelection.ResolveCasterSlot(0, OwynOnly));
    }

    [Fact]
    public void NothingRememberedOpensOnTheFirstCaster() {
        Assert.Equal(1, CastMenuSelection.ResolveCasterSlot(CastMenuSelection.None, OwynOnly));
    }

    [Fact]
    public void APartyWithNoCasterResolvesToNothing() {
        Assert.Equal(CastMenuSelection.None, CastMenuSelection.ResolveCasterSlot(1, NoCasters));
        Assert.Equal(CastMenuSelection.None, CastMenuSelection.ResolveCasterSlot(1, null));
    }

    [Fact]
    public void AnOutOfRangeRememberedSlotFallsBackRatherThanThrowing() {
        Assert.Equal(1, CastMenuSelection.ResolveCasterSlot(7, OwynOnly));
        Assert.Equal(1, CastMenuSelection.ResolveCasterSlot(-4, OwynOnly));
    }

    [Fact]
    public void TheRingOpensOnTheRememberedSchool() {
        Assert.Equal(3, CastMenuSelection.ResolveSchool(3));
        Assert.Equal(0, CastMenuSelection.ResolveSchool(0));
    }

    [Fact]
    public void TheDefaultSchoolIsTheLastOneNotTheFirst() {
        Assert.Equal(5, CastMenuSelection.DefaultSchool);
        Assert.Equal(5, CastMenuSelection.ResolveSchool(CastMenuSelection.None));
        Assert.Equal(5, CastMenuSelection.ResolveSchool(6));
    }

    [Fact]
    public void AFaceThatCannotCastIsClickableButDoesNotBecomeTheCaster() {
        Assert.True(CastMenuSelection.CanSelect(1, OwynOnly));
        Assert.False(CastMenuSelection.CanSelect(0, OwynOnly));
        Assert.False(CastMenuSelection.CanSelect(9, OwynOnly));
    }
}
