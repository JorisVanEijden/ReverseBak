namespace ResourceExtraction.Tests.Extractors;

using ResourceExtraction.Extractors;
using Xunit;

public class ZoneDefExtractorTests
{
    [Fact]
    public void Extract_Reads50BytesCorrectly()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)2);     // ZoneLocation
        writer.Write((ushort)0x10);  // ZonePointer
        writer.Write((uint)0xAABBCCDD); // Field04
        writer.Write((ushort)0x05);  // Field08
        writer.Write((ushort)0x01);  // Flags
        writer.Write((byte)0x0A);    // Unknown0C
        writer.Write((byte)0x0B);    // Unknown0D
        writer.Write((uint)100);     // Field0E
        writer.Write((uint)5000);    // CameraZPosition
        writer.Write((uint)200);     // Field16
        writer.Write((uint)300);     // Field1A
        writer.Write((ushort)3);     // RmpResourceCount
        writer.Write((ushort)64);    // Field20
        writer.Write((uint)400);     // Field22
        writer.Write((uint)500);     // Field26
        writer.Write((ushort)32);    // Field2A
        writer.Write((uint)600);     // Field2C
        writer.Write((uint)700);     // Field30
        writer.Flush();
        stream.Position = 0;

        var extractor = new ZoneDefExtractor();
        var result = extractor.Extract("Z01DEF.DAT", stream);

        Assert.Equal((ushort)2, result.ZoneLocation);
        Assert.Equal(0xAABBCCDDu, result.Field04);
        Assert.Equal((ushort)0x01, result.Flags);
        Assert.Equal((byte)0x0A, result.Unknown0C);
        Assert.Equal(5000u, result.CameraZPosition);
        Assert.Equal((ushort)3, result.RmpResourceCount);
        Assert.Equal(700u, result.Field30);
    }
}
