namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Spells;
using Xunit;

/// <summary>
/// The spellbook page's geometry (<c>charscreen_draw_spell_book_actor</c>). Canonical 1600x1200.
/// </summary>
public class SpellBookPageLayoutTests {
    [Fact]
    public void TheRowsAreCountedFromOne() {
        // VGA row * 0x20 - 0x1b. Counting from zero lifts the page 32 rows and puts the first box
        // off the top of the screen.
        Assert.Equal(5 * 6, SpellBookPageLayout.RowY(1));
        Assert.Equal(0x20 * 6, SpellBookPageLayout.RowY(2) - SpellBookPageLayout.RowY(1));
    }

    [Fact]
    public void EveryRowFitsBelowTheOneBefore() {
        for (var row = 1; row < SpellBookPageLayout.Rows; row++) {
            Assert.True(
                SpellBookPageLayout.RowY(row) + SpellBookPageLayout.BoxHeight
                <= SpellBookPageLayout.RowY(row + 1),
                $"row {row} overlaps row {row + 1}");
        }
    }

    [Fact]
    public void TheBoxIsShadowedByAWholeSecondRectangle() {
        // Not a border and not a blur: the original fills a black box of the SAME SIZE one pixel
        // down-right, before painting the real one.
        Assert.Equal(0, SpellBookPageLayout.BoxShadowPen);
        Assert.NotEqual(SpellBookPageLayout.BoxFillPen, SpellBookPageLayout.BoxShadowPen);
        Assert.Equal(5, SpellBookPageLayout.ShadowOffsetX);
        Assert.Equal(6, SpellBookPageLayout.ShadowOffsetY);
    }

    [Fact]
    public void TheIconAndTheListSitInsideTheirRow() {
        Assert.True(SpellBookPageLayout.IconX > SpellBookPageLayout.BoxX);
        // The list starts beyond the box, not inside it: the box holds only the icon.
        Assert.True(SpellBookPageLayout.TextX
            > SpellBookPageLayout.BoxX + SpellBookPageLayout.BoxWidth);
    }

    [Fact]
    public void TheListIsWrittenTwiceInDifferentPens() {
        Assert.NotEqual(SpellBookPageLayout.TextPen, SpellBookPageLayout.TextShadowPen);
    }
}
