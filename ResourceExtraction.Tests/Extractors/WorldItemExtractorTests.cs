namespace ResourceExtraction.Tests.Extractors;

using GameData.Resources.World;
using ResourceExtraction.Extractors;
using Xunit;

public class WorldItemExtractorTests
{
    [Fact]
    public void Extract_ReadsSingleItem()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)42);    // TypeId
        writer.Write((ushort)100);   // Rotation.X
        writer.Write((ushort)200);   // Rotation.Y
        writer.Write((ushort)300);   // Rotation.Z
        writer.Write((uint)1000);    // Position.X
        writer.Write((uint)2000);    // Position.Y
        writer.Write((uint)3000);    // Position.Z
        writer.Flush();
        stream.Position = 0;

        var extractor = new WorldItemExtractor();
        var result = extractor.Extract("T010203.WLD", stream);

        Assert.Single(result.Items);
        Assert.Equal((ushort)42, result.Items[0].TypeId);
        Assert.Equal((ushort)100, result.Items[0].Rotation.X);
        Assert.Equal(1000u, result.Items[0].Position.X);
        Assert.Equal(1, result.ZoneNumber);
        Assert.Equal(2, result.X);
        Assert.Equal(3, result.Y);
    }

    [Fact]
    public void Extract_CapsAt300Items()
    {
        using var stream = new MemoryStream(new byte[301 * 20]);
        var extractor = new WorldItemExtractor();
        var result = extractor.Extract("T010101.WLD", stream);
        Assert.Equal(300, result.Items.Count);
    }

    [Fact]
    public void Extract_ParsesFilenameCoordinates()
    {
        using var stream = new MemoryStream(new byte[20]);
        var extractor = new WorldItemExtractor();
        var result = extractor.Extract("T120515.WLD", stream);
        Assert.Equal(12, result.ZoneNumber);
        Assert.Equal(5, result.X);
        Assert.Equal(15, result.Y);
    }
}
