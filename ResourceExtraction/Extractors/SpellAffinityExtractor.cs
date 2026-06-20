namespace ResourceExtraction.Extractors;

using GameData.Resources.Spells;
using System.IO;

/// <summary>
/// Parses the per-spell creature-type affinity bitmask format shared by SPELLWEA.DAT
/// (weaknesses) and SPELLRES.DAT (resistances): <c>u16 spellCount</c> then
/// <c>spellCount × (3 × u16)</c>, a 48-bit creature-type mask per spell. The masks are
/// decoded into explicit creature-type index lists. See <see cref="SpellAffinityTable"/>
/// for the semantics and the IDA references (Load_spell_weakness_and_resistance @
/// ovr177:0x6b4dc).
/// </summary>
public class SpellAffinityExtractor : ExtractorBase<SpellAffinityTable> {
    private const int MaskWords = 3; // 3 × u16 = 48 creature-type bits

    public override SpellAffinityTable Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var table = new SpellAffinityTable(id);

        int spellCount = reader.ReadUInt16();
        for (int spell = 0; spell < spellCount; spell++) {
            var affinity = new SpellAffinity { SpellNumber = spell };
            for (int word = 0; word < MaskWords; word++) {
                ushort mask = reader.ReadUInt16();
                for (int bit = 0; bit < 16; bit++) {
                    if ((mask & (1 << bit)) != 0) {
                        affinity.CreatureTypes.Add(word * 16 + bit);
                    }
                }
            }
            table.Spells.Add(affinity);
        }

        return table;
    }
}
