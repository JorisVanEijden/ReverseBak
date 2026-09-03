namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The one encounter that fights against a painted backdrop.
/// </summary>
public class CombatBackdropTests {
    [Fact]
    public void OnlyEncounter545CarriesOne() {
        Assert.Equal("FCOMBAT.SCX", CombatBackdrop.ImageFor(545));
        Assert.Null(CombatBackdrop.ImageFor(544));
        Assert.Null(CombatBackdrop.ImageFor(546));
    }

    [Fact]
    public void EncounterZeroIsNOTAMatch() {
        // Zero means "every loaded record" to the dialog sub-actions next door, and -1 is what the
        // hotspot service holds when no fight is running. Neither may pick up a backdrop.
        Assert.Null(CombatBackdrop.ImageFor(0));
        Assert.Null(CombatBackdrop.ImageFor(-1));
    }

    [Fact]
    public void ThePaletteIsAlreadyKnown() {
        // PaletteMapping had FCOMBAT -> OPTIONS.PAL before anything displayed the image; the two
        // must not drift apart into a picture drawn with the wrong pens.
        Assert.Equal("OPTIONS.PAL", GameData.PaletteMapping.GetPaletteFor("FCOMBAT"));
    }
}
