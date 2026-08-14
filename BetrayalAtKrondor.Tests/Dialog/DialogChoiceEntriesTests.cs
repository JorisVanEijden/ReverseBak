namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// Building an ordinary choice menu, and the latch clearing that makes it answerable more than once.
/// </summary>
public class DialogChoiceEntriesTests {
    [Fact]
    public void AChoiceMenuHasExactlyOneButtonPerBranch() {
        // No spare slot — unlike the keyword grid's farewell and the party row's cancel.
        Assert.Equal(2, DialogChoiceEntries.ButtonCount(2));
        Assert.NotEqual(PartyMemberPicker.ButtonCount(2), DialogChoiceEntries.ButtonCount(2));
    }

    [Fact]
    public void SoEscapingAChoiceMenuPicksItsLastBranch() {
        // The dismiss rule resolves to the last entry, and here every entry is a branch — which
        // makes the last branch the de-facto default and its order in the data significant.
        Assert.True(DialogChoiceMenu.DismissalPressesLastEntry(1));
        Assert.Equal(1, DialogChoiceEntries.ButtonCount(2) - 1);
    }

    [Fact]
    public void EveryCandidateLatchIsClearedBeforeTheMenuAppears() {
        // Both Yes and No, not just the one that will be chosen — otherwise a latch left set by an
        // earlier menu auto-matches and the player never sees the question.
        Assert.True(DialogChoiceEntries.ClearsEveryCandidateFirst);
        Assert.Equal(0, DialogChoiceEntries.ClearedValue);
    }

    [Fact]
    public void ClearingAndChoosingAreTheTwoEndsOfOneLatch() {
        Assert.NotEqual(DialogChoiceEntries.ClearedValue, DialogChoiceMenu.ValueWrittenForChoice());
    }

    [Fact]
    public void ItSharesTheActionIdEncodingWithEveryOtherDialogMenu() {
        Assert.Equal(KeywordMenu.ActionIdFor(0), DialogChoiceEntries.ActionIdFor(0));
        Assert.Equal(PartyMemberPicker.ActionIdFor(2), DialogChoiceEntries.ActionIdFor(2));
        Assert.Equal(2, DialogChoiceMenu.EntryIndexOf(DialogChoiceEntries.ActionIdFor(2)));
    }

    [Fact]
    public void LabelsComeFromTheSameOneBasedKeywordTableAsTheTopicGrid() {
        // Which is why a Yes/No menu's buttons are KEYWORD.DAT strings rather than branch data.
        Assert.Equal(KeywordMenu.LabelIndexFor(256), DialogChoiceEntries.LabelIndexFor(256));
        Assert.Equal(255, DialogChoiceEntries.LabelIndexFor(256));
    }

    [Fact]
    public void TheRowGeometryIsTheOneThePartyPickerUses() {
        // Two builders, one row layout — the same instructions in both.
        Assert.Equal(DialogButtonRow.ButtonWidth(30), PartyMemberPicker.ButtonWidth(30));
        Assert.Equal(DialogButtonRow.RowY(100, 10), PartyMemberPicker.RowY(100, 10));
        Assert.Equal(DialogButtonRow.ButtonHeight(10), PartyMemberPicker.ButtonHeight(10));
        Assert.Equal(DialogButtonRow.ButtonX(1, 200, 3, 40),
            PartyMemberPicker.ButtonX(1, 200, 3, 40));
    }
}
