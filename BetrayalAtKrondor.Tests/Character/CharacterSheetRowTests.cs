namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using Xunit;

/// <summary>
/// A rating row on the character sheet (<c>charscreen_draw_sheet_stat_row</c> @0x57dec).
/// </summary>
[Collection(BetrayalAtKrondor.Tests.Text.UiStringsCollection.Name)]
public class CharacterSheetRowTests {
    // ---- the two columns ----------------------------------------------------------------------

    [Fact]
    public void StatsGoLeftAndSkillsGoRight() {
        Assert.False(CharacterSheetRow.IsSkill(0));
        Assert.False(CharacterSheetRow.IsSkill(9));
        Assert.True(CharacterSheetRow.IsSkill(10));

        Assert.Equal(30 * 5, CharacterSheetRow.ColumnX(9));
        Assert.Equal(147 * 5, CharacterSheetRow.ColumnX(10));
    }

    [Fact]
    public void EveryRowCarriesABarAndTheColumnOnlyDecidesWhichSide() {
        // An earlier reading had bars belonging to the right column alone. The cmp against the
        // column x chooses the SIDE and the mirror, not whether there is a bar: both arms draw the
        // empty icon and both draw a fill.
        for (int attribute = 0; attribute < CharacterSheetRow.DisplayableAttributes; attribute++) {
            Assert.True(CharacterSheetRow.ShowsBar(attribute));
        }

        Assert.False(CharacterSheetRow.BarIsMirrored(4));
        Assert.True(CharacterSheetRow.BarIsMirrored(10));
        // The left column's bar hangs off the LEFT of its column and the right one off the right.
        Assert.True(CharacterSheetRow.BarX(4) < CharacterSheetRow.ColumnX(4));
        Assert.True(CharacterSheetRow.BarX(10) > CharacterSheetRow.ColumnX(10));
    }

    [Fact]
    public void TheTwoColumnsFillFromOppositeEnds() {
        (int leftColumnLow, int leftColumnHigh) = CharacterSheetRow.BarFillClip(4, 50);
        (int rightColumnLow, int rightColumnHigh) = CharacterSheetRow.BarFillClip(10, 50);

        // The right column grows rightward from a fixed left edge; the left column grows LEFTWARD
        // from a fixed right edge. Filling both the same way runs one of them backwards, which only
        // shows once a rating is part-full.
        Assert.Equal(CharacterSheetRow.BarFillClip(4, 100).Right, leftColumnHigh);
        Assert.True(CharacterSheetRow.BarFillClip(4, 100).Left < leftColumnLow);
        Assert.Equal(CharacterSheetRow.BarFillClip(10, 100).Left, rightColumnLow);
        Assert.True(CharacterSheetRow.BarFillClip(10, 100).Right > rightColumnHigh);
    }

    [Fact]
    public void AnEmptyRatingDrawsNoFillAtAll() {
        // Not a zero-width fill: the original returns before drawing it.
        Assert.False(CharacterSheetRow.BarHasFill(0));
        Assert.False(CharacterSheetRow.BarHasFill(-5));
        Assert.True(CharacterSheetRow.BarHasFill(1));
    }

    [Fact]
    public void TheFillIsItsOwnBitmapRatherThanAShortenedSword() {
        // The empty bar is drawn whole and a DIFFERENT icon is drawn over it inside the clip, so a
        // half-full bar is not a half-length sword.
        Assert.NotEqual(CharacterSheetRow.BarEmptyIcon, CharacterSheetRow.BarFillIcon);
        Assert.NotEqual(CharacterSheetRow.BarFillIcon, CharacterSheetRow.BarEndMarkerIcon);
    }

    [Fact]
    public void TheLeftColumnRunsCleanlyFromTopToBottom() {
        // C's truncating remainder makes attributes below 4 land ABOVE the nominal first row rather
        // than wrapping to the bottom — which is what turns the formula into a plain 10-row run.
        Assert.Equal(23 * 6, CharacterSheetRow.RowY(0));
        Assert.Equal(87 * 6, CharacterSheetRow.RowY(4));
        Assert.Equal(167 * 6, CharacterSheetRow.RowY(9));
    }

