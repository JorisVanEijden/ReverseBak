namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using System.IO;

public class DefTrapExtractor : DefFamilyExtractorBase<DefTrapEntry> {
    protected override int PayloadSize => 409;

    protected override DefTrapEntry ReadPayload(BinaryReader reader) {
        return new DefTrapEntry {
            Gap0             = reader.ReadUInt16(),
            EncounterNumber  = reader.ReadUInt32(),
            DialogId1        = reader.ReadUInt32(),
            DialogId2        = reader.ReadUInt32(),
            GapE             = reader.ReadUInt32(),
            Field12          = reader.ReadByte(),
            Gap13            = reader.ReadBytes(9),
            Field1C          = reader.ReadByte(),
            Gap1D            = reader.ReadBytes(9),
            Field26          = reader.ReadByte(),
            Gap27            = reader.ReadBytes(9),
            Field30          = reader.ReadByte(),
            Gap31            = reader.ReadBytes(9),
            Coordinates      = new Coordinates64k {
                X = reader.ReadInt32(),
                Y = reader.ReadInt32(),
            },
            Field42          = reader.ReadUInt16(),
            Struct339        = new DefTrapStruct339 {
                Gap0     = reader.ReadByte(),
                Field1   = reader.ReadUInt16(),
                Gap3     = reader.ReadBytes(335),
                Field152 = reader.ReadByte(),
            },
            Field197         = reader.ReadUInt16(),
        };
    }
}
