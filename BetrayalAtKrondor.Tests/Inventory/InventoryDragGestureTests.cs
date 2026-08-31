namespace BetrayalAtKrondor.Tests.Inventory;

using GameData.Resources.Inventory;
using Xunit;

/// <summary>
/// The inventory drag gesture — <c>invui_handle_item_drag</c>.
/// </summary>
/// <remarks>
/// <b>These constants were on TASK-30's "cosmetic tuning, wants a side-by-side capture" list.</b>
/// The routine states every one of them, and the port differs from it in KIND rather than in degree
/// — a circle where the original has a diamond, a dimmed cell where it empties one, a border width
/// where it has a cycling pen.
/// </remarks>
public class InventoryDragGestureTests {
    // The canonical frame's scales, restated here so the test converts the way a caller must.
    private const float VgaScaleX = 5f;
    private const float VgaScaleY = 6f;

    private static bool FromCanonical(float dx, float dy) =>
        InventoryDragGesture.StartsDrag(dx / VgaScaleX, dy / VgaScaleY);

    [Fact]
    public void TheThresholdIsSTRICT() {
        // `if (dist > 4)` — exactly four does not drag.
        Assert.False(InventoryDragGesture.StartsDrag(4f, 0f));
        Assert.True(InventoryDragGesture.StartsDrag(4.5f, 0f));
        Assert.False(InventoryDragGesture.StartsDrag(0f, 0f));
    }

    [Fact]
    public void TheMetricIsMANHATTAN_soADiagonalDragsSoonerThanACircleWould() {
        // *** The shape of the boundary, and the half a Euclidean threshold gets wrong. ***
        // (3,3) has a Manhattan distance of 6 and a Euclidean one of 4.24. The original drags;
        // a radius-4 circle would not.
        Assert.True(InventoryDragGesture.StartsDrag(3f, 3f));
        Assert.True(System.Math.Sqrt(18) < InventoryDragGesture.ThresholdVga + 0.25);
    }

    [Fact]
    public void TWENTYCanonicalIsCorrectAcrossAndAFifthTooTightDown() {
        // *** Why a single canonical scalar cannot be right. *** 4 VGA px is 20 canonical px across
        // and 24 down, so the 20f the port carried — described in its own comment as "the original's
        // own threshold" — starts a vertical drag a fifth early.
        Assert.False(FromCanonical(20f, 0f));
        Assert.True(FromCanonical(20.5f, 0f), "the horizontal vertex sits exactly at 20");

        Assert.False(FromCanonical(0f, 24f));
        Assert.True(FromCanonical(0f, 24.5f), "but the vertical one sits at 24");

        // The value the port used, moved vertically: over the old scalar, under the real threshold.
        Assert.False(FromCanonical(0f, 21f));
    }

    [Fact]
    public void TheOriginCellIsEMPTIED_exceptInAShop() {
        // focused->wSprite_base = 0 under `if (!dragging && !is_shop)`. Our 0.25 opacity leaves a
        // ghost behind, so the item appears twice; the original shows an empty slot.
        Assert.True(InventoryDragGesture.EmptiesTheOriginCell(isShop: false));
        Assert.False(InventoryDragGesture.EmptiesTheOriginCell(isShop: true));
    }

    [Fact]
    public void TheCellOutlineIsONEPixel_andThereforeNotOneWidth() {
        // The outline is a PEN, so its width is the blitter's single pixel. And one VGA pixel is 5
        // canonical across but 6 down, so the border is genuinely thicker top-and-bottom — there is
        // no single number, which is why an 8f could not have been right whatever value it took.
        Assert.Equal(1, InventoryDragGesture.OutlineWidthVga);
        Assert.NotEqual(5 * InventoryDragGesture.OutlineWidthVga,
            6 * InventoryDragGesture.OutlineWidthVga);
    }

    [Fact]
    public void THECELLDoesNotPulse_butTHEPORTRAITDoes() {
        // *** A correction to my own first reading, pinned so it cannot come back. *** The cycling
        // pens are real but belong to invui_portr_panel_fill_pulsing / invui_portrait_panel_draw.
        // The item cell's highlight uses a CONSTANT outline pen over a constant fill.
        Assert.Equal(0x8b, InventoryDragGesture.SelectedCellOutlinePen);
        Assert.Equal(0x8f, InventoryDragGesture.SelectedCellFillPen);
        Assert.DoesNotContain(InventoryDragGesture.SelectedCellOutlinePen,
            InventoryDragGesture.PortraitPulsePens);
    }

    [Fact]
    public void ThePortraitPulseRunsOutAndBackWithNoSeam() {
        // phase % 6 over (m > 3) ? ('q' - m) : (m + 'k'): 0x6b..0x6e then back down.
        int[] pens = InventoryDragGesture.PortraitPulsePens;
        Assert.Equal(6, pens.Length);
        Assert.Equal(0x6b, pens[0]);
        Assert.Equal(0x6e, pens[3]);
        // Wrapping from the last back to the first is a single step, like every other.
        Assert.Equal(1, System.Math.Abs(pens[^1] - pens[0]));
    }

    [Fact]
    public void TheGhostIsNotSizedByUs() {
        // invui_cur_spr_paint_ctrd(sprite, 0) — the icon, no scale argument, centred on the cursor.
        Assert.True(InventoryDragGesture.GhostIsTheIconAtNaturalSizeCentredOnTheCursor);
    }
}
