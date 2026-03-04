namespace ResourceExtraction.Tests.Extractors;

using ResourceExtraction.Extractors;
using Xunit;

public class ZoneRefExtractorTests
{
    [Fact]
    public void Extract_ReadsTileCoordinates()
    {
        byte[] data = { 3, 10, 20, 30, 40, 50, 60 };
        using var stream = new MemoryStream(data);
        var extractor = new ZoneRefExtractor();
        var result = extractor.Extract("Z01REF.DAT", stream);
        Assert.Equal(3, result.Tiles.Count);
        Assert.Equal(10, result.Tiles[0].X);
        Assert.Equal(20, result.Tiles[0].Y);
        Assert.Equal(50, result.Tiles[2].X);
    }
}
