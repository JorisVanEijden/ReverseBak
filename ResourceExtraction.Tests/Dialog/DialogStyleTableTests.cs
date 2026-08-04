namespace ResourceExtraction.Tests.Dialog;

using System.Text.Json;
using GameData.Resources.Dialog;
using GameData.Resources.Layout;
using Xunit;

public class DialogStyleTableTests {
    /// <summary>
    /// Every defined row's shipped rect, canonical (VGA x5 horizontal / x6 vertical). Held as
    /// one table so a transcription slip in any row surfaces as its own failing case instead of
    /// hiding in a row nobody happened to assert.
    /// </summary>
    public static TheoryData<int, int, int, int, int> ShippedRows => new() {
        //  row   L    T     W     H         VGA source        x5/x6
        { 1, 40, 720, 1525, 450 },  // (8, 120, 305, 75)
        { 2, 65, 66, 1470, 606 },   // (13, 11, 294, 101)
        { 3, 40, 708, 1525, 438 },  // (8, 118, 305, 73)
        { 4, 40, 720, 1525, 450 },  // (8, 120, 305, 75)
        { 5, 65, 66, 1470, 726 },   // (13, 11, 294, 121)
        { 6, 125, 126, 1350, 960 }, // (25, 21, 270, 160)
    };

    [Theory]
    [MemberData(nameof(ShippedRows))]
    public void DefaultArea_IsTheShippedCanonicalRect_InDesignFramePx(
        int row, int left, int top, int width, int height) {
        LayoutHint area = DialogStyleTable.Get(row).DefaultArea;

        // Unit-bearing assertions throughout: a bare number would pass just as happily against a
        // percentage of the same magnitude, and "right number, wrong unit" is the defect class
        // this project has already shipped more than once.
        Assert.Equal(LayoutLength.Px(left), area.Left);
        Assert.Equal(LayoutLength.Px(top), area.Top);
        Assert.Equal(LayoutLength.Px(width), area.Width);
        Assert.Equal(LayoutLength.Px(height), area.Height);

        // Absolute + TopLeft is what makes those insets mean "measured from the design frame's
        // top-left corner". Drop either and the same four numbers place the panel somewhere else
        // entirely, so they are as load-bearing as the numbers themselves.
        Assert.Equal(LayoutPosition.Absolute, area.Position);
        Assert.Equal(LayoutAnchor.TopLeft, area.Anchor);

        // The shipped rect is fully determined by Left/Top/Width/Height; the far edges must carry
        // no opinion, or the panel would be stretched between two pinned edges AND an explicit
        // size.
        Assert.Equal(LayoutLength.Auto, area.Right);
        Assert.Equal(LayoutLength.Auto, area.Bottom);
    }

    /// <summary>
    /// Rows 2 and 5 are the same chrome at two heights — the in-game variant (5) is simply taller
    /// (VGA 121 vs 101). Pinning "identical in everything but Height" is cheap and catches the
    /// transcription slip the per-row table above cannot: a copy-paste that moved the wrong field
    /// between the two rows would still satisfy both rows' own expected numbers only if those
    /// numbers were also edited, but a slip in one direction (e.g. row 5 inheriting row 2's Top
    /// from a mis-typed literal) shows up here as a relationship break.
    /// </summary>
    [Fact]
    public void Row5_DiffersFromRow2_InHeightAlone() {
        DialogStyle row2 = DialogStyleTable.Get(2);
        DialogStyle row5 = DialogStyleTable.Get(5);

        Assert.Equal(row2.DefaultArea.Left, row5.DefaultArea.Left);
        Assert.Equal(row2.DefaultArea.Top, row5.DefaultArea.Top);
        Assert.Equal(row2.DefaultArea.Width, row5.DefaultArea.Width);
        Assert.NotEqual(row2.DefaultArea.Height, row5.DefaultArea.Height);
        Assert.Equal(LayoutLength.Px(606f), row2.DefaultArea.Height); // VGA 101 x6
        Assert.Equal(LayoutLength.Px(726f), row5.DefaultArea.Height); // VGA 121 x6

        // All five pens and both text pads are byte-identical between the two rows.
        Assert.Equal(row2.FillPenColor, row5.FillPenColor);
        Assert.Equal(row2.BorderPenColor, row5.BorderPenColor);
        Assert.Equal(row2.ShadowPenColor, row5.ShadowPenColor);
        Assert.Equal(row2.BodyTextPenColor, row5.BodyTextPenColor);
        Assert.Equal(row2.TextShadowPenSource, row5.TextShadowPenSource);
        Assert.Equal(row2.TextPadLeftPct, row5.TextPadLeftPct);
        Assert.Equal(row2.TextPadRightPct, row5.TextPadRightPct);
    }

