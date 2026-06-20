namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Spells;

using ResourceExtraction.Extractors;

using System.IO;
using System.Text;

using Xunit;

/// <summary>
/// Verifies SPELLDOC.DAT parsing: u16 count, count × u32 blob offsets, u16 declared size,
/// then a NUL-terminated string blob (to EOF). Shared offsets resolve to the same string.
/// See <see cref="SpellDescriptions"/> / docs/FileFormats/SPELLDOC.DAT.md.
/// </summary>
public class SpellDocExtractorTests {

    static SpellDocExtractorTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static byte[] Build(string[] strings, uint[] offsetTable) {
        // Build a blob from the distinct strings and record each string's offset.
        var blob = new MemoryStream();
        var offsetOf = new System.Collections.Generic.Dictionary<string, uint>();
        foreach (string sv in strings) {
            if (offsetOf.ContainsKey(sv)) {
                continue;
            }
            offsetOf[sv] = (uint)blob.Position;
            byte[] bytes = Encoding.ASCII.GetBytes(sv);
            blob.Write(bytes, 0, bytes.Length);
            blob.WriteByte(0);
        }
        byte[] blobBytes = blob.ToArray();

        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write((ushort)offsetTable.Length);
        foreach (uint off in offsetTable) {
            w.Write(off);
        }
        w.Write((ushort)0); // declared size — ignored by the extractor (blob runs to EOF)
        w.Write(blobBytes);
        return ms.ToArray();
    }

    [Fact]
    public void Extract_ResolvesOffsetsIncludingSharedEmptyString() {
        // strings present in the blob: "Dragon's Breath", "Cost: 5", "" (shared separator)
        string[] distinct = { "Dragon's Breath", "Cost: 5", "" };
        // offsets: name@0, cost@16, empty@24 (computed below to keep the test robust)
        uint nameOff = 0;
        uint costOff = (uint)("Dragon's Breath".Length + 1);
        uint emptyOff = costOff + (uint)("Cost: 5".Length + 1);

        byte[] file = Build(distinct, new[] { nameOff, costOff, emptyOff, emptyOff });
        SpellDescriptions doc = new SpellDocExtractor().Extract("SPELLDOC.DAT", new MemoryStream(file));

        Assert.Equal(4, doc.Descriptions.Count);
        Assert.Equal("Dragon's Breath", doc.Descriptions[0]);
        Assert.Equal("Cost: 5", doc.Descriptions[1]);
        Assert.Equal("", doc.Descriptions[2]);
        Assert.Equal("", doc.Descriptions[3]); // shared empty-string offset
    }
}
