namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using System.Collections.Generic;
using System.Linq;
using Xunit;

/// <summary>
/// The travel screen's active-effects strip — <c>UI_DrawActiveSpellSymbols</c>.
/// </summary>
public class SpellEffectCaptionTests {
    [Fact]
    public void EveryGlyphIsOneABOVEItsTableEntry() {
        // The routine reads the table and increments before storing, so the table is not the answer
        // — and reading it straight gives nine WRONG symbols rather than nine missing ones.
        Assert.Equal(0x1e, SpellEffectCaption.GlyphFor(0));
        Assert.Equal(0x26, SpellEffectCaption.GlyphFor(8));
    }

    [Fact]
    public void EffectsThreeAndFourTakeEachOthersPlaceInTheSequence() {
        // The one break in an otherwise consecutive table. Computing the glyph as 0x1e + effect
        // agrees everywhere else, which is exactly what would make this pair hard to notice.
        Assert.Equal(0x22, SpellEffectCaption.GlyphFor(3));
        Assert.Equal(0x21, SpellEffectCaption.GlyphFor(4));

        IEnumerable<int> computed = Enumerable.Range(0, SpellPaletteEvents.Count).Select(e => 0x1e + e);
        IEnumerable<int> actual = Enumerable.Range(0, SpellPaletteEvents.Count).Select(SpellEffectCaption.GlyphFor);
        Assert.Equal(2, computed.Zip(actual, (c, a) => c == a ? 0 : 1).Sum());
    }

    [Fact]
    public void AnEffectIdOutsideTheTableDrawsNothing() {
        Assert.Equal(0, SpellEffectCaption.GlyphFor(-1));
        Assert.Equal(0, SpellEffectCaption.GlyphFor(SpellPaletteEvents.Count));
    }

    [Fact]
    public void TheSymbolsComeOutInEFFECTOrderWhateverTheMask() {
        // The bits are walked from zero, so the strip reads the same whichever effect started
        // first — worth pinning, because a port that appends on cast would order them by time.
        int mask = SpellPaletteEvents.BitFor(5) | SpellPaletteEvents.BitFor(1);

        Assert.Equal(new[] { SpellEffectCaption.GlyphFor(1), SpellEffectCaption.GlyphFor(5) },
            SpellEffectCaption.Glyphs(mask));
    }

    [Fact]
    public void NothingRunningIsAnEmptyCaptionRatherThanNoWidget() {
        Assert.Empty(SpellEffectCaption.Glyphs(0));
        Assert.True(SpellEffectCaption.PlaqueDrawsWhenNothingIsActive);
    }

    [Fact]
    public void EveryEffectTheMaskCanHoldHasASymbol() {
        // Nine bits, nine glyphs — the caption cannot run short of the mask.
        int all = 0;
        for (var e = 0; e < SpellPaletteEvents.Count; e++) {
            all |= SpellPaletteEvents.BitFor(e);
        }

        Assert.Equal(SpellPaletteEvents.Count, SpellEffectCaption.Glyphs(all).Count);
        Assert.Equal(SpellEffectCaption.Glyphs(all).Distinct().Count(), SpellEffectCaption.Glyphs(all).Count);
    }
}
