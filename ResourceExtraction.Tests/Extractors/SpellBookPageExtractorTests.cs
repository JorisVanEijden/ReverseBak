namespace ResourceExtraction.Tests.Extractors;

using ResourceExtraction.Extractors;
using System.Text;
using Xunit;

/// <summary>
/// INVSPELL.DAT: six groups of <c>{ u16 icon; u16 count; count * { char name[0x18]; u16 id } }</c>,
/// per <c>charscreen_draw_spell_book_actor</c>.
/// </summary>
public class SpellBookPageExtractorTests {
    static SpellBookPageExtractorTests() =>
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static void WriteName(BinaryWriter w, string name) {
        var field = new byte[0x18];
        byte[] raw = Encoding.ASCII.GetBytes(name);
        System.Array.Copy(raw, field, System.Math.Min(raw.Length, field.Length));
        w.Write(field);
    }

    private static MemoryStream Page(params (int Icon, (string Name, ushort Id)[] Spells)[] groups) {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        foreach ((int icon, (string, ushort)[] spells) in groups) {
            writer.Write((ushort)icon);
            writer.Write((ushort)spells.Length);
            foreach ((string name, ushort id) in spells) {
                WriteName(writer, name);
                writer.Write(id);
            }
        }
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private static (int, (string, ushort)[]) Empty(int icon) => (icon, new (string, ushort)[0]);

    [Fact]
    public void ReadsSixGroupsWithTheirIconsAndSpells() {
        using MemoryStream stream = Page(
            (37, new[] { ("Bane of Black Slayers", (ushort)9), ("Evil Seek", (ushort)44) }),
            Empty(36), Empty(38), Empty(39), Empty(55), Empty(56));

        var page = new SpellBookPageExtractor().Extract("invspell.dat", stream);

        Assert.Equal(6, page.Groups.Count);
        Assert.Equal(37, page.Groups[0].Icon);
        Assert.Equal(56, page.Groups[5].Icon);
        Assert.Equal(2, page.Groups[0].Spells.Count);
        Assert.Equal("Bane of Black Slayers", page.Groups[0].Spells[0].Name);
        Assert.Equal(9, page.Groups[0].Spells[0].SpellId);
        Assert.Equal("Evil Seek", page.Groups[0].Spells[1].Name);
        Assert.Equal(44, page.Groups[0].Spells[1].SpellId);
    }

    [Fact]
    public void ANameStopsAtItsNulRatherThanCarryingThePadding() {
        using MemoryStream stream = Page(
            (1, new[] { ("Evil Seek", (ushort)44) }), Empty(0), Empty(0), Empty(0), Empty(0), Empty(0));

        var page = new SpellBookPageExtractor().Extract("invspell.dat", stream);

        Assert.Equal("Evil Seek", page.Groups[0].Spells[0].Name);
    }

    [Fact]
    public void EachSpellCarriesItsContentKey() {
        using MemoryStream stream = Page(
            (1, new[] { ("Firestorm", (ushort)12) }), Empty(0), Empty(0), Empty(0), Empty(0), Empty(0));

        var page = new SpellBookPageExtractor().Extract("invspell.dat", stream);

        Assert.Equal("base:spell:12", page.Groups[0].Spells[0].SpellKey);
    }

    [Fact]
    public void AnEmptyGroupIsStillAGroup() {
        using MemoryStream stream = Page(
            Empty(5), Empty(6), Empty(7), Empty(8), Empty(9), Empty(10));

        var page = new SpellBookPageExtractor().Extract("invspell.dat", stream);

        Assert.Equal(6, page.Groups.Count);
        Assert.All(page.Groups, g => Assert.Empty(g.Spells));
    }
}
