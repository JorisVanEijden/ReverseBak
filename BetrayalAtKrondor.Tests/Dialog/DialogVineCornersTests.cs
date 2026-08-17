namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>The full-screen dialog's vine corners (<c>dialog_DrawChrome</c> @0x48864).</summary>
public class DialogVineCornersTests {
    [Fact]
    public void OnlyTheFullScreenStyleIsDecorated() {
        Assert.True(DialogVineCorners.DecoratesStyle((int)DialogType.PlainFullScreen));
        foreach (int other in new[] { 1, 2, 3, 4, 5 }) {
            Assert.False(DialogVineCorners.DecoratesStyle(other), "style " + other);
        }
    }

    [Fact]
    public void ThereAreTwoPiecesFromOneImage() =>
        // One sprite drawn twice — not two different corner assets.
        Assert.Equal(2, DialogVineCorners.Placements.Count);

    [Fact]
    public void TheSecondPieceIsRotated() {
        Assert.False(DialogVineCorners.Placements[0].Rotated);
        Assert.True(DialogVineCorners.Placements[1].Rotated,
            "bitmapFlags 3 is VerticalFlip|HorizontalFlip — a 180 degree turn, not a mirror");
    }

    [Fact]
    public void TheLowerPieceHangsOffTheLeftEdge() =>
        // VGA -4. The negative x is deliberate and is why these cannot live inside the panel:
        // the overhang would be clipped away.
        Assert.True(DialogVineCorners.Placements[0].X < 0);

    [Fact]
    public void ThePiecesSitAtOppositeCorners() {
        DialogVineCorners.Placement lower = DialogVineCorners.Placements[0];
        DialogVineCorners.Placement upper = DialogVineCorners.Placements[1];

        Assert.True(upper.X > lower.X, "the rotated piece is to the right");
        Assert.True(upper.Y < lower.Y, "and above");
    }

    [Fact]
    public void CoordinatesAreScaledFromVga() {
        // x5 across, x6 down — the canonical mapping, checked rather than copied.
        Assert.Equal(-4 * 5, DialogVineCorners.Placements[0].X);
        Assert.Equal(131 * 6, DialogVineCorners.Placements[0].Y);
        Assert.Equal(234 * 5, DialogVineCorners.Placements[1].X);
        Assert.Equal(3 * 6, DialogVineCorners.Placements[1].Y);
    }
}
