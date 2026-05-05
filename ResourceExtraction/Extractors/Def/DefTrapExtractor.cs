namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using System.IO;

public class DefTrapExtractor : DefFamilyExtractorBase<DefTrapEntry> {
    protected override int PayloadSize => 409;

    private static LandingPosition ReadLanding(BinaryReader reader) {
        return new LandingPosition {
            FineX     = reader.ReadInt32(),
            FineY     = reader.ReadInt32(),
            RotationZ = reader.ReadUInt16(),
        };
    }

    protected override DefTrapEntry ReadPayload(BinaryReader reader) {
        return new DefTrapEntry {
            Gap0             = reader.ReadUInt16(),
            EncounterNumber  = reader.ReadUInt32(),
            DialogId1        = reader.ReadUInt32(),
            DialogId2        = reader.ReadUInt32(),
            GapE             = reader.ReadUInt32(),
            LandingDir1      = ReadLanding(reader),
            LandingDir2      = ReadLanding(reader),
            LandingDir4      = ReadLanding(reader),
            LandingDir8      = ReadLanding(reader),
            LandingPrimary   = ReadLanding(reader),
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
