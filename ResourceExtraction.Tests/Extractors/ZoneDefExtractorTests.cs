namespace ResourceExtraction.Tests.Extractors;

using ResourceExtraction.Extractors;
using Xunit;

public class ZoneDefExtractorTests
{
    [Fact]
    public void Extract_Reads52BytesCorrectly()
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((short)2);      // ZoneLocation
        writer.Write((short)0x10);   // ZonePointer
        writer.Write((uint)0xAABBCCDD); // DefaultCameraZ
        writer.Write((ushort)0x05);  // DefaultCameraPitch
        writer.Write((ushort)0x01);  // Flags
        writer.Write((byte)0x0A);    // SkyColor
        writer.Write((byte)0x0B);    // GroundColor
        writer.Write((uint)100);     // MapMinZ
        writer.Write((uint)5000);    // CameraZPosition
        writer.Write((uint)200);     // MapMaxZ
        writer.Write((uint)300);     // MapZoomStep
        writer.Write((short)3);      // RmpResourceCount
        writer.Write((short)64);     // SpriteFogDivisor
        writer.Write((uint)400);     // SpriteFogNearDistance
        writer.Write((uint)500);     // Unused26
        writer.Write((short)32);     // PolygonFogDivisor
        writer.Write((uint)600);     // PolygonFogNearDistance
        writer.Write((uint)700);     // FarClipDistance
        writer.Flush();
        stream.Position = 0;

        var extractor = new ZoneDefExtractor();
        var result = extractor.Extract("Z01DEF.DAT", stream);

        Assert.Equal((short)2, result.ZoneLocation);
        Assert.Equal(0xAABBCCDDu, result.DefaultCameraZ);
        Assert.Equal((ushort)0x05, result.DefaultCameraPitch);
        Assert.Equal((ushort)0x01, result.Flags);
        Assert.Equal((byte)0x0A, result.SkyColor);
        Assert.Equal((byte)0x0B, result.GroundColor);
        Assert.Equal(100u, result.MapMinZ);
        Assert.Equal(5000u, result.CameraZPosition);
        Assert.Equal(200u, result.MapMaxZ);
        Assert.Equal(300u, result.MapZoomStep);
        Assert.Equal((short)3, result.RmpResourceCount);
        Assert.Equal((short)64, result.SpriteFogDivisor);
        Assert.Equal(400u, result.SpriteFogNearDistance);
        Assert.Equal(500u, result.Unused26);
        Assert.Equal((short)32, result.PolygonFogDivisor);
        Assert.Equal(600u, result.PolygonFogNearDistance);
        Assert.Equal(700u, result.FarClipDistance);
    }
}
