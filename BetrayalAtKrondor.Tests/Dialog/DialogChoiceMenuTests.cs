namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// Picking something out of a dialog menu. The selection is a flag write, not a jump — and escape
/// presses a button rather than closing anything.
/// </summary>
public class DialogChoiceMenuTests {
    [Fact]
    public void EscapePressesTheLastButtonRatherThanClosingTheMenu() {
        // For a keyword grid that is the farewell, for the party row it is cancel — which is why
        // neither needs its own dismiss path.
        Assert.True(DialogChoiceMenu.DismissalPressesLastEntry(1));
        Assert.False(DialogChoiceMenu.DismissalPressesLastEntry(0));
    }

    [Fact]
    public void AnActionIdAboveTheBaseNamesAnEntry() {
        Assert.Equal(0, DialogChoiceMenu.EntryIndexOf(0x80));
        Assert.Equal(5, DialogChoiceMenu.EntryIndexOf(0x85));
    }

    [Fact]
    public void BelowTheBaseTheValueIsAKeystrokeNotAnEntry() {
        Assert.Equal(-1, DialogChoiceMenu.EntryIndexOf('S'));
        Assert.Equal(-1, DialogChoiceMenu.EntryIndexOf(1));
    }

    [Fact]
    public void TheEntryBaseIsTheOneBothMenusBuildAgainst() {
        Assert.Equal(KeywordMenu.FirstKeywordActionId, DialogChoiceMenu.EntryActionIdBase);
        Assert.Equal(0, DialogChoiceMenu.EntryIndexOf(KeywordMenu.ActionIdFor(0)));
        Assert.Equal(2, DialogChoiceMenu.EntryIndexOf(PartyMemberPicker.ActionIdFor(2)));
    }

    [Fact]
    public void AnAmbiguousLetterSelectsNothingAtAll() {
        // Two topics beginning with the same letter cannot be driven by the keyboard for either.
        // Taking the first match is the obvious "improvement" and changes what the keyboard does.
        Assert.True(DialogChoiceMenu.AcceleratorResolves(1));
        Assert.False(DialogChoiceMenu.AcceleratorResolves(2));
        Assert.False(DialogChoiceMenu.AcceleratorResolves(0));
    }

    [Fact]
    public void BranchRecordsAreTenBytesAfterANineByteHeader() {
        Assert.Equal(9, DialogChoiceMenu.BranchOffset(0));
        Assert.Equal(19, DialogChoiceMenu.BranchOffset(1));
        Assert.Equal(DialogChoiceMenu.BranchRecordSize,
            DialogChoiceMenu.BranchOffset(1) - DialogChoiceMenu.BranchOffset(0));
    }

    [Fact]
    public void ChoosingABranchLatchesAFlagRatherThanFollowingIt() {
        // The dialog's main branch loop does the navigating; the menu's whole effect is one write.
        Assert.Equal(1, DialogChoiceMenu.ValueWrittenForChoice());
    }

    [Fact]
    public void TheTwoMenuKindsReturnDifferentThingsFromTheSameFunction() {
        // 1 means "cancelled" for the picker and "entry 1" for a branch menu, so a caller cannot
        // read the result without knowing which kind it opened.
        Assert.Equal(1, DialogChoiceMenu.PartyPickerResult(cancelled: true));
        Assert.Equal(0, DialogChoiceMenu.PartyPickerResult(cancelled: false));
        Assert.Equal(1, DialogChoiceMenu.EntryIndexOf(DialogChoiceMenu.EntryActionIdBase + 1));
    }

    [Fact]
    public void AChosenMemberIsRecordedOneBased() {
        Assert.Equal(1, DialogChoiceMenu.TextVariableForMember(0));
        Assert.Equal(4, DialogChoiceMenu.TextVariableForMember(3));
    }
}
