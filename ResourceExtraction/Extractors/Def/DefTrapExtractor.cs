namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Content;
using GameData.Resources.Data;
using System.IO;

public class DefTrapExtractor : DefFamilyExtractorBase<DefTrapEntry> {
    protected override int PayloadSize => 409;

    protected override DefTrapEntry ReadPayload(BinaryReader reader) {
        var entry = new DefTrapEntry {
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
            Avoidable        = (reader.ReadUInt16() & 1) != 0,   // bit 0: Stealth/Scouting roll active
        };
        entry.EncounterKey = ContentKey.ForBase("traps", (int)entry.EncounterNumber); // #14
        return entry;
    }
}
