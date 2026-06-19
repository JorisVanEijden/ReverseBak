namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Object;

using ResourceExtraction.Extractors;

using System.IO;
using System.Text;

using Xunit;

/// <summary>
/// Verifies ONAMES.DAT parsing: u16 count; (count+1) u16 offsets relative to the
/// string base (= 2 + 2*(count+1)); NUL-terminated strings. The final offset is an
/// end sentinel. See <see cref="ObjectNames"/> / docs/FileFormats/ONAMES.DAT.md.
/// </summary>
public class OnamesExtractorTests {

    static OnamesExtractorTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static byte[] BuildOnamesDat(params string[] names) {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms, Encoding.ASCII);

        // string blob (NUL-terminated), and offsets relative to the blob base
        var blob = new MemoryStream();
        var offsets = new ushort[names.Length + 1]; // +1 end sentinel
        for (int i = 0; i < names.Length; i++) {
            offsets[i] = (ushort)blob.Position;
            byte[] bytes = Encoding.ASCII.GetBytes(names[i]);
            blob.Write(bytes, 0, bytes.Length);
            blob.WriteByte(0);
        }
        offsets[names.Length] = (ushort)blob.Position; // sentinel = total blob size

        w.Write((ushort)names.Length);
        foreach (ushort off in offsets) {
            w.Write(off);
        }
        byte[] blobBytes = blob.ToArray();
        w.Write(blobBytes, 0, blobBytes.Length);
        return ms.ToArray();
    }

    private static ObjectNames Extract(params string[] names) =>
        new OnamesExtractor().Extract("ONAMES.DAT", new MemoryStream(BuildOnamesDat(names)));

    [Fact]
    public void Extract_ReturnsAllNamesInOrder() {
        ObjectNames result = Extract("Staff of Macros", "CRYSTAL Staff", "ROYAL KEY");
        Assert.Equal(3, result.Names.Count);
        Assert.Equal("Staff of Macros", result.Names[0]); // offset 0 (string base)
        Assert.Equal("CRYSTAL Staff", result.Names[1]);   // resolves via non-zero relative offset
        Assert.Equal("ROYAL KEY", result.Names[2]);
    }

    [Fact]
    public void Extract_HonoursHeaderCount() {
        ObjectNames result = Extract("A", "B");
        Assert.Equal(2, result.Names.Count);
    }
}
