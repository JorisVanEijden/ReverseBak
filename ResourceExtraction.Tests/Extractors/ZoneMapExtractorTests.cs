namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.World;
using ResourceExtraction.Extractors;
using Xunit;

public class ZoneMapExtractorTests
{
    [Fact]
    public void Extract_Reads400Bytes()
    {
        var data = new byte[400];
        data[0] = 0b00000101;
        using var stream = new MemoryStream(data);
        var extractor = new ZoneMapExtractor();
        var result = extractor.Extract("Z01MAP.DAT", stream);
        Assert.Equal(400, result.BitmapData.Length);
        Assert.True(result.IsTileInZone(0, 0));
        Assert.False(result.IsTileInZone(1, 0));
        Assert.True(result.IsTileInZone(2, 0));
    }

    [Fact]
    public void IsTileInZone_CorrectBitAccess()
    {
        var map = new ZoneMap("test");
        map.BitmapData[5 * 8 + 2] = 0x80;
        Assert.True(map.IsTileInZone(23, 5));
        Assert.False(map.IsTileInZone(22, 5));
    }
}
