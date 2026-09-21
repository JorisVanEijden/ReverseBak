namespace BetrayalAtKrondor.Tests.Inventory;

using GameData.Resources.Inventory;
using Xunit;

/// <summary>
/// <c>invui_animate_item_fly</c>'s geometry (INVENTOR.C:553-600).
/// </summary>
/// <remarks>
/// <b>The direction assertion is the point of this file.</b> The flight was implemented offset from
/// the START rather than the destination, which reverses it, and a live drop onto a portrait could
/// not tell the difference — dropping on the centre makes the two endpoints equal, so both forms
/// produce identical numbers. Every test here therefore uses a cursor that is NOT the portrait
/// centre.
/// </remarks>
public class ItemDropFlightTests {
    // Deliberately off-centre in both axes: an on-centre case passes against the reversed formula.
    private const double CursorX = 100;
    private const double CursorY = 200;
    private const double PortraitX = 500;
    private const double PortraitY = 800;
    private const double FullW = 60;
    private const double FullH = 40;

    private static (double Left, double Top) At(int step) =>
        ItemDropFlight.CornerAt(CursorX, CursorY, PortraitX, PortraitY, FullW, FullH, step);

    [Fact]
    public void TheFlightEndsOnThePortrait_NotOnTheCursor() {
        // i == 0: the moving term is gone and the size is zero, so the corner IS the portrait
        // centre. Reversed, this lands on the cursor instead.
        (double left, double top) = At(0);
        Assert.Equal(PortraitX, left, 6);
        Assert.Equal(PortraitY, top, 6);
    }

    [Fact]
    public void TheFirstDrawnStepIsNearTheCursor() {
        // i == 14 of 15: 14/15 of the way back toward the cursor, less half the scaled size.
        (double w, double h) = ItemDropFlight.SizeAt(FullW, FullH, ItemDropFlight.Steps - 1);
        (double left, double top) = At(ItemDropFlight.Steps - 1);
        Assert.Equal((CursorX - PortraitX) * 14 / 15 + PortraitX - w / 2, left, 6);
        Assert.Equal((CursorY - PortraitY) * 14 / 15 + PortraitY - h / 2, top, 6);
        // And it really is nearer the cursor than the portrait, which is the whole direction claim.
        Assert.True(System.Math.Abs(left - CursorX) < System.Math.Abs(left - PortraitX));
        Assert.True(System.Math.Abs(top - CursorY) < System.Math.Abs(top - PortraitY));
    }

    [Fact]
    public void ItMovesMonotonicallyTowardThePortrait() {
        double previous = double.NaN;
        for (int step = ItemDropFlight.Steps - 1; step >= 0; step--) {
            (double left, _) = At(step);
            double distance = System.Math.Abs(left - PortraitX);
            if (!double.IsNaN(previous)) {
                Assert.True(distance < previous,
                    $"step {step} moved away from the portrait: {distance} >= {previous}");
            }
            previous = distance;
        }
    }

    [Fact]
    public void TheIconShrinksToNothing() {
        (double w14, double h14) = ItemDropFlight.SizeAt(FullW, FullH, 14);
        Assert.Equal(FullW * 14 / 15, w14, 6);
        Assert.Equal(FullH * 14 / 15, h14, 6);
        (double w0, double h0) = ItemDropFlight.SizeAt(FullW, FullH, 0);
        Assert.Equal(0, w0);
        Assert.Equal(0, h0);
        // Never larger than the icon: the first step is already scaled down.
        Assert.True(w14 < FullW);
    }

    [Fact]
    public void TheStepCountIsAlsoTheScaleDivisor() {
        // Both are 0xf in the original. If someone raises Steps to slow the flight down (the one
        // parameter JvE is expected to change), the scale has to follow or the icon starts at the
        // wrong size.
        (double w, _) = ItemDropFlight.SizeAt(FullW, FullH, ItemDropFlight.Steps);
        Assert.Equal(FullW, w, 6);
    }
}
