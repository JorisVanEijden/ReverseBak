namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.World;
using ResourceExtraction.Extractors;
using Xunit;

public class ZoneMapExtractorTests
{
    [Fact]
    public void Extract_UnpacksBitsToTiles()
    {
        var data = new byte[400];
        data[0] = 0b00000101;
        using var stream = new MemoryStream(data);
        var extractor = new ZoneMapExtractor();
        var result = extractor.Extract("Z01MAP.DAT", stream);
        Assert.Equal(ZoneMap.Width, result.Tiles.Length);
        Assert.Equal(ZoneMap.Height, result.Tiles[0].Length);
        Assert.True(result.IsTileInZone(0, 0));
        Assert.False(result.IsTileInZone(1, 0));
        Assert.True(result.IsTileInZone(2, 0));
    }

    [Fact]
    public void IsTileInZone_CorrectBitAccess()
    {
        var data = new byte[400];
        data[5 * 8 + 2] = 0x80;
        using var stream = new MemoryStream(data);
        var extractor = new ZoneMapExtractor();
        var map = extractor.Extract("test", stream);
        Assert.True(map.IsTileInZone(23, 5));
        Assert.False(map.IsTileInZone(22, 5));
    }
}
