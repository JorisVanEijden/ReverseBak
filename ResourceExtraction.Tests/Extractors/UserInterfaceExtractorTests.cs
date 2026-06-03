namespace ResourceExtraction.Tests.Extractors;

using ResourceExtraction.Extractors;
using Xunit;

public class UserInterfaceExtractorTests {
    [Fact]
    public void Extract_ReturnsCanonicalSpaceCoordinates() {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true)) {
            writer.Write((ushort)0);    // UserInterfaceType
            writer.Write((ushort)0);    // IsModal
            writer.Write((ushort)169);  // ColorBase
            writer.Write((ushort)13);   // XPosition -> 65
            writer.Write((ushort)11);   // YPosition -> 66
            writer.Write((ushort)320);  // Width     -> 1600
            writer.Write((ushort)200);  // Height    -> 1200
            writer.Write((ushort)0);    // entry count placeholder
            writer.Write((ushort)0);    // entry pointer placeholder
            writer.Write((short)-1);    // titleOffset (none)
            writer.Write((short)10);    // XOffset -> 50
            writer.Write((short)20);    // YOffset -> 120
            writer.Write((uint)0);      // bitmap pointer placeholder
            writer.Write((ushort)1);    // numberOfElements
            // --- element 0 ---
            writer.Write((ushort)0);    // ElementType
            writer.Write((short)0);     // ActionId
            writer.Write(true);         // Visible (1 byte)
            writer.Write((ushort)0);    // ColorBase
            writer.Write((ushort)0);    // Disabled
            writer.Write((ushort)0);    // State
            writer.Write((ushort)2);    // XPosition -> 10
            writer.Write((ushort)3);    // YPosition -> 18
            writer.Write((ushort)4);    // Width     -> 20
            writer.Write((ushort)5);    // Height    -> 30
            writer.Write((short)-1);    // Field13Offset
            writer.Write((short)-1);    // LabelOffset
            writer.Write((short)-1);    // LabelAltOffset
            writer.Write((short)0);     // IconBase
            writer.Write((ushort)0);    // Cursor
            writer.Write((ushort)0);    // SoundFlags
            writer.Write((ushort)0);    // ClickSound
            writer.Write((ushort)0);    // labelBufferSize (no string bytes follow)
        }
        stream.Position = 0;

        var result = new UserInterfaceExtractor().Extract("REQ_TEST.DAT", stream);

        Assert.Equal(65, result.XPosition);
        Assert.Equal(66, result.YPosition);
        Assert.Equal(1600, result.Width);
        Assert.Equal(1200, result.Height);
        Assert.Equal(50, result.XOffset);
        Assert.Equal(120, result.YOffset);
        Assert.Equal(10, result.MenuEntries[0].XPosition);
        Assert.Equal(18, result.MenuEntries[0].YPosition);
        Assert.Equal(20, result.MenuEntries[0].Width);
        Assert.Equal(30, result.MenuEntries[0].Height);
    }
}