    [Fact]
    public void EveryLeftColumnRowIsSixteenOriginalRowsBelowTheLast() {
        for (var attribute = 1; attribute < CharacterSheetRow.FirstRightColumnAttribute; attribute++) {
            Assert.Equal(16 * 6,
                CharacterSheetRow.RowY(attribute) - CharacterSheetRow.RowY(attribute - 1));
        }
    }

    [Fact]
    public void AFlooredModuloWouldFoldTheFirstFourRowsToTheWrongEnd() {
        // The bug this pins: with a floored modulo, (0-4) mod 6 = 2 and attribute 0 would land on
        // attribute 6's line instead of above attribute 1.
        Assert.NotEqual(CharacterSheetRow.RowY(6), CharacterSheetRow.RowY(0));
        Assert.True(CharacterSheetRow.RowY(0) < CharacterSheetRow.RowY(1));
    }

    [Fact]
    public void TheRightColumnStartsItsOwnRunOfSix() {
        Assert.Equal(87 * 6, CharacterSheetRow.RowY(10));
        Assert.Equal(167 * 6, CharacterSheetRow.RowY(15));
    }

    [Fact]
    public void ASeventeenthAttributeWouldCollide() =>
        // The arithmetic repeats every six rows, so 16 lands on 10's line. Recorded as a bound
        // rather than discovered as an overlap.
        Assert.Equal(CharacterSheetRow.RowY(CharacterSheetRow.FirstRightColumnAttribute),
            CharacterSheetRow.RowY(CharacterSheetRow.DisplayableAttributes));

    // ---- the value ------------------------------------------------------------------------------

    [Fact]
    public void ARatingWithNoMaximumReadsAsUnavailable() =>
        // Tested on the MAXIMUM: never had it, versus lost all of it. The text comes from the
        // catalog rather than a literal, so a translation reaches it.
        Assert.Equal(GameData.Resources.Text.UiStrings.Get(CharacterSheetRow.NotApplicableKey),
            CharacterSheetRow.ValueText(maximum: 0, percentage: 40));

    [Fact]
    public void ARatingDrainedToNothingStillReadsAsAPercentage() =>
        Assert.Equal("  0%", CharacterSheetRow.ValueText(maximum: 80, percentage: 0));

    [Fact]
    public void TheValueIsPaddedToThreeColumns() {
        Assert.Equal("  5%", CharacterSheetRow.ValueText(maximum: 80, percentage: 5));
        Assert.Equal(" 50%", CharacterSheetRow.ValueText(maximum: 80, percentage: 50));
        Assert.Equal("100%", CharacterSheetRow.ValueText(maximum: 80, percentage: 100));
    }

    [Theory]
    [InlineData(-20, 0)]
    [InlineData(0, 0)]
    [InlineData(55, 55)]
    [InlineData(100, 100)]
    [InlineData(140, 100)]
    public void TheBarIsClampedBothWays(int percentage, int expected) =>
        // Over 100 fills the bar rather than overrunning it; below zero empties it rather than
        // drawing backwards.
        Assert.Equal(expected, CharacterSheetRow.ClampPercentage(percentage));

    // ---- the change highlight ---------------------------------------------------------------------

    [Fact]
    public void AChangedRatingIsInkedDifferently() {
        Assert.Equal(CharacterSheetRow.ChangedPen, CharacterSheetRow.NamePen(true));
        Assert.Equal(CharacterSheetRow.Pen, CharacterSheetRow.NamePen(false));
        Assert.NotEqual(CharacterSheetRow.NamePen(true), CharacterSheetRow.NamePen(false));
        Assert.NotEqual(CharacterSheetRow.NameShadowPen(true), CharacterSheetRow.NameShadowPen(false));
    }

    [Fact]
    public void EachActorsFlagsAreTheirOwn() {
        // Seventeen per actor, so two characters' ratings cannot alias each other's highlights.
        Assert.Equal(CharacterSheetRow.ChangedFlagBase, CharacterSheetRow.ChangedFlagFor(0, 0));
        Assert.Equal(CharacterSheetRow.ChangedFlagBase + 17, CharacterSheetRow.ChangedFlagFor(1, 0));
        Assert.NotEqual(CharacterSheetRow.ChangedFlagFor(0, 16), CharacterSheetRow.ChangedFlagFor(1, 0));
    }
}
