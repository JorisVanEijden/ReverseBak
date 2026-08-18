namespace BetrayalAtKrondor.Tests.Location;

using GameData.Resources.Config;
using Xunit;

/// <summary>The rift map's gate and its "you are here" projection.</summary>
public class RiftMapTests {
    /// <summary>Only the note whose variable is 32 is the rift map; the rest are ordinary notes.</summary>
    [Theory]
    [InlineData(32, 9, true)]
    [InlineData(31, 9, false)]
    [InlineData(32, 8, false)]   // right note, wrong zone — no marker
    public void TheMarkerNeedsBothTheRiftNoteAndZoneNine(int variable, int zone, bool expected) =>
        Assert.Equal(expected, RiftMap.ShowsMarker(variable, zone));

    /// <summary>
    /// The projection's origin: a party exactly at the world origin the branch subtracts sits at
    /// the map's own origin, VGA (144, 192), less the half-box that centres the marker.
    /// </summary>
    [Fact]
    public void AtTheWorldOriginTheMarkerSitsAtTheMapOrigin() {
        (int x, int y) = RiftMap.MarkerTopLeft(640000, 640000);

        Assert.Equal((144 - 6) * 5, x);
        Assert.Equal((192 - 5) * 6, y);
    }

    /// <summary>
    /// <b>Y is inverted.</b> Walking north (increasing world Y) must move the marker UP the map,
    /// because the map's origin is at its bottom edge. Getting this backwards puts the party in
    /// the wrong half of the rift and looks plausible until you walk.
    /// </summary>
    [Fact]
    public void WalkingNorthMovesTheMarkerUp() {
        (int _, int near) = RiftMap.MarkerTopLeft(640000, 640000);
        (int _, int far) = RiftMap.MarkerTopLeft(640000, 640000 + 2346 * 10);

        Assert.True(far < near, "increasing world Y must decrease the marker's Y");
    }

    [Fact]
    public void WalkingEastMovesTheMarkerRight() {
        (int near, int _) = RiftMap.MarkerTopLeft(640000, 640000);
        (int far, int _) = RiftMap.MarkerTopLeft(640000 + 2295 * 10, 640000);

        Assert.True(far > near);
    }

    /// <summary>
    /// One map pixel per divisor, and the scaling happens AFTER the integer divide — so ten
    /// pixels east is exactly ten canonical steps of 5, not a value rounded in canonical space.
    /// </summary>
    [Fact]
    public void TheDivideHappensInVgaSpaceThenScales() {
        (int origin, int _) = RiftMap.MarkerTopLeft(640000, 640000);
        (int tenEast, int _) = RiftMap.MarkerTopLeft(640000 + 2295 * 10, 640000);

        Assert.Equal(10 * 5, tenEast - origin);
    }

    [Fact]
    public void TheMarkerBoxIsTwelveByTenVgaInCanonicalUnits() {
        Assert.Equal(12 * 5, RiftMap.MarkerWidth);
        Assert.Equal(10 * 6, RiftMap.MarkerHeight);
    }
}
