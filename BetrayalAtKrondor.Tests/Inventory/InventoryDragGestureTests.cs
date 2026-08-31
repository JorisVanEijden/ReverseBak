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
    public void TheOutlineIsAPENThatCYCLES_notAWidth() {
        // bGfx_outline_color is a pen and the screen phases it. A static width answers a question
        // the original does not ask, and cannot show the pulse that says which slot is live.
        Assert.Equal(1, InventoryDragGesture.OutlineWidthVga);
        Assert.Equal(7, InventoryDragGesture.OutlinePulsePens.Length);
        Assert.Equal(InventoryDragGesture.OutlinePulsePens[0],
            InventoryDragGesture.OutlinePulsePens[^1]);

        // Out and back: it rises to a peak and returns, so the pulse has no seam.
        var peak = 0;
        for (var i = 1; i < InventoryDragGesture.OutlinePulsePens.Length; i++) {
            if (InventoryDragGesture.OutlinePulsePens[i] > InventoryDragGesture.OutlinePulsePens[peak]) {
                peak = i;
            }
        }
        for (var i = 1; i <= peak; i++) {
            Assert.True(InventoryDragGesture.OutlinePulsePens[i]
                > InventoryDragGesture.OutlinePulsePens[i - 1]);
        }
        for (int i = peak + 1; i < InventoryDragGesture.OutlinePulsePens.Length; i++) {
            Assert.True(InventoryDragGesture.OutlinePulsePens[i]
                < InventoryDragGesture.OutlinePulsePens[i - 1]);
        }
    }

    [Fact]
    public void TheGhostIsNotSizedByUs() {
        // invui_cur_spr_paint_ctrd(sprite, 0) — the icon, no scale argument, centred on the cursor.
        Assert.True(InventoryDragGesture.GhostIsTheIconAtNaturalSizeCentredOnTheCursor);
    }
}
