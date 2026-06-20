namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using System.IO;

public class DefTownExtractor : DefFamilyExtractorBase<DefTownEntry> {
    protected override int PayloadSize => 21;

    protected override DefTownEntry ReadPayload(BinaryReader reader) {
        return new DefTownEntry {
            Gap0               = reader.ReadUInt16(),
            GdsSceneNumber     = reader.ReadUInt16(),
            Gap4               = reader.ReadUInt16(),
            DialogId           = reader.ReadUInt32(),
            GapA               = reader.ReadUInt32(),
            ApproachTileOffset = reader.ReadUInt16(),
            ApproachHeading    = reader.ReadUInt16(),
            DoApproachWalk     = reader.ReadByte(),
            Field13            = reader.ReadByte(),
            Field14            = reader.ReadByte(),
        };
    }
}
