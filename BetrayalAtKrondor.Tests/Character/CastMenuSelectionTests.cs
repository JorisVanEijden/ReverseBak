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
    public void TheDefaultSchoolIsSYMBOL5_WhichIsINDEX4() {
        // *** THIS TEST USED TO PIN THE OFF-BY-ONE. *** It asserted 5 under the name
        // "TheDefaultSchoolIsTheLastOneNotTheFirst", from a note reading "the original opens on 5".
        // The original does open on 5 — SYMBOL5.DAT — but the SYMBOL files are 1-BASED and this
        // index is 0-based (CastScreen.LoadSchoolAsync loads SYMBOL{school + 1}.DAT), so 5 loaded
        // SYMBOL6: one school too far.
        //
        // Why it stayed invisible: SYMBOL6's spells are 0, 2, 26, 34, 35, and the only one Owyn
        // knows is Candle Glow, which is correctly refused above ground. The ring therefore drew
        // nothing and the panel named nothing, which read as a dead screen (TASK-332) rather than
        // as the wrong school. SYMBOL5 yields Scent of Sarig — the spell the original names in the
        // right panel on open, which is what the task recorded seeing.
        Assert.Equal(4, CastMenuSelection.DefaultSchool);
        Assert.Equal(4, CastMenuSelection.ResolveSchool(CastMenuSelection.None));
        Assert.Equal(4, CastMenuSelection.ResolveSchool(6));

        // Still in range, so ResolveSchool's own bounds test is unaffected by the change.
        Assert.InRange(CastMenuSelection.DefaultSchool, 0, CastRingLayout.CategoryCount - 1);
    }

    [Fact]
    public void AFaceThatCannotCastIsClickableButDoesNotBecomeTheCaster() {
        Assert.True(CastMenuSelection.CanSelect(1, OwynOnly));
        Assert.False(CastMenuSelection.CanSelect(0, OwynOnly));
        Assert.False(CastMenuSelection.CanSelect(9, OwynOnly));
    }
}
