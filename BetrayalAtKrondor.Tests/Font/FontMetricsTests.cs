namespace BetrayalAtKrondor.Tests.Font;

using GameData.Resources.Font;
using Xunit;

/// <summary>The engine's one text measurement — <c>getStringWidthInPixels</c>.</summary>
public class FontMetricsTests {
    /// <summary>A font covering 'A'..'C' with deliberately different widths.</summary>
    private static FontResource Font() {
        var font = new FontResource("test") { FirstCharacter = 'A', Height = 8 };
        font.Glyphs.Add(new FontGlyph { Width = 3 });   // A
        font.Glyphs.Add(new FontGlyph { Width = 5 });   // B
        font.Glyphs.Add(new FontGlyph { Width = 7 });   // C
        return font;
    }

    [Fact]
    public void AStringIsThePlainSumOfItsGlyphWidths() {
        // *** NO LETTER SPACING AND NO KERNING. *** A port that added even one pixel between
        // glyphs would widen every measured string by its length, and every width-fitted box
        // with it.
        Assert.Equal(3 + 5 + 7, FontMetrics.TextWidth(Font(), "ABC"));
        Assert.Equal(3 + 3, FontMetrics.TextWidth(Font(), "AA"));
    }

    [Fact]
    public void ACharacterTheFontDoesNotCarryContributesNothing() {
        // The original range-checks and skips, with no fallback glyph — so a string with
        // characters outside the font silently under-measures. Faithful, and the reason this
        // returns a width rather than reporting the miss.
        Assert.Equal(3 + 7, FontMetrics.TextWidth(Font(), "AZC"));
        Assert.Equal(0, FontMetrics.TextWidth(Font(), "z"));
        Assert.Equal(0, FontMetrics.TextWidth(Font(), "@"));   // below FirstCharacter
    }

    [Fact]
    public void NothingToMeasureIsZero() {
        Assert.Equal(0, FontMetrics.TextWidth(Font(), ""));
        Assert.Equal(0, FontMetrics.TextWidth(Font(), null));
        Assert.Equal(0, FontMetrics.TextWidth(null, "ABC"));
    }

    [Fact]
    public void ARowTakesTheWidestLabel_NotEachLabelsOwn() {
        // One width for the whole row: the menu builder keeps a running maximum and gives every
        // button that same width, so buttons are uniform rather than fitted.
        Assert.Equal(7, FontMetrics.WidestOf(Font(), new[] { "A", "B", "C" }));
        Assert.Equal(3 + 5, FontMetrics.WidestOf(Font(), new[] { "AB", "C" }));
    }

    [Fact]
    public void AnEmptyRowIsZeroWide() {
        Assert.Equal(0, FontMetrics.WidestOf(Font(), new string[0]));
        Assert.Equal(0, FontMetrics.WidestOf(Font(), null));
    }

    [Fact]
    public void TheButtonWidthIsTheWidestLabelPlusThePadding() {
        // The two halves meet here: FontMetrics answers the width and DialogButtonRow adds the
        // original's `add ax, 10` at 0x4b2ca.
        int widest = FontMetrics.WidestOf(Font(), new[] { "A", "BC" });
        Assert.Equal(widest + GameData.Resources.Dialog.DialogButtonRow.LabelPadding,
            GameData.Resources.Dialog.DialogButtonRow.ButtonWidth(widest));
    }
}
