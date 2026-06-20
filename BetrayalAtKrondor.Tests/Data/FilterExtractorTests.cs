namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Config;

using ResourceExtraction.Extractors;

using System.IO;

using Xunit;

/// <summary>
/// Verifies FILTER.DAT parsing: N blocks of 43 little-endian i32 per-entity-type
/// draw-distance thresholds (N derived from file length), indexed by detail level.
/// See <see cref="FilterData"/> / docs/FileFormats/FILTER.DAT.md.
/// </summary>
public class FilterExtractorTests {

    private static byte[] BuildFilterDat(params int[][] blocks) {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        foreach (int[] block in blocks) {
            Assert.Equal(FilterData.EntityTypeCount, block.Length);
            foreach (int v in block) {
                w.Write(v);
            }
        }
        return ms.ToArray();
    }

    private static int[] Block(int fill) {
        var b = new int[FilterData.EntityTypeCount];
        for (int i = 0; i < b.Length; i++) {
            b[i] = fill + i;
        }
        return b;
    }

    private static FilterData Extract(params int[][] blocks) =>
        new FilterExtractor().Extract("FILTER.DAT", new MemoryStream(BuildFilterDat(blocks)));

    [Fact]
    public void Extract_ReadsOneBlockPerDetailLevel() {
        FilterData result = Extract(Block(1000), Block(2000), Block(3000), Block(4000));

        Assert.Equal(4, result.DetailLevels.Count);
        for (int level = 0; level < 4; level++) {
            Assert.Equal(level, result.DetailLevels[level].Level);
            Assert.Equal(FilterData.EntityTypeCount, result.DetailLevels[level].DrawDistances.Length);
        }
    }

    [Fact]
    public void Extract_PreservesPerEntityTypeValuesInOrder() {
        FilterData result = Extract(Block(1000), Block(2000), Block(3000), Block(4000));

        Assert.Equal(1000, result.DetailLevels[0].DrawDistances[0]);
        Assert.Equal(1042, result.DetailLevels[0].DrawDistances[42]);
        Assert.Equal(3005, result.DetailLevels[2].DrawDistances[5]);
    }

    [Fact]
    public void Extract_KeepsSentinelValuesSigned() {
        // -1 ("never drawn") must survive as a signed i32, not 0xFFFFFFFF.
        var never = new int[FilterData.EntityTypeCount];
        never[10] = -1;
        never[11] = 1; // "always drawn"
        FilterData result = Extract(never);

        Assert.Single(result.DetailLevels);
        Assert.Equal(-1, result.DetailLevels[0].DrawDistances[10]);
        Assert.Equal(1, result.DetailLevels[0].DrawDistances[11]);
    }

    [Fact]
    public void Extract_DerivesBlockCountFromLength() {
        FilterData result = Extract(Block(500), Block(600));
        Assert.Equal(2, result.DetailLevels.Count);
    }
}
