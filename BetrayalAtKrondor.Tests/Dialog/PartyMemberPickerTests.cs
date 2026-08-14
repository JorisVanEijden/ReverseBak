namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// The party-member button row. Named after keywords, related to none of them.
/// </summary>
public class PartyMemberPickerTests {
    [Fact]
    public void ThereIsOneButtonPerMemberPlusCancel() {
        Assert.Equal(4, PartyMemberPicker.ButtonCount(3));
        Assert.Equal(3, PartyMemberPicker.CancelIndex(3));
    }

    [Fact]
    public void CancelKeepsTheActionIdItsSlotWouldHaveHad() {
        // Not special-cased — a caller expecting a distinct cancel id reads it as a fourth member.
        Assert.Equal(PartyMemberPicker.ActionIdFor(3), PartyMemberPicker.FirstActionId + 3);
        Assert.Equal(PartyMemberPicker.ActionIdFor(PartyMemberPicker.CancelIndex(3)),
            PartyMemberPicker.FirstActionId + 3);
    }

    [Fact]
    public void ItSharesItsActionIdBaseWithTheKeywordGrid() {
        // Which is why an id alone does not say which menu produced it.
        Assert.Equal(KeywordMenu.FirstKeywordActionId, PartyMemberPicker.FirstActionId);
        Assert.Equal(KeywordMenu.ActionIdFor(0), PartyMemberPicker.ActionIdFor(0));
    }

    [Fact]
    public void EveryButtonSharesOneWidthTakenFromTheWidestLabel() {
        Assert.Equal(42, PartyMemberPicker.ButtonWidth(32));
    }

    [Fact]
    public void TheRowIsSpreadEvenlyWithAGapAtEachEnd() {
        // Divided into count + 1 parts with each button centred on a division, not packed or
        // edge-aligned.
        const int panelWidth = 200;
        const int count = 4;
        const int width = 40;

        int first = PartyMemberPicker.ButtonX(0, panelWidth, count, width);
        int second = PartyMemberPicker.ButtonX(1, panelWidth, count, width);
        int last = PartyMemberPicker.ButtonX(count - 1, panelWidth, count, width);

        Assert.True(first > 0, "there is a gap before the first button");
        Assert.Equal(second - first, PartyMemberPicker.ButtonX(2, panelWidth, count, width) - second);
        Assert.True(last + width < panelWidth, "and one after the last");
    }

    [Fact]
    public void TheRowIsAnchoredToTheBottomOfThePanel() {
        // Measured up from the bottom edge, so a taller panel pushes it down rather than stretching.
        Assert.Equal(100 - (10 + PartyMemberPicker.BottomMargin),
            PartyMemberPicker.RowY(100, 10));
        Assert.True(PartyMemberPicker.RowY(200, 10) > PartyMemberPicker.RowY(100, 10));
    }

    [Fact]
    public void ButtonHeightFollowsTheFont() {
        Assert.Equal(14, PartyMemberPicker.ButtonHeight(10));
        Assert.True(PartyMemberPicker.ButtonHeight(10) < PartyMemberPicker.ButtonHeight(12));
    }

    [Fact]
    public void TheRowFitsInsideThePanelItIsGiven() {
        const int panelWidth = 320;
        const int count = 4;
        int width = PartyMemberPicker.ButtonWidth(40);

        for (var i = 0; i < count; i++) {
            int x = PartyMemberPicker.ButtonX(i, panelWidth, count, width);

            Assert.InRange(x, 0, panelWidth - width);
        }
    }

    [Fact]
    public void CancelIsALiteralAndNotAPartyMember() {
        Assert.Equal("Cancel", PartyMemberPicker.CancelLabel);
    }
}
