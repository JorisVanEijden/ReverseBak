namespace ResourceExtraction.Extractors;

using GameData;
using GameData.Resources.Content;
using GameData.Resources.Spells;

using ResourceExtraction.Extensions;

using System.Collections.Generic;
using System.IO;
using System.Text;

public class SpellExtractor : ExtractorBase<SpellList> {
    public override SpellList Extract(string id, Stream resourceStream) {
        // Read spell data
        var spellList = new SpellList(id);
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        ushort numberOfEntries = resourceReader.ReadUInt16();
        spellList.Spells = new Dictionary<int, Spell>(numberOfEntries);
        var spellNameOffsets = new short[numberOfEntries];
        for (var i = 0; i < numberOfEntries; i++) {
            spellNameOffsets[i] = resourceReader.ReadInt16();
            var spell = new Spell($"{i}") {
                Key = ContentKey.ForBase("spell", i),
                MinimumCost = resourceReader.ReadInt16(),
                MaximumCost = resourceReader.ReadInt16(),
                IsMartial = resourceReader.ReadInt16() == 1,
                TargetingType = resourceReader.ReadInt16(),
                EffectSubject = resourceReader.ReadInt16(),
                AnimationEffectType = resourceReader.ReadInt16(),
                ObjectId = resourceReader.ReadInt16(),
                Calculation = (SpellCalculation)resourceReader.ReadInt16(),
                Damage = resourceReader.ReadInt16(),
                Duration = resourceReader.ReadInt16()
            };
            // #8: de-index ObjectId → objinfo key (ObjectId -1 = no associated inventory object).
            spell.ObjectKey = spell.ObjectId >= 0 ? ContentKey.ForBase("objinfo", spell.ObjectId) : null;
            spellList.Spells[i] = spell;
        }
        // Read spell name block
        ushort stringBufferSize = resourceReader.ReadUInt16();
        char[] stringBuffer = resourceReader.ReadChars(stringBufferSize);

        // Add the names to the spells
        for (var i = 0; i < numberOfEntries; i++) {
            spellList.Spells[i].Name = stringBuffer.GetZeroTerminatedString(spellNameOffsets[i]);
        }

        return spellList;
    }
}