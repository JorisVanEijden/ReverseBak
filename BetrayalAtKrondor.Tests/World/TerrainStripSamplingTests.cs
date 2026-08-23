namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;

using Xunit;

/// <summary>
/// Which face pens sample the zone's terrain strip — a property of the PEN, not of the entity.
/// </summary>
public class TerrainStripSamplingTests {
    [Theory]
    [InlineData(0x00)] [InlineData(0x05)] [InlineData(0x09)]
    public void TheGroundAndTrackPensAlwaysSample(byte pen) =>
        Assert.Equal(TerrainStripSampling.Always, PolygonFace.StripSamplingFor(pen));

    [Theory]
    // Two PARALLEL 8-entry shade ramps, not one run: 0xE0-0xE7 and 0xF7-0xFF.
    [InlineData(0xE0)] [InlineData(0xE7)] [InlineData(0xF7)] [InlineData(0xFF)]
    public void TheShadeRampsSampleOnlyAtDistance(byte pen) =>
        Assert.Equal(TerrainStripSampling.LevelOfDetail, PolygonFace.StripSamplingFor(pen));

    [Theory]
    // The boundaries, both sides. 0x0A is just past the strip pens; 0xE8-0xF6 is the GAP between
    // the two ramps, which a single 0xE0..0xFF range test would wrongly swallow.
    [InlineData(0x0A)] [InlineData(0x7F)] [InlineData(0xDF)]
    [InlineData(0xE8)] [InlineData(0xF0)] [InlineData(0xF6)]
    public void EverythingElseIsAFlatFill(byte pen) =>
        Assert.Equal(TerrainStripSampling.None, PolygonFace.StripSamplingFor(pen));

    [Fact]
    public void TheFaceReadsItsOwnPen() {
        Assert.Equal(TerrainStripSampling.Always,
            new PolygonFace { VgaColor = 0x01 }.StripSampling);
        Assert.Equal(TerrainStripSampling.None,
            new PolygonFace { VgaColor = 0xF0 }.StripSampling);
    }
}
