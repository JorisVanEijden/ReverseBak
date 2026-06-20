namespace BetrayalAtKrondor.Tests.Data;

using GameData.Resources.Spells;

using ResourceExtraction.Extractors;

using System.Collections.Generic;
using System.IO;
using System.Text;

using Xunit;

/// <summary>
/// Verifies SPELLDOC.DAT parsing: u16 entryCount, entryCount × u32 blob offsets, u16
/// declared size, then a NUL-terminated string blob (to EOF). Entries are grouped 7-per-spell
/// into named fields; shared offsets resolve to the same string. See
/// <see cref="SpellDescriptions"/> / docs/FileFormats/SPELLDOC.DAT.md.
/// </summary>
public class SpellDocExtractorTests {

    static SpellDocExtractorTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    // Builds a SPELLDOC.DAT from per-entry strings (one offset per entry; identical strings
    // share an offset, mirroring the shipped file's shared empty separator).
    private static byte[] Build(params string[] entries) {
        var blob = new MemoryStream();
        var offsetOf = new Dictionary<string, uint>();
        var offsets = new uint[entries.Length];
        for (int i = 0; i < entries.Length; i++) {
            if (!offsetOf.TryGetValue(entries[i], out uint off)) {
                off = (uint)blob.Position;
                offsetOf[entries[i]] = off;
                byte[] bytes = Encoding.ASCII.GetBytes(entries[i]);
                blob.Write(bytes, 0, bytes.Length);
                blob.WriteByte(0);
            }
            offsets[i] = off;
        }

        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write((ushort)entries.Length);
        foreach (uint off in offsets) {
            w.Write(off);
        }
        w.Write((ushort)0); // declared size — ignored by the extractor (blob runs to EOF)
        w.Write(blob.ToArray());
        return ms.ToArray();
    }

    [Fact]
    public void Extract_GroupsSevenFieldsPerSpell() {
        SpellDescriptions doc = new SpellDocExtractor().Extract("SPELLDOC.DAT", new MemoryStream(Build(
            // spell 0 — note the shared blank entries
            "Flamecast", "Cost: 1-20 Health/Stamina", "Damage: 3 x Cost", "", "Line of sight: Yes", "Area fire damage", "",
            // spell 1 — two-line effect
            "Strength Drain", "Cost: 2", "Damage: None", "", "", "Drains strength from victim and", "gives to caster.")));

        Assert.Equal(2, doc.Spells.Count);

        SpellDescription s0 = doc.Spells[0];
        Assert.Equal(0, s0.SpellNumber);
        Assert.Equal("Flamecast", s0.Name);
        Assert.Equal("Cost: 1-20 Health/Stamina", s0.Cost);
        Assert.Equal("Damage: 3 x Cost", s0.Damage);
        Assert.Equal("", s0.Duration);
        Assert.Equal("Line of sight: Yes", s0.LineOfSight);
        Assert.Equal("Area fire damage", s0.Effect);
        Assert.Equal("", s0.EffectLine2);

        SpellDescription s1 = doc.Spells[1];
        Assert.Equal("Strength Drain", s1.Name);
        Assert.Equal("Drains strength from victim and", s1.Effect);
        Assert.Equal("gives to caster.", s1.EffectLine2);
    }
}
