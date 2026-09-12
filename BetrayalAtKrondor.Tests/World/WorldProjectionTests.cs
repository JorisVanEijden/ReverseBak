namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.Spells;
using GameData.Resources.World;
using System;
using Xunit;

/// <summary>
/// The travel view and the map views do NOT share a projection — TASK-374.
/// </summary>
/// <remarks>
/// The original keeps two <c>ViewContext</c>s whose zoom differs by two bits: START.DAT's 9 for
/// travel and combat, <c>VIEW_ZOOM_DEFAULT</c> 7 for the overhead map and the locator inset. The
/// port used the travel FOV everywhere, so the map showed a quarter of the ground and the locator
/// dropped every marker past a quarter of its range.
/// </remarks>
public class WorldProjectionTests {
    /// <summary>The travel view's VGA rectangle is 294x101 (START.DAT, canonical 1470x606).</summary>
    private const int TravelViewHeightVga = 101;

    [Fact]
    public void TheTravelFovIsTheTRUEVerticalHalfAngle_NotTheOneThatMatchesHorizontally() {
        // *** 13.50 WAS THE HORIZONTAL-MATCHING VALUE, AND IT IS WRONG FOR THE VERTICAL. ***
        // It folded VGA's 6:5 pixel aspect into the FOV, which lands the horizontal exactly right
        // and leaves the vertical short by that same 1.2. Measured 2026-09-12 against the original's
        // own tactical grid with both games in one fight: all 13 rows agreed on width to within
        // 0.5%, and the vertical was compressed by 0.85 about the viewport centre (TASK-439).
        //
        // The horizontal is restored by CameraAspect, NOT here.
        double travel = WorldProjection.VerticalFovDegrees(
            TravelViewHeightVga, WorldProjection.TravelProjectionShift);

        Assert.Equal(11.27, travel, 2);

        // And the old number is exactly 1.2x this one in tangent — which is what says the change is
        // the pixel aspect and not a recalibration.
        Assert.Equal(13.50,
            2.0 * Math.Atan(WorldProjection.CanonicalPixelAspect
                * Math.Tan(travel * Math.PI / 360.0)) * 180.0 / Math.PI, 2);
    }

    [Fact]
    public void TheCameraAspectCarriesTheSixToFive() {
        // The travel viewport is canonical 1470x606 — VGA 294x101 — so the answer is that VGA
        // rect's own w/h, and the camera is 1.2x wider than its pixels are.
        Assert.Equal(294.0 / 101.0, WorldProjection.CameraAspect(1470, 606), 6);
        Assert.Equal(WorldProjection.CanonicalPixelAspect * 1470.0 / 606.0,
            WorldProjection.CameraAspect(1470, 606), 9);

        // Device pixels work as well as canonical ones: only the ratio matters, which is what lets
        // WorldViewportView pass the RenderTexture's size straight in. They are not IDENTICAL --
        // a RenderTexture is whole pixels, so 1470x606 scaled to a 1080-tall window rounds to
        // 1323x545 and the aspect lands 0.07% out. Assert the tolerance rather than a decimal
        // count, so the number the engine actually produces is the one under test.
        double canonical = WorldProjection.CameraAspect(1470, 606);
        double device = WorldProjection.CameraAspect(1323, 545);
        Assert.True(System.Math.Abs(device - canonical) / canonical < 0.005,
            $"canonical {canonical:F5} vs device {device:F5}");
    }

    [Fact]
    public void TheMapViewsSeeFOURTIMESTheGroundAtTheSameHeight() {
        // Two bits of shift is a factor of four in the projected scale, and the ground extent a
        // top-down camera covers is height * tan(halfFov), so the ratio is the tangent ratio.
        double travel = WorldProjection.VerticalFovDegrees(
            TravelViewHeightVga, WorldProjection.TravelProjectionShift);
        double map = WorldProjection.VerticalFovDegrees(
            TravelViewHeightVga, WorldProjection.MapProjectionShift);

        double ratio = Math.Tan(map * Math.PI / 360.0) / Math.Tan(travel * Math.PI / 360.0);

        Assert.Equal(4.0, ratio, 6);
    }

    [Fact]
    public void TheLocatorInsetIsSHORTERThanTheTravelViewSoItsFovIsNarrower() {
        // The inset is its own rectangle, not the travel one — 167x89 against 294x101. Sharing the
        // map shift is not the same as sharing the map FOV, and using the overhead map's value here
        // would over-reach the inset by the height ratio.
        (int _, int _, int _, int height) = FieldSpells.LocatorViewport;
        double locator = WorldProjection.VerticalFovDegrees(height, WorldProjection.MapProjectionShift);
        double overhead = WorldProjection.VerticalFovDegrees(
            TravelViewHeightVga, WorldProjection.MapProjectionShift);

        Assert.Equal(89, height);
        Assert.True(locator < overhead, $"locator {locator:F2} should be narrower than map {overhead:F2}");
        Assert.Equal(38.34, locator, 2);
        Assert.Equal(43.06, overhead, 2);
    }

    [Fact]
    public void AZeroHeightViewIsRejectedRatherThanReturningZeroDegrees() {
        // A camera with FOV 0 renders nothing and looks like a black screen, not like a bad
        // argument. The map screens read their height from data that can arrive unresolved.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => WorldProjection.VerticalFovDegrees(0, WorldProjection.MapProjectionShift));
    }
}
