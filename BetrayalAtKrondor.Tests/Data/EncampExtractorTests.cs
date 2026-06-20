namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Config;

using ResourceExtraction.Extractors;

using System.IO;

using Xunit;

/// <summary>
/// Verifies ENCAMP.DAT parsing: four u16 icon-geometry words, then a u16 clock-entry count
/// + (x,y) pairs, then a u16 needle-entry count + (x,y) pairs. All coordinates are scaled
/// from 320×200 VGA into canonical 1600×1200 space (×5 / ×6). See <see cref="EncampData"/> /
/// docs/FileFormats/ENCAMP.DAT.md.
/// </summary>
public class EncampExtractorTests {

    private static byte[] Build(
        (ushort anchorX, ushort anchorY, ushort width, ushort height) geom,
        (ushort x, ushort y)[] clock,
        (ushort x, ushort y)[] needle) {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(geom.anchorX); w.Write(geom.anchorY); w.Write(geom.width); w.Write(geom.height);
        w.Write((ushort)clock.Length);
        foreach ((ushort x, ushort y) in clock) { w.Write(x); w.Write(y); }
        w.Write((ushort)needle.Length);
        foreach ((ushort x, ushort y) in needle) { w.Write(x); w.Write(y); }
        return ms.ToArray();
    }

    private static EncampData Extract(byte[] bytes) =>
        new EncampExtractor().Extract("ENCAMP.DAT", new MemoryStream(bytes));

    [Fact]
    public void Extract_ScalesGeometryAndCounts() {
        EncampData result = Extract(Build(
            (3, 3, 9, 9),
            new (ushort, ushort)[] { (71, 106), (57, 104) },
            new (ushort, ushort)[] { (12, 34) }));

        // header scaled ×5 (X/width) and ×6 (Y/height)
        Assert.Equal(15, result.IconAnchorX);
        Assert.Equal(18, result.IconAnchorY);
        Assert.Equal(45, result.IconWidth);
        Assert.Equal(54, result.IconHeight);

        Assert.Equal(2, result.ClockEntries.Count);
        Assert.Single(result.NeedleEntries);
    }

    [Fact]
    public void Extract_ScalesPointCoordinates() {
        EncampData result = Extract(Build(
            (3, 3, 9, 9),
            new (ushort, ushort)[] { (71, 106) },
            new (ushort, ushort)[] { (12, 34) }));

        Assert.Equal(71 * 5, result.ClockEntries[0].X);
        Assert.Equal(106 * 6, result.ClockEntries[0].Y);
        Assert.Equal(12 * 5, result.NeedleEntries[0].X);
        Assert.Equal(34 * 6, result.NeedleEntries[0].Y);
    }
}
