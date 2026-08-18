namespace BetrayalAtKrondor.Tests.Character;

using GameData.Resources.Character;
using Xunit;

/// <summary>
/// The four rows inside the ratings panel (<c>UI_show_attribute_x_of_y</c> @0x5800f). Canonical
/// 1600x1200.
/// </summary>
public class CharacterSheetPanelRowTests {
    [Fact]
    public void OnlyTheFirstTwoRowsCarryAMaximum() {
        // cmp si, 1 / jg return at 0x580b2: Health and Stamina read "x of y", Speed and Strength
        // are a bare number even though the drawer has their maximum to hand.
        Assert.True(CharacterSheetPanelRow.ShowsMaximum(0));
        Assert.True(CharacterSheetPanelRow.ShowsMaximum(1));
        Assert.False(CharacterSheetPanelRow.ShowsMaximum(2));
        Assert.False(CharacterSheetPanelRow.ShowsMaximum(3));
    }

    [Fact]
    public void TheFourRowsAreElevenOriginalRowsApartStartingAtTwentyEight() {
        Assert.Equal(28 * 6, CharacterSheetPanelRow.RowY(0));
        Assert.Equal(11 * 6, CharacterSheetPanelRow.RowY(1) - CharacterSheetPanelRow.RowY(0));
        Assert.Equal(11 * 6, CharacterSheetPanelRow.RowY(3) - CharacterSheetPanelRow.RowY(2));
    }

    [Fact]
    public void EveryRowSitsInsideThePanel() {
        for (int attribute = 0; attribute < CharacterSheetPanelRow.Count; attribute++) {
            Assert.InRange(CharacterSheetPanelRow.RowY(attribute),
                CharacterSheetLayout.PanelY,
                CharacterSheetLayout.PanelY + CharacterSheetLayout.PanelHeight);
        }
    }

    [Fact]
    public void ThePanelRowsAreNotPlacedByTheLowerHalfsFormula() {
        // The sheet has two row drawers with different arithmetic, and the lower half's accepts any
        // attribute number and answers plausibly for the panel's four. It puts three of them on the
        // wrong line -- and agrees on the fourth, which is the trap: checking one row would clear a
        // port that has the other three wrong.
        Assert.Equal(CharacterSheetRow.RowY(1), CharacterSheetPanelRow.RowY(1));
        foreach (int attribute in new[] { 0, 2, 3 }) {
            Assert.NotEqual(CharacterSheetRow.RowY(attribute),
                CharacterSheetPanelRow.RowY(attribute));
        }
    }

    [Fact]
    public void TheTwoNumbersAreRightAlignedOnOppositeSidesOfTheWordBetweenThem() {
        Assert.True(CharacterSheetPanelRow.ValueRightX < CharacterSheetPanelRow.SeparatorX);
        Assert.True(CharacterSheetPanelRow.SeparatorX < CharacterSheetPanelRow.MaximumRightX);
        Assert.True(CharacterSheetPanelRow.NameX < CharacterSheetPanelRow.ValueRightX);
    }

    [Fact]
    public void AnUnchangedRowTakesTheTextRoutinesDefaultsRatherThanTheLowerHalfsPens() {
        // DisplayText @0x5634d substitutes 0x9F/0 for the -1 the sheet passes. The lower half's
        // rows name 0x0A/0x01 for themselves, so reusing those here would ink four rows wrongly.
        Assert.Equal(0x9F, CharacterSheetPanelRow.RowPen(changedSinceLastSeen: false));
        Assert.Equal(0x00, CharacterSheetPanelRow.RowShadowPen(changedSinceLastSeen: false));
        Assert.NotEqual(CharacterSheetRow.Pen, CharacterSheetPanelRow.Pen);
    }

    [Fact]
    public void AChangedRowIsInkedTheSameWayAChangedRatingIs() {
        Assert.Equal(CharacterSheetRow.ChangedPen,
            CharacterSheetPanelRow.RowPen(changedSinceLastSeen: true));
        Assert.Equal(CharacterSheetRow.ChangedShadowPen,
            CharacterSheetPanelRow.RowShadowPen(changedSinceLastSeen: true));
    }

    [Fact]
    public void BothDrawersReadTheSameChangedFlag() =>
        // Same base, same 17-wide stride — one flag per actor attribute, whichever drawer shows it.
        Assert.Equal(CharacterSheetRow.ChangedFlagFor(2, 1),
            CharacterSheetPanelRow.ChangedFlagFor(2, 1));
}
