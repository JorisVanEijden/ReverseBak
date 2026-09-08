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
    public void TheTravelFovIsTheHANDCALIBRATEDConstantItReplaces() {
        // 13.5 was measured against the original's arena grid to a 0.3% span agreement (TASK-201)
        // before anyone knew where it came from. Deriving it is what proves the two-shift model:
        // an arbitrary formula that happened to widen the map would not land back on this number.
        double travel = WorldProjection.VerticalFovDegrees(
            TravelViewHeightVga, WorldProjection.TravelProjectionShift);

        Assert.Equal(13.5, travel, 2);
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
        Assert.Equal(45.29, locator, 2);
        Assert.Equal(50.67, overhead, 2);
    }

    [Fact]
    public void AZeroHeightViewIsRejectedRatherThanReturningZeroDegrees() {
        // A camera with FOV 0 renders nothing and looks like a black screen, not like a bad
        // argument. The map screens read their height from data that can arrive unresolved.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => WorldProjection.VerticalFovDegrees(0, WorldProjection.MapProjectionShift));
    }
}
