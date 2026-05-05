namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using System.IO;

public class DefDialExtractor : DefFamilyExtractorBase<DefDialEntry> {
    protected override int PayloadSize => 8;

    protected override DefDialEntry ReadPayload(BinaryReader reader) {
        return new DefDialEntry {
            Field0   = reader.ReadByte(),
            Field1   = reader.ReadByte(),
            DialogId = reader.ReadUInt32(),
            Field6   = reader.ReadByte(),
            Pad7     = reader.ReadByte(),
        };
    }
}
