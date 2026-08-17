namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// The vertical text insets (<c>field_7</c>/<c>field_8</c>), which went unported for a long time
/// while a tuned constant stood in for them.
/// </summary>
public class DialogStyleTableInsetTests {
    private static DialogStyle Row(int id) => DialogStyleTable.CreateShipped().Get(id);

    [Fact]
    public void TheFullScreenRowsInsetIsAHairline() {
        // field_7 = field_8 = 1 VGA px. The tuned constant that stood in for it was 180 — thirty
        // times too large — which pushed a long narrative out of the bottom of its own panel.
        Assert.Equal(6f, Row(6).TextPadTop);
        Assert.Equal(6f, Row(6).TextPadBottom);
    }

    [Fact]
    public void TheNarrativeStripsInsetIsFiveVgaPixels() {
        Assert.Equal(30f, Row(3).TextPadTop);
        Assert.Equal(12f, Row(3).TextPadBottom);
    }

    [Fact]
    public void TheBorderedBoxesAreSymmetric() {
        // field_7 = field_8 = 3 for the bordered rows, unlike the strips' asymmetric 5/2.
        Assert.Equal(Row(2).TextPadTop, Row(2).TextPadBottom);
        Assert.Equal(18f, Row(2).TextPadTop);
    }

    [Fact]
    public void EveryReachableRowHasAnInset() {
        // Row 0 is unused padding; 1..6 are all reachable and all carry a non-zero top inset, so a
        // zero would mean the row was missed when the fields were ported.
        for (var id = 1; id <= 6; id++) {
            Assert.True(Row(id).TextPadTop > 0f, "row " + id + " has no top inset");
        }
    }

    [Fact]
    public void TheFullScreenBodyFitsItsOwnPanel() {
        // The regression this came from: 14 lines of game text at the shipped line pitch inside
        // row 6's 960px-tall area. With the real inset there is room; with the old 180 there was
        // not, and the overflow left the screen entirely.
        DialogStyle row = Row(6);
        float usable = row.DefaultArea.Height.Value - row.TextPadTop - row.TextPadBottom;

        Assert.True(usable >= 924f,
            "row 6 must leave room for a full-height narrative; usable was " + usable);
    }
}
