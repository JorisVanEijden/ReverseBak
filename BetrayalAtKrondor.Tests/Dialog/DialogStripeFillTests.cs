namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>The dialog fill's phase (<c>dialog_DrawChrome</c>'s FILL phase @0x486a1).</summary>
public class DialogStripeFillTests {
    [Fact]
    public void AnOrdinaryEntryIsRePhasedEachDrawing() =>
        // Randomised is the DEFAULT. The flag is what turns it off, not what turns it on.
        Assert.True(DialogStripeFill.IsRandomised(DialogEntryFlags.None));

    [Fact]
    public void TheFlagPinsThePattern() =>
        Assert.False(DialogStripeFill.IsRandomised(DialogEntryFlags.FixedStripePattern));

    [Fact]
    public void TheFlagIsFoundAmongOthers() =>
        Assert.False(DialogStripeFill.IsRandomised(
            DialogEntryFlags.CenterText | DialogEntryFlags.FixedStripePattern
            | DialogEntryFlags.SkipWait));

    [Fact]
    public void APinnedEntryAlwaysStartsAtTheBase() {
        for (var roll = 0; roll < 250; roll++) {
            Assert.Equal(0, DialogStripeFill.PhaseFor(DialogEntryFlags.FixedStripePattern, roll));
        }
    }

    [Fact]
    public void APhaseStaysInsideTheWindow() {
        for (var roll = 0; roll < 500; roll++) {
            int phase = DialogStripeFill.PhaseFor(DialogEntryFlags.None, roll);
            Assert.InRange(phase, 0, DialogStripeFill.PhaseWindow - 1);
        }
    }

    [Fact]
    public void TheWindowIsTheOriginalsHundredBytes() =>
        Assert.Equal(100, DialogStripeFill.PhaseWindow);

    [Fact]
    public void ANegativeRollStillLandsInTheWindow() =>
        // Guards the modulo: C#'s % keeps the dividend's sign, so a negative roll would produce a
        // negative offset and push the fill the wrong way.
        Assert.InRange(DialogStripeFill.PhaseFor(DialogEntryFlags.None, -37),
            0, DialogStripeFill.PhaseWindow - 1);
}
