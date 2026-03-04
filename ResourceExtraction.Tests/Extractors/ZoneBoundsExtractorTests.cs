namespace ResourceExtraction.Tests.Extractors;

using ResourceExtraction.Extractors;
using Xunit;

public class ZoneBoundsExtractorTests {
    [Fact]
    public void Extract_ReadsAllFourFields() {
        byte[] data = { 0x0A, 0x00, 0x14, 0x00, 0x40, 0x01, 0xC8, 0x00 };
        using var stream = new MemoryStream(data);
        var extractor = new ZoneBoundsExtractor();
        var result = extractor.Extract("zone.dat", stream);
        Assert.Equal("zone.dat", result.Id);
        Assert.Equal((ushort)10, result.XOffset);
        Assert.Equal((ushort)20, result.YOffset);
        Assert.Equal((ushort)320, result.Width);
        Assert.Equal((ushort)200, result.Height);
    }
}
