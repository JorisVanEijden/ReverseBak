namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using System.IO;

public class DefCombExtractor : DefFamilyExtractorBase<DefCombEntry> {
    protected override int PayloadSize => 399;

    protected override DefCombEntry ReadPayload(BinaryReader reader) {
        return new DefCombEntry {
            Field0          = reader.ReadUInt16(),
            EncounterNumber = reader.ReadUInt32(),
            DialogId1       = reader.ReadUInt32(),
            DialogId2       = reader.ReadUInt32(),
            GapE            = reader.ReadByte(),
            GlobalKey       = reader.ReadUInt16(),
            Gap11           = reader.ReadByte(),
            Field12         = reader.ReadUInt16(),
            Gap14           = reader.ReadBytes(8),
            Field1C         = reader.ReadUInt16(),
            Gap1E           = reader.ReadBytes(8),
            Field26         = reader.ReadUInt16(),
            Gap28           = reader.ReadBytes(8),
            Field30         = reader.ReadUInt16(),
            Gap32           = reader.ReadBytes(8),
            Field3A         = reader.ReadByte(),
            MonsterNumber   = reader.ReadUInt16(),
            Gap3D           = reader.ReadBytes(336),
            Field18D        = reader.ReadUInt16(),
        };
    }
}