    /// <summary>
    /// The override path (Task 3) will hand <see cref="DialogStyle"/> to a serializer, and a
    /// percentage-valued area is the whole reason <see cref="LayoutHint"/> replaced the old
    /// int rect — so the percentage case is what gets round-tripped, not the px one the table
    /// already covers.
    /// </summary>
    [Fact]
    public void DialogStyle_RoundTripsThroughSystemTextJson_WithAPercentageDefaultArea() {
        // Deliberately asymmetric, non-round, and no two values sharing a number: an
        // implementation that transposed two fields, or dropped one and reused its neighbour,
        // cannot reproduce this set by accident.
        var original = new DialogStyle {
            FillPenColor = 0x03,
            BorderPenColor = 0x09,
            ShadowPenColor = 0x0E,
            BodyTextPenColor = 0x06,
            TextShadowPenSource = 0x0B,
            DefaultArea = new LayoutHint {
                Anchor = LayoutAnchor.Center,
                Left = LayoutLength.Percent(7.5f),
                Top = LayoutLength.Percent(11.25f),
                Width = LayoutLength.Percent(63.75f),
                Height = LayoutLength.Percent(29.5f),
            },
            TextPadLeftPct = 4.75f,
            TextPadRightPct = 8.125f,
        };

        DialogStyle restored =
            JsonSerializer.Deserialize<DialogStyle>(JsonSerializer.Serialize(original))!;

        Assert.Equal(0x03, restored.FillPenColor);
        Assert.Equal(0x09, restored.BorderPenColor);
        Assert.Equal(0x0E, restored.ShadowPenColor);
        Assert.Equal(0x06, restored.BodyTextPenColor);
        Assert.Equal(0x0B, restored.TextShadowPenSource);
        Assert.Equal(LayoutAnchor.Center, restored.DefaultArea.Anchor);
        Assert.Equal(LayoutLength.Percent(7.5f), restored.DefaultArea.Left);
        Assert.Equal(LayoutLength.Percent(11.25f), restored.DefaultArea.Top);
        Assert.Equal(LayoutLength.Percent(63.75f), restored.DefaultArea.Width);
        Assert.Equal(LayoutLength.Percent(29.5f), restored.DefaultArea.Height);
        Assert.Equal(4.75f, restored.TextPadLeftPct);
        Assert.Equal(8.125f, restored.TextPadRightPct);
    }

    /// <summary>
    /// A settable-property class deserializes field-by-field, so an override document that names
    /// only some fields is legal and the rest land on the type's own defaults — no constructor
    /// has to be matched, which is the property Task 3's override path depends on.
    /// </summary>
    [Fact]
    public void DialogStyle_DeserializesFromAPartialDocument_LeavingUnnamedFieldsAtTheirDefaults() {
        const string json = "{\"DefaultArea\":{\"Left\":\"7.5%\",\"Height\":\"29.5%\"}}";

        DialogStyle style = JsonSerializer.Deserialize<DialogStyle>(json)!;

        Assert.Equal(LayoutLength.Percent(7.5f), style.DefaultArea.Left);
        Assert.Equal(LayoutLength.Percent(29.5f), style.DefaultArea.Height);
        // Unnamed area edges keep LayoutHint's own Auto default rather than collapsing to zero px.
        Assert.Equal(LayoutLength.Auto, style.DefaultArea.Top);
        Assert.Equal(LayoutLength.Auto, style.DefaultArea.Width);
    }

    /// <summary>
    /// <see cref="DialogStyle"/> is a class now, so <see cref="DialogStyleTable.Get"/> hands back
    /// a reference into the shared table rather than the defensive copy the old record struct
    /// gave every caller. This test states that as a fact of the API (it is what Task 3's
    /// resource identity will hang an override off) so a future change that starts cloning is a
    /// deliberate decision rather than a silent one.
    /// </summary>
    [Fact]
    public void Get_ReturnsTheSharedTableRow_NotACopy() {
        Assert.Same(DialogStyleTable.Get(2), DialogStyleTable.Get(2));
    }
}
