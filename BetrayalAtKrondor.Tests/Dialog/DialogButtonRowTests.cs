namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// The row of buttons along a dialog panel's bottom —
/// <c>CreateMenuEntriesFromDialogData</c> (@0x4b1e7).
/// </summary>
public class DialogButtonRowTests {
    [Fact]
    public void EveryButtonInTheRowGetsTheSameWidth() {
        // One width for the whole row, from the WIDEST label — the builder keeps a running maximum
        // (0x4b2cf) and gives every button that width, so buttons are uniform rather than fitted.
        Assert.Equal(40 + DialogButtonRow.LabelPadding, DialogButtonRow.ButtonWidth(40));
    }

    [Fact]
    public void TheRowIsSpreadByDividingIntoOneMorePartThanThereAreButtons() {
        // Evenly spaced with a gap at each end, rather than packed or edge-aligned: each button is
        // centred on a division of panelWidth / (count + 1).
        const int panel = 300;
        const int width = 50;
        int a = DialogButtonRow.ButtonX(0, panel, 2, width);
        int b = DialogButtonRow.ButtonX(1, panel, 2, width);

        Assert.Equal(100 + DialogButtonRow.RowInset - 25, a);
        Assert.Equal(200 + DialogButtonRow.RowInset - 25, b);
        // The two gaps at the ends match each other, which is what "a gap at each end" means.
        Assert.Equal(a - DialogButtonRow.RowInset, panel - (b + width) + DialogButtonRow.RowInset);
    }

    [Fact]
    public void TheRowIsAnchoredToThePanelsBottomEdge() {
        // Measured UP from the bottom, so a taller panel moves the row down with it rather than
        // leaving it stranded under the text.
        Assert.Equal(100 - (10 + DialogButtonRow.BottomMargin), DialogButtonRow.RowY(100, 10));
        Assert.Equal(200 - (10 + DialogButtonRow.BottomMargin), DialogButtonRow.RowY(200, 10));
    }

    [Fact]
    public void CanonicalSpaceScalesTheRESULT_NotTheInputs() {
        // *** NOT INTERCHANGEABLE. *** The spread is an integer division by count+1, so dividing a
        // canonical width instead spreads the remainder differently and walks each button off the
        // original's position. 301/3 = 100 (then x5 = 500), while 1505/3 = 501.
        const int panel = 301;
        (int x, _, _, _) = DialogButtonRow.ButtonRect(0, panel, 100, 2, 40, 10);

        int faithful = DialogButtonRow.ButtonX(0, panel, 2, DialogButtonRow.ButtonWidth(40))
            * DialogButtonRow.CanonicalScaleX;
        Assert.Equal(faithful, x);

        int scaledInputsFirst =
            DialogButtonRow.ButtonX(0, panel * DialogButtonRow.CanonicalScaleX, 2,
                DialogButtonRow.ButtonWidth(40) * DialogButtonRow.CanonicalScaleX);
        Assert.NotEqual(scaledInputsFirst, x);
    }

    [Fact]
    public void TheCanonicalBoxUsesTheVerticalScaleForItsVerticalHalf() {
        // x5 across and x6 down — the aspect the original's pixels are NOT square in. One scale for
        // both would squash the row.
        (_, int y, int w, int h) = DialogButtonRow.ButtonRect(0, 300, 100, 2, 40, 10);

        Assert.Equal(DialogButtonRow.RowY(100, 10) * DialogButtonRow.CanonicalScaleY, y);
        Assert.Equal(DialogButtonRow.ButtonWidth(40) * DialogButtonRow.CanonicalScaleX, w);
        Assert.Equal(DialogButtonRow.ButtonHeight(10) * DialogButtonRow.CanonicalScaleY, h);
        Assert.NotEqual(DialogButtonRow.CanonicalScaleX, DialogButtonRow.CanonicalScaleY);
    }

    [Fact]
    public void ASingleButtonSitsCentred() {
        // The OK case: one button, two divisions, so it lands on the middle one.
        const int panel = 300;
        int width = DialogButtonRow.ButtonWidth(40);
        Assert.Equal(150 + DialogButtonRow.RowInset - (width / 2),
            DialogButtonRow.ButtonX(0, panel, 1, width));
    }
}
