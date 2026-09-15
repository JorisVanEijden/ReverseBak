namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Spells;

using ResourceExtraction.Extractors;

using System.IO;

using Xunit;

/// <summary>
/// Verifies the SPELLWEA.DAT / SPELLRES.DAT format: u16 row count, then 3×u16 per CREATURE TYPE =
/// a 48-bit SPELL mask, regrouped into per-spell creature-type lists. See
/// <see cref="SpellAffinityTable"/> / docs/FileFormats/SPELLWEA_SPELLRES.DAT.md.
/// </summary>
public class SpellAffinityExtractorTests {

    private static byte[] Build(params ushort[][] creatureRows) {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write((ushort)creatureRows.Length);
        foreach (ushort[] words in creatureRows) {
            Assert.Equal(3, words.Length);
            foreach (ushort word in words) {
                w.Write(word);
            }
        }
        return ms.ToArray();
    }

    private static SpellAffinityTable Extract(params ushort[][] creatureRows) =>
        new SpellAffinityExtractor().Extract("SPELLRES.DAT", new MemoryStream(Build(creatureRows)));

    [Fact]
    public void Extract_ARowIsACreatureAndABitIsASpell() {
        // Row 1 sets word2 bit12 -> creature 1 is listed for spell 44 (Evil Seek), and nothing else.
        // The old decode read the row as the spell and would have listed creature 44 under spell 1.
        SpellAffinityTable table = Extract(
            new ushort[] { 0x0000, 0x0000, 0x0000 },
            new ushort[] { 0x0000, 0x0000, 0x1000 });

        Assert.Equal(SpellAffinityTable.SpellCount, table.Spells.Count);
        Assert.Equal(44, table.Spells[44].SpellNumber);
        Assert.Equal(new[] { 1 }, table.Spells[44].CreatureTypes);
        Assert.Empty(table.Spells[1].CreatureTypes);
    }

    [Fact]
    public void Extract_DecodesSpellBitsAcrossThreeWordsForEveryRow() {
        // Creature 2: word0 bit0 -> spell 0, word1 bit0 -> spell 16, word2 bit15 -> spell 47 (the max).
        // Creature 3 shares spell 16, so the per-spell list keeps both rows in order.
        SpellAffinityTable table = Extract(
            new ushort[] { 0x0000, 0x0000, 0x0000 },
            new ushort[] { 0x0000, 0x0000, 0x0000 },
            new ushort[] { 0x0001, 0x0001, 0x8000 },
            new ushort[] { 0x0000, 0x0001, 0x0000 });

        Assert.Equal(new[] { 2 }, table.Spells[0].CreatureTypes);
        Assert.Equal(new[] { 2, 3 }, table.Spells[16].CreatureTypes);
        Assert.Equal(new[] { 2 }, table.Spells[47].CreatureTypes);
    }
}
