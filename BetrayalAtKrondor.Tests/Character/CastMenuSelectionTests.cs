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
    public void TheDefaultSchoolIsSYMBOL6_WhichIsINDEX5() {
        // *** THIS TEST ONCE PINNED 4, AND 4 WAS WRONG. *** The argument for it was a screenshot:
        // opening the screen on SYMBOL6 with Owyn above ground draws an EMPTY ring, because the one
        // spell he knows there is Candle Glow and Candle Glow is refused above ground. That reads
        // like an off-by-one and is not one.
        //
        // The original settles it twice over. cspell_cast_menu_loop assigns school = 5 with nothing
        // remembered (CSPELL.C:2181), and cspell_symbol_resources_load builds the name with
        // szFile[6] = chapter + '1' (CSPELL.C:1745) — so 5 IS SYMBOL6, the same 0-based/1-based
        // relationship CastScreen.LoadSchoolAsync uses. And the ring draws only CASTABLE spells
        // (cspell_menu_animate_hilite gates each glyph on cspell_check_castable, CSPELL.C:1840), so
        // the original draws the same empty ring from the same save. The emptiness was the correct
        // rendering of that state.
        Assert.Equal(5, CastMenuSelection.DefaultSchool);
        Assert.Equal(5, CastMenuSelection.ResolveSchool(CastMenuSelection.None));
        Assert.Equal(5, CastMenuSelection.ResolveSchool(6));

        // Still in range, so ResolveSchool's own bounds test is unaffected.
        Assert.InRange(CastMenuSelection.DefaultSchool, 0, CastRingLayout.CategoryCount - 1);
    }

    [Fact]
    public void AFaceThatCannotCastIsClickableButDoesNotBecomeTheCaster() {
        Assert.True(CastMenuSelection.CanSelect(1, OwynOnly));
        Assert.False(CastMenuSelection.CanSelect(0, OwynOnly));
        Assert.False(CastMenuSelection.CanSelect(9, OwynOnly));
    }
}
