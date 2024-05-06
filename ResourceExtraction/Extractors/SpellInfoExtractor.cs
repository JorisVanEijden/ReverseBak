namespace ResourceExtraction.Extractors;

using GameData.Resources.Spells;

using ResourceExtraction.Extensions;

using System.Collections.Generic;
using System.IO;
using System.Text;

public class SpellInfoExtractor : ExtractorBase<SpellInfoList> {
    public override SpellInfoList Extract(string id, Stream resourceStream) {
        var spellInfoList = new SpellInfoList(id);

        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        ushort numberOfEntries = resourceReader.ReadUInt16();
        var stringOffsets = new List<int>(numberOfEntries);
        for (int i = 0; i < numberOfEntries; i++) {
            stringOffsets.Add(resourceReader.ReadInt32());
        }

        // Read the string buffer
        ushort stringBufferSize = resourceReader.ReadUInt16();
        char[] stringBuffer = resourceReader.ReadChars(stringBufferSize);

        // Read 7 strings for each spell
        for (int i = 0, j = 0; i < numberOfEntries && j < stringOffsets.Count; i++) {
            spellInfoList.List.Add(new SpellInfo {
                    Title = stringBuffer.GetZeroTerminatedString(stringOffsets[j++]),
                    Cost = stringBuffer.GetZeroTerminatedString(stringOffsets[j++]),
                    Damage = stringBuffer.GetZeroTerminatedString(stringOffsets[j++]),
                    Duration = stringBuffer.GetZeroTerminatedString(stringOffsets[j++]),
                    LineOfSight = stringBuffer.GetZeroTerminatedString(stringOffsets[j++]),
                    Description = stringBuffer.GetZeroTerminatedString(stringOffsets[j++]) + " " + stringBuffer.GetZeroTerminatedString(stringOffsets[j++])
                }
            );
        }

        return spellInfoList;
    }
}