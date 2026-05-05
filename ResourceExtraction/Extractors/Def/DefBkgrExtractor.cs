namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using System.IO;

public class DefBkgrExtractor : DefFamilyExtractorBase<DefBkgrEntry> {
    protected override int PayloadSize => 21;

    protected override DefBkgrEntry ReadPayload(BinaryReader reader) {
        return new DefBkgrEntry {
            Gap0     = reader.ReadUInt16(),
            Field2   = reader.ReadUInt16(),
            Gap4     = reader.ReadUInt16(),
            DialogId = reader.ReadUInt32(),
            GapA     = reader.ReadUInt32(),
            FieldE   = reader.ReadUInt16(),
            Gap10    = reader.ReadUInt16(),
            Field12  = reader.ReadByte(),
            Field13  = reader.ReadByte(),
            Field14  = reader.ReadByte(),
        };
    }
}
