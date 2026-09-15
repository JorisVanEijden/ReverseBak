namespace ResourceExtraction.Extractors;

using GameData.Resources.Content;
using GameData.Resources.Spells;
using System.IO;

/// <summary>
/// Parses the creature × spell affinity bitmask format shared by SPELLWEA.DAT (weaknesses) and
/// SPELLRES.DAT (resistances): <c>u16 rowCount</c> (64) then <c>rowCount × (3 × u16)</c>.
/// <b>A row is a creature type and a bit is a spell</b> — every lookup passes the creature type
/// first (<c>Cast_Evil_Seek</c> pushes the spell, then <c>combatData.creatureType</c>, before
/// calling <c>check_spell_resistance</c>, which multiplies its FIRST argument by 3). The masks are
/// regrouped here into per-spell creature-type lists. See <see cref="SpellAffinityTable"/>.
/// </summary>
public class SpellAffinityExtractor : ExtractorBase<SpellAffinityTable> {
    private const int MaskWords = 3; // 3 × u16 = 48 spell bits per creature row

    public override SpellAffinityTable Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var table = new SpellAffinityTable(id);
        for (int spell = 0; spell < SpellAffinityTable.SpellCount; spell++) {
            table.Spells.Add(new SpellAffinity { SpellNumber = spell });
        }

        int creatureCount = reader.ReadUInt16();
        for (int creatureType = 0; creatureType < creatureCount; creatureType++) {
            for (int word = 0; word < MaskWords; word++) {
                ushort mask = reader.ReadUInt16();
                for (int bit = 0; bit < 16; bit++) {
                    if ((mask & (1 << bit)) != 0) {
                        SpellAffinity affinity = table.Spells[word * 16 + bit];
                        affinity.CreatureTypes.Add(creatureType);
                        affinity.CreatureKeys.Add(ContentKey.ForBase("mnames", creatureType)); // #10
                    }
                }
            }
        }

        return table;
    }
}
