namespace ResourceExtractor.Extractors;

using ResourceExtractor.Resources.Spells;
using ResourceExtractor.Extensions;

using System.Text;

internal class SpellExtractor : ExtractorBase {
    public static List<Spell> ExtractSpells(string filePath) {
        List<Spell> spells = ExtractSpellsFile(filePath);
        AddSpellInfo(spells, filePath);

        return spells;
    }

    public static List<Spell> ExtractSpellsFile(string filePath) {
        using FileStream resourceFile = File.OpenRead(Path.Join(filePath, "SPELLS.DAT"));
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        // Read spell data
        ushort numberOfEntries = resourceReader.ReadUInt16();
        var spellList = new List<Spell>(numberOfEntries);
        short[] spellNameOffsets = new short[numberOfEntries];
        for (int i = 0; i < numberOfEntries; i++) {
            spellNameOffsets[i] = resourceReader.ReadInt16();
            var spell = new Spell {
                Id = i,
                MinimumCost = resourceReader.ReadInt16(),
                MaximumCost = resourceReader.ReadInt16(),
                Field6 = resourceReader.ReadInt16() == 1,
                Field8 = resourceReader.ReadInt16(),
                FieldA = resourceReader.ReadInt16(),
                FieldC = resourceReader.ReadInt16(),
                ObjectId = resourceReader.ReadInt16(),
                Field10 = resourceReader.ReadInt16(),
                Damage = resourceReader.ReadInt16(),
                Duration = resourceReader.ReadInt16()
            };
            spellList.Add(spell);
        }
        // Read spell name block
        ushort stringBufferSize = resourceReader.ReadUInt16();
        char[] stringBuffer = resourceReader.ReadChars(stringBufferSize);

        // Add the names to the spells
        for (int i = 0; i < numberOfEntries; i++) {
            spellList[i].Name = stringBuffer.GetZeroTerminatedString(spellNameOffsets[i]);
        }

        return spellList;
    }

    private static void AddSpellInfo(IReadOnlyList<Spell> spells, string filePath) {
        using FileStream resourceFile = File.OpenRead(Path.Join(filePath, "SPELLDOC.DAT"));
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        // Read a list of string buffer offsets  
        ushort numberOfEntries = resourceReader.ReadUInt16();
        var stringOffsets = new List<int>(numberOfEntries);
        for (int i = 0; i < numberOfEntries; i++) {
            stringOffsets.Add(resourceReader.ReadInt32());
        }

        // Read the string buffer
        ushort stringBufferSize = resourceReader.ReadUInt16();
        char[] stringBuffer = resourceReader.ReadChars(stringBufferSize);

        // Read 7 strings for each spell
        for (int i = 0, j = 0; i < spells.Count; i++) {
            var spellInfo = new SpellInfo {
                Title = stringBuffer.GetZeroTerminatedString(stringOffsets[j++]),
                Cost = stringBuffer.GetZeroTerminatedString(stringOffsets[j++]),
                Damage = stringBuffer.GetZeroTerminatedString(stringOffsets[j++]),
                Duration = stringBuffer.GetZeroTerminatedString(stringOffsets[j++]),
                LineOfSight = stringBuffer.GetZeroTerminatedString(stringOffsets[j++]),
                Description = stringBuffer.GetZeroTerminatedString(stringOffsets[j++]) + " " + stringBuffer.GetZeroTerminatedString(stringOffsets[j++])
            };
            spells[i].Info = spellInfo;
        }
    }
}