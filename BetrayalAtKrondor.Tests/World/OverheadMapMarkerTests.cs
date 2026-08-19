namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using Xunit;

/// <summary>
/// The party marker on the overhead map — the tail of <c>drawMap</c> (0x21711).
/// </summary>
public class OverheadMapMarkerTests {
    [Fact]
    public void TheDirectionRoundsToTheNEARESTOfSixteen() {
        // (yaw + 0x800) >> 0xC. Without the half-step the marker would sit up to a whole direction
        // behind the party at every heading between two icons.
        Assert.Equal(0, OverheadMapMarker.DirectionFor(0));
        Assert.Equal(0, OverheadMapMarker.DirectionFor(0x07ff));
        Assert.Equal(1, OverheadMapMarker.DirectionFor(0x0800));
        Assert.Equal(1, OverheadMapMarker.DirectionFor(0x1000));
        Assert.Equal(8, OverheadMapMarker.DirectionFor(0x8000));
    }

    [Fact]
    public void ItWrapsRatherThanRunningOffTheEndOfTheSheet() {
        // A heading just short of a full turn rounds up to 16, which is direction 0 again — an
        // unmasked shift would index one past the last icon.
        Assert.Equal(0, OverheadMapMarker.DirectionFor(0xffff));
        Assert.InRange(OverheadMapMarker.DirectionFor(0xf801), 0, OverheadMapMarker.Directions - 1);
    }

    [Fact]
    public void THEHEADINGLivesInEitherTheIconOrTheCameraAndNeverBoth() {
        // North-up renders the world at yaw 0 and points the icon; a turning map renders at the
        // party's yaw and the icon is fixed. Doing both would turn the party twice.
        Assert.True(OverheadMapMarker.IconCarriesTheHeading(northUp: true));
        Assert.Equal(0, LocalMapScreen.MapRendersWithYaw(0x2000, northUp: true));

        Assert.False(OverheadMapMarker.IconCarriesTheHeading(northUp: false));
        Assert.Equal(0x2000, LocalMapScreen.MapRendersWithYaw(0x2000, northUp: false));
    }

    [Fact]
    public void TheMarkerIsCENTREDOnTheViewportNotCornered() {
        // The original's -4 / -3 is half of an 8x6 sprite, so this is centring rather than two magic
        // numbers — a port with a different icon size centres that instead.
        (int width, int height) = OverheadMapMarker.ImpliedIconSize;
        (int x, int y) = OverheadMapMarker.TopLeftFor(
            viewportX: 13, viewportY: 11, viewportWidth: 294, viewportHeight: 101,
            iconWidth: width, iconHeight: height);

        Assert.Equal(13 + 147 - 4, x);
        Assert.Equal(11 + 50 - 3, y);
    }

    [Fact]
    public void ABiggerIconIsStillCentred() {
        (int x, int y) = OverheadMapMarker.TopLeftFor(0, 0, 100, 100, iconWidth: 20, iconHeight: 10);
        Assert.Equal(40, x);
        Assert.Equal(45, y);
    }

    [Fact]
    public void SixteenDirectionsAtTheAnglesTheEngineCounts() {
        Assert.Equal(16, OverheadMapMarker.Directions);
        Assert.Equal(0x1000, OverheadMapMarker.StepSize);
        // Every direction is reachable, and each step lands on the next one.
        for (var i = 0; i < OverheadMapMarker.Directions; i++) {
            Assert.Equal(i, OverheadMapMarker.DirectionFor(i * OverheadMapMarker.StepSize));
        }
    }
}
