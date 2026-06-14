namespace ResourceExtraction.Tests.Extractors;

using System.IO;
using System.Linq;
using GameData.Resources.Spells;
using ResourceExtraction.Extractors;
using Xunit;

public class CastRingExtractorTests {
    /// <summary>Walk up from the test output dir to find OriginalGame/&lt;name&gt; (present on dev
    /// machines, absent on CI). Returns null when the shipped data isn't available.</summary>
    private static string? FindGameFile(string name) {
        string? dir = System.AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string candidate = Path.Combine(dir, "OriginalGame", name);
            if (File.Exists(candidate)) {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    // RING.DAT on disk: u16 x[30] then u16 y[30] (two parallel little-endian arrays).
    private static byte[] BuildRing(ushort[] xs, ushort[] ys) {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        foreach (ushort x in xs) {
            w.Write(x);
        }
        foreach (ushort y in ys) {
            w.Write(y);
        }
        w.Flush();
        return ms.ToArray();
    }

    [Fact]
    public void Extract_ScalesVgaToCanonical() {
        var xs = new ushort[30];
        var ys = new ushort[30];
        for (int i = 0; i < 30; i++) {
            xs[i] = (ushort)i;
            ys[i] = (ushort)(i * 2);
        }

        CastRing ring = new CastRingExtractor().Extract("RING.DAT", new MemoryStream(BuildRing(xs, ys)));

        Assert.Equal(30, ring.Positions.Count);
        // 320x200 VGA -> canonical 1600x1200: x*5, y*6.
        Assert.Equal(0, ring.Positions[0].X);
        Assert.Equal(0, ring.Positions[0].Y);
        Assert.Equal(4 * 5, ring.Positions[4].X);
        Assert.Equal(8 * 6, ring.Positions[4].Y);
        Assert.Equal(29 * 5, ring.Positions[29].X);
        Assert.Equal(58 * 6, ring.Positions[29].Y);
    }

    [Fact]
    public void Extract_AnchorsAreExactlyEveryFifthPosition() {
        CastRing ring = new CastRingExtractor()
            .Extract("RING.DAT", new MemoryStream(BuildRing(new ushort[30], new ushort[30])));

        int[] anchors = Enumerable.Range(0, 30).Where(i => ring.Positions[i].IsCategoryAnchor).ToArray();
        Assert.Equal(new[] { 4, 9, 14, 19, 24, 29 }, anchors);
    }

    [SkippableFact]
    public void Extract_RealRingDat_Has30AnchoredPositions_InCanonicalBounds() {
        string? path = FindGameFile("RING.DAT");
        Skip.If(path == null, "OriginalGame/RING.DAT not found");
        using FileStream s = File.OpenRead(path!);

        CastRing ring = new CastRingExtractor().Extract("RING.DAT", s);

        Assert.Equal(30, ring.Positions.Count);
        Assert.Equal(6, ring.Positions.Count(p => p.IsCategoryAnchor));
        Assert.All(ring.Positions, p => {
            Assert.InRange(p.X, 0, 1600);
            Assert.InRange(p.Y, 0, 1200);
        });
    }
}
