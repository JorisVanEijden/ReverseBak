namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Spells;

using ResourceExtraction.Extractors;

using System.IO;

using Xunit;

/// <summary>
/// Verifies the SPELLWEA.DAT / SPELLRES.DAT format: u16 spell count, then 3×u16 per spell =
/// a 48-bit creature-type mask, decoded into creature-type index lists. See
/// <see cref="SpellAffinityTable"/> / docs/FileFormats/SPELLWEA_SPELLRES.DAT.md.
/// </summary>
public class SpellAffinityExtractorTests {

    private static byte[] Build(params ushort[][] spellMasks) {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write((ushort)spellMasks.Length);
        foreach (ushort[] words in spellMasks) {
            Assert.Equal(3, words.Length);
            foreach (ushort word in words) {
                w.Write(word);
            }
        }
        return ms.ToArray();
    }

    private static SpellAffinityTable Extract(params ushort[][] spellMasks) =>
        new SpellAffinityExtractor().Extract("SPELLWEA.DAT", new MemoryStream(Build(spellMasks)));

    [Fact]
    public void Extract_DecodesCreatureTypeBitsAcrossThreeWords() {
        // word0 bit0 -> type 0, word1 bit0 -> type 16, word2 bit12 -> type 44
        SpellAffinityTable table = Extract(
            new ushort[] { 0x0001, 0x0001, 0x1000 },
            new ushort[] { 0x0000, 0x0000, 0x0000 });

        Assert.Equal(2, table.Spells.Count);
        Assert.Equal(0, table.Spells[0].SpellNumber);
        Assert.Equal(new[] { 0, 16, 44 }, table.Spells[0].CreatureTypes);
        Assert.Empty(table.Spells[1].CreatureTypes);
    }

    [Fact]
    public void Extract_HandlesHighBitInLastWord() {
        // word2 bit15 -> creature type 47 (the max)
        SpellAffinityTable table = Extract(new ushort[] { 0x0000, 0x0000, 0x8000 });

        Assert.Equal(new[] { 47 }, table.Spells[0].CreatureTypes);
    }
}
