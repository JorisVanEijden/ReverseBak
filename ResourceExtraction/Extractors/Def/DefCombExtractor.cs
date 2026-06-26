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
            LandingDir1     = ReadLanding(reader),
            LandingDir2     = ReadLanding(reader),
            LandingDir4     = ReadLanding(reader),
            LandingDir8     = ReadLanding(reader),
            EnemySetup      = ReadEnemySetup(reader),
            Field18D        = reader.ReadUInt16(),
        };
    }
}
