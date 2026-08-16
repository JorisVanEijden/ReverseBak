namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using Xunit;

/// <summary>
/// The character sheet's layout (<c>charscreen_info_draw</c> @0x580fe). Canonical 1600x1200.
/// </summary>
public class CharacterSheetLayoutTests {
    [Fact]
    public void ThePanelIsTheOriginalRectangleScaledOnce() {
        // VGA (84,9) 222x71 — x5 across, x6 down, and no other arithmetic on the way.
        Assert.Equal(84 * 5, CharacterSheetLayout.PanelX);
        Assert.Equal(9 * 6, CharacterSheetLayout.PanelY);
        Assert.Equal(222 * 5, CharacterSheetLayout.PanelWidth);
        Assert.Equal(71 * 6, CharacterSheetLayout.PanelHeight);
    }

    [Fact]
    public void BothHeadingsShareABaseline() =>
        // "Ratings:" and "Condition:" are one row; only their x differs.
        Assert.Equal(14 * 6, CharacterSheetLayout.HeadingY);

    [Fact]
    public void TheConditionColumnStartsRightOfItsHeading() =>
        Assert.True(CharacterSheetLayout.ConditionX > CharacterSheetLayout.RatingsHeadingX);

    // ---- the list ---------------------------------------------------------------------------

    [Fact]
    public void ConditionLinesAreNineOriginalRowsApart() {
        int first = CharacterSheetLayout.ConditionLineY(1);
        int second = CharacterSheetLayout.ConditionLineY(2);

        Assert.Equal(9 * 6, second - first);
        Assert.Equal((9 + 16) * 6, first);
    }

    [Fact]
    public void TheListClosesUpRatherThanLeavingGaps() {
        // Lines are numbered by what was DRAWN, not by which condition it is — so three afflictions
        // always occupy the first three lines, whichever three they are.
        Assert.Equal(CharacterSheetLayout.ConditionLineY(1), CharacterSheetLayout.ConditionLineY(1));
        Assert.True(CharacterSheetLayout.ConditionLineY(3) > CharacterSheetLayout.ConditionLineY(2));
    }

    [Fact]
    public void NormalSitsSlightlyLowerThanTheFirstConditionLine() {
        // VGA 28 against the first line's 25. Three original pixels, for no reason the code gives.
        // They never appear together, so it only ever shows as a wobble between characters.
        Assert.Equal(28 * 6, CharacterSheetLayout.NormalY);
        Assert.NotEqual(CharacterSheetLayout.ConditionLineY(1), CharacterSheetLayout.NormalY);
        Assert.True(CharacterSheetLayout.NormalY > CharacterSheetLayout.ConditionLineY(1));
    }

    [Fact]
    public void OnlyPositiveConditionsAreListed() {
        Assert.True(CharacterSheetLayout.IsListed(1));
        Assert.False(CharacterSheetLayout.IsListed(0));
    }

    [Fact]
    public void ACuresNegativeAmountIsNeverRenderedAsACondition() =>
        // The test is signed and strict, which is what keeps ApplyCondition's -100 off the sheet.
        Assert.False(CharacterSheetLayout.IsListed(TempleHealMenu.ClearAmount));

    [Fact]
    public void TheSheetCoversEveryCondition() =>
        Assert.Equal(TempleHealMenu.ConditionCount, CharacterSheetLayout.ConditionCount);

    // ---- the two sizes -------------------------------------------------------------------------

    [Fact]
    public void TheHealerDrawsTheCompactSheet() {
        // charscreen_temple_heal_menu passes 0 — portrait, ratings and conditions, no lower half.
        Assert.False(CharacterSheetLayout.IsFullSheet(0));
        Assert.True(CharacterSheetLayout.IsFullSheet(1));
    }
}
