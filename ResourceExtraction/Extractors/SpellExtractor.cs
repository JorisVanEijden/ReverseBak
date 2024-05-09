namespace ResourceExtraction.Extractors;

using GameData;
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
                MinimumCost = resourceReader.ReadInt16(),
                MaximumCost = resourceReader.ReadInt16(),
                Field6 = resourceReader.ReadInt16() == 1,
                Field8 = resourceReader.ReadInt16(),
                FieldA = resourceReader.ReadInt16(),
                FieldC = resourceReader.ReadInt16(),
                ObjectId = resourceReader.ReadInt16(),
                Calculation = (SpellCalculation)resourceReader.ReadInt16(),
                Damage = resourceReader.ReadInt16(),
                Duration = resourceReader.ReadInt16()
            };
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