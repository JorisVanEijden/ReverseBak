namespace ResourceExtraction.Tests.Extractors;

using System.IO;
using System.Linq;
using GameData.Resources.Spells;
using ResourceExtraction.Extractors;
using Xunit;

public class SpellSymbolExtractorTests {
    /// <summary>Walk up from the test output dir to find OriginalGame/&lt;name&gt; (present on dev
    /// machines, absent on CI). Returns null when the shipped data isn't available.</summary>
    private static string? FindGameFile(string name) {
        string? dir = System.AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string candidate = Path.Combine(dir, "OriginalGame", name);
            if (File.Exists(candidate)) {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    // SYMBOL<n>.DAT on disk: u16 count, then count * { u16 spellId; u16 x; u16 y; u8 character }.
    private static byte[] BuildSymbols(params (ushort spellId, ushort x, ushort y, byte ch)[] nodes) {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((ushort)nodes.Length);
        foreach ((ushort spellId, ushort x, ushort y, byte ch) in nodes) {
            w.Write(spellId);
            w.Write(x);
            w.Write(y);
            w.Write(ch);
        }
        w.Flush();
        return ms.ToArray();
    }

    [Fact]
    public void Extract_ParsesNodes_ScalesCoords_AndAddsOneToGlyph() {
        byte[] data = BuildSymbols((36, 42, 45, 0), (7, 100, 80, 5));

        SpellSymbolLayout layout = new SpellSymbolExtractor().Extract("SYMBOL3.DAT", new MemoryStream(data));

        Assert.Equal(2, layout.Category); // SYMBOL3 -> zero-based 2
        Assert.Equal(2, layout.Nodes.Count);

        SpellSymbolNode n0 = layout.Nodes[0];
        Assert.Equal(36, n0.SpellId);
        Assert.Equal(42 * 5, n0.X);
        Assert.Equal(45 * 6, n0.Y);
        Assert.Equal(1, n0.FontGlyph); // on-disk 0, engine adds 1

        SpellSymbolNode n1 = layout.Nodes[1];
        Assert.Equal(7, n1.SpellId);
        Assert.Equal(100 * 5, n1.X);
        Assert.Equal(80 * 6, n1.Y);
        Assert.Equal(6, n1.FontGlyph);
    }

    [Theory]
    [InlineData("SYMBOL1.DAT", 0)]
    [InlineData("SYMBOL6.DAT", 5)]
    public void Extract_DerivesZeroBasedCategoryFromFilename(string id, int expectedCategory) {
        SpellSymbolLayout layout = new SpellSymbolExtractor().Extract(id, new MemoryStream(BuildSymbols()));

        Assert.Equal(expectedCategory, layout.Category);
        Assert.Empty(layout.Nodes);
    }

    [SkippableFact]
    public void Extract_RealSymbol1Dat_IsCategory0_WithSixNodes() {
        string? path = FindGameFile("SYMBOL1.DAT");
        Skip.If(path == null, "OriginalGame/SYMBOL1.DAT not found");
        using FileStream s = File.OpenRead(path!);

        SpellSymbolLayout layout = new SpellSymbolExtractor().Extract("SYMBOL1.DAT", s);

        Assert.Equal(0, layout.Category);
        Assert.Equal(6, layout.Nodes.Count);
        Assert.All(layout.Nodes, n => Assert.True(n.FontGlyph >= 1));
    }
}
