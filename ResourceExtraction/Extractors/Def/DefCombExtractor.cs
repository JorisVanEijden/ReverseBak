namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using System.IO;

public class DefCombExtractor : DefFamilyExtractorBase<DefCombEntry> {
    protected override int PayloadSize => 399;

    private static LandingPosition ReadLanding(BinaryReader reader) {
        return new LandingPosition {
            FineX     = reader.ReadInt32(),
            FineY     = reader.ReadInt32(),
            RotationZ = reader.ReadUInt16(),
        };
    }

    protected override DefCombEntry ReadPayload(BinaryReader reader) {
        return new DefCombEntry {
            Field0          = reader.ReadUInt16(),
            EncounterNumber = reader.ReadUInt32(),
            DialogId1       = reader.ReadUInt32(),
            DialogId2       = reader.ReadUInt32(),
            GapE            = reader.ReadByte(),
            GlobalKey       = reader.ReadUInt16(),
            Gap11           = reader.ReadByte(),
            LandingDir1     = ReadLanding(reader),
            LandingDir2     = ReadLanding(reader),
            LandingDir4     = ReadLanding(reader),
            LandingDir8     = ReadLanding(reader),
            Field3A         = reader.ReadByte(),
            MonsterNumber   = reader.ReadUInt16(),
            Gap3D           = reader.ReadBytes(336),
            Field18D        = reader.ReadUInt16(),
        };
    }
}
