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
            LandingDir1      = ReadLanding(reader),
            LandingDir2      = ReadLanding(reader),
            LandingDir4      = ReadLanding(reader),
            LandingDir8      = ReadLanding(reader),
            LandingPrimary   = ReadLanding(reader),
            EnemySetup       = ReadEnemySetup(reader),
            Field197         = reader.ReadUInt16(),
        };
    }
}
