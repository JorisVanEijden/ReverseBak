namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using System.IO;

public class DefDisaExtractor : DefFamilyExtractorBase<DefDisaEntry> {
    protected override int PayloadSize => 7;

    protected override DefDisaEntry ReadPayload(BinaryReader reader) {
        return new DefDisaEntry {
            Field0    = reader.ReadByte(),
            Field1    = reader.ReadByte(),
            Chance    = reader.ReadByte(),
            GlobalKey = reader.ReadUInt16(),
            Field5    = reader.ReadByte(),
            Field6    = reader.ReadByte(),
        };
    }
}
