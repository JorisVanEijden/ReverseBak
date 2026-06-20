namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Menu;

using ResourceExtraction.Extractors;

using System.IO;
using System.Text;

using Xunit;

/// <summary>
/// Verifies IN_*.DAT ("input form") parsing: u16 string-pool length + pool, u16 field count,
/// then 21-byte field records; caption resolved from the pool offset (-1 = none); box and
/// caption coordinates scaled 320×200 → canonical 1600×1200 (×5 / ×6). See
/// <see cref="InputForm"/> / docs/FileFormats/IN_DAT.md.
/// </summary>
public class InExtractorTests {

    static InExtractorTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static byte[] BuildField(
        ushort itemCount, ushort x, ushort y, ushort width,
        byte[] style, ushort labelOffset, ushort labelX, ushort labelY, ushort allocFlag) {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(itemCount); w.Write(x); w.Write(y); w.Write(width);
        w.Write(style); // 5 bytes
        w.Write(labelOffset); w.Write(labelX); w.Write(labelY); w.Write(allocFlag);
        return ms.ToArray();
    }

    private static InputForm Extract(byte[] pool, params byte[][] fields) {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write((ushort)pool.Length);
        w.Write(pool);
        w.Write((ushort)fields.Length);
        foreach (byte[] f in fields) {
            w.Write(f);
        }
        ms.Position = 0;
        return new InExtractor().Extract("IN_TEST.DAT", ms);
    }

    [Fact]
    public void Extract_ResolvesLabelsAndScalesCoordinates() {
        byte[] pool = Encoding.ASCII.GetBytes("Zone\0TileX\0");
        InputForm form = Extract(
            pool,
            BuildField(3, 36, 137, 25, new byte[] { 14, 1, 1, 0, 10 }, 0, 12, 139, 0),
            BuildField(3, 97, 137, 25, new byte[] { 14, 1, 1, 0, 10 }, 5, 75, 139, 0));

        Assert.Equal(2, form.Fields.Count);

        InputField zone = form.Fields[0];
        Assert.Equal("Zone", zone.Label);
        Assert.Equal(3, zone.ItemCount);
        Assert.Equal(36 * 5, zone.X);
        Assert.Equal(137 * 6, zone.Y);
        Assert.Equal(25 * 5, zone.Width);
        Assert.Equal(12 * 5, zone.LabelX);
        Assert.Equal(139 * 6, zone.LabelY);
        Assert.Equal(new[] { 14, 1, 1, 0, 10 }, zone.StyleBytes);

        Assert.Equal("TileX", form.Fields[1].Label);
    }

    [Fact]
    public void Extract_TreatsMinusOneOffsetAsNoLabel() {
        InputForm form = Extract(
            System.Array.Empty<byte>(),
            BuildField(1, 0, 0, 0, new byte[5], 0xFFFF, 0, 0, 0));

        Assert.Equal("", form.Fields[0].Label);
    }
}
