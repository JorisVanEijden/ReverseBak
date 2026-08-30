namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// The "which of you?" row shares the choice row's geometry and differs only in its labels —
/// <c>ProcessKeywordSelection</c> (@0x4b3ab) against <c>CreateMenuEntriesFromDialogData</c>.
/// </summary>
public class PartyPickerRowTests {
    [Fact]
    public void ThereIsAlwaysOneMoreButtonThanThereArePartyMembers() {
        Assert.Equal(4, PartyMemberPicker.ButtonCount(3));
        Assert.Equal(3, PartyMemberPicker.CancelIndex(3));
    }

    [Fact]
    public void AFullPartyRowIsLaidOutByTheSameArithmeticAsAChoiceRow() {
        // The original computes both rows with the same instructions; only the contents differ. A
        // four-button row divides the panel into five, so the members and Cancel are evenly spread.
        const int panel = 0x9c;
        const int fontHeight = 10;
        int width = DialogButtonRow.ButtonWidth(30);
        int buttons = PartyMemberPicker.ButtonCount(3);

        var xs = new int[buttons];
        for (var i = 0; i < buttons; i++) {
            xs[i] = DialogButtonRow.ButtonX(i, panel, buttons, width);
        }

        int step = xs[1] - xs[0];
        for (var i = 2; i < buttons; i++) {
            Assert.Equal(step, xs[i] - xs[i - 1]);
        }
        // And the row still sits inside the panel it is anchored to.
        Assert.True(DialogButtonRow.RowY(60, fontHeight) + DialogButtonRow.ButtonHeight(fontHeight)
            <= 60);
    }

    [Fact]
    public void CancelIsNotGivenAnIdOfItsOwn() {
        // *** It keeps the action id its SLOT would have had. *** A caller that expects a distinct
        // cancel code reads it as one more party member, which is the trap this pins.
        Assert.Equal(PartyMemberPicker.FirstActionId + 3, PartyMemberPicker.ActionIdFor(3));
        Assert.Equal(PartyMemberPicker.ActionIdFor(PartyMemberPicker.CancelIndex(3)),
            PartyMemberPicker.FirstActionId + 3);
    }

    [Fact]
    public void ThePickerSharesTheKeywordGridsActionIdBase() {
        // Deliberately the same 128: an action id alone does not say which menu produced it, so a
        // caller has to know which one is open. Two independent constants would hide that.
        Assert.Equal(KeywordMenu.FirstKeywordActionId, PartyMemberPicker.FirstActionId);
    }
}
