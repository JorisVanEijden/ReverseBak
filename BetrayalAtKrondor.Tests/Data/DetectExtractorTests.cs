namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Config;

using ResourceExtraction.Extractors;

using System.IO;

using Xunit;

/// <summary>
/// Verifies DETECT.DAT parsing: N blocks of 43 little-endian i32 per-entity-type
/// interaction-detection ranges (block 0 = aboveground, block 1 = underground), N derived
/// from file length. See <see cref="DetectData"/> / docs/FileFormats/DETECT.DAT.md.
/// </summary>
public class DetectExtractorTests {

    private static byte[] BuildDetectDat(params int[][] blocks) {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        foreach (int[] block in blocks) {
            Assert.Equal(DetectData.EntityTypeCount, block.Length);
            foreach (int v in block) {
                w.Write(v);
            }
        }
        return ms.ToArray();
    }

    private static int[] Block(int fill) {
        var b = new int[DetectData.EntityTypeCount];
        for (int i = 0; i < b.Length; i++) {
            b[i] = fill + i;
        }
        return b;
    }

    private static DetectData Extract(params int[][] blocks) =>
        new DetectExtractor().Extract("DETECT.DAT", new MemoryStream(BuildDetectDat(blocks)));

    [Fact]
    public void Extract_ReadsAbovegroundAndUndergroundBlocks() {
        DetectData result = Extract(Block(7000), Block(2500));

        Assert.Equal(2, result.Locations.Count);
        Assert.Equal("Aboveground", result.Locations[0].Location);
        Assert.Equal("Underground", result.Locations[1].Location);
        Assert.Equal(DetectData.EntityTypeCount, result.Locations[0].DetectRanges.Length);
    }

    [Fact]
    public void Extract_PreservesPerEntityTypeValuesInOrder() {
        DetectData result = Extract(Block(7000), Block(2500));

        Assert.Equal(7000, result.Locations[0].DetectRanges[0]);
        Assert.Equal(7042, result.Locations[0].DetectRanges[42]);
        Assert.Equal(2505, result.Locations[1].DetectRanges[5]);
    }

    [Fact]
    public void Extract_KeepsZeroAsNotInteractable() {
        var aboveground = new int[DetectData.EntityTypeCount];
        aboveground[16] = 16000; // detectable
        // index 0 left at 0 = never interactable
        DetectData result = Extract(aboveground);

        Assert.Single(result.Locations);
        Assert.Equal(0, result.Locations[0].DetectRanges[0]);
        Assert.Equal(16000, result.Locations[0].DetectRanges[16]);
    }
}
