namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// The measurement the dialog button row is sized from, against the shipped GAME.FNT widths.
/// </summary>
/// <remarks>
/// <b>The widths live on the Unity side (<c>BakFontData</c>) and cannot be referenced from here</b>,
/// so this pins the arithmetic that consumes them using the same numbers. If the table ever moves
/// into GameData, this test should read it instead of restating it.
/// </remarks>
public class GameFontMeasurementTests {
    // GAME.FNT, firstChar 32: 'Y'=5, 'e'=5, 's'=5 / 'N'=5, 'o'=5 — from BakFontData.GameWidths.
    private const int YesWidth = 5 + 5 + 5;
    private const int NoWidth = 5 + 5;

    [Fact]
    public void AYesNoRowIsSizedFromYES_TheWiderOfTheTwo() {
        // One width for the whole row, and it is the WIDEST label that sets it — so "No" gets a
        // button as wide as "Yes" rather than a narrower one.
        int widest = YesWidth > NoWidth ? YesWidth : NoWidth;
        Assert.Equal(YesWidth, widest);
        Assert.Equal(YesWidth + DialogButtonRow.LabelPadding, DialogButtonRow.ButtonWidth(widest));
    }

    [Fact]
    public void ATwoButtonRowLandsOnTheThirdsOfTheDialogPanel() {
        // The original's 0x9c-wide dialog panel, divided into three parts with a button centred on
        // each of the two inner divisions.
        const int panelWidth = 0x9c;   // 156
        int width = DialogButtonRow.ButtonWidth(YesWidth);

        int yes = DialogButtonRow.ButtonX(0, panelWidth, 2, width);
        int no = DialogButtonRow.ButtonX(1, panelWidth, 2, width);

        Assert.Equal(52 + DialogButtonRow.RowInset - (width / 2), yes);
        Assert.Equal(104 + DialogButtonRow.RowInset - (width / 2), no);
        Assert.True(no > yes + width, "the buttons do not overlap");
    }

    [Fact]
    public void TheRowSitsWithinThePanelItIsAnchoredTo() {
        // A sanity bound rather than a recovered fact: a row whose top plus height ran past the
        // panel's bottom would be laid out under the frame, which is the visible failure this
        // whole change risks.
        const int panelHeight = 60;
        const int fontHeight = 10;

        int top = DialogButtonRow.RowY(panelHeight, fontHeight);
        int bottom = top + DialogButtonRow.ButtonHeight(fontHeight);

        Assert.True(top > 0, "the row starts inside the panel");
        Assert.True(bottom <= panelHeight, "and ends inside it");
    }
}
