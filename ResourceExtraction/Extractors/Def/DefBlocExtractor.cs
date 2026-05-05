namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using System.IO;

public class DefBlocExtractor : DefFamilyExtractorBase<DefBlocEntry> {
    protected override int PayloadSize => 8;

    protected override DefBlocEntry ReadPayload(BinaryReader reader) {
        return new DefBlocEntry {
            Gap0     = reader.ReadUInt16(),
            DialogId = reader.ReadUInt32(),
            Gap6     = reader.ReadByte(),
            Field7   = reader.ReadByte(),
        };
    }
}
