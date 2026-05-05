namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using GameData.Resources.Location;
using System.IO;

public class DefZoneExtractor : DefFamilyExtractorBase<DefZoneEntry> {
    protected override int PayloadSize => 19;

    protected override DefZoneEntry ReadPayload(BinaryReader reader) {
        return new DefZoneEntry {
            Gap0      = reader.ReadUInt16(),
            Location  = new Location {
                ZoneNumber = reader.ReadByte(),
                X          = reader.ReadByte(),
                Y          = reader.ReadByte(),
                XOffset    = reader.ReadByte(),
                YOffset    = reader.ReadByte(),
                ZRotation  = reader.ReadUInt16(),
            },
            DialogId1 = reader.ReadUInt32(),
            DialogId2 = reader.ReadUInt32(),
            Gap11     = reader.ReadByte(),
            Field12   = reader.ReadByte(),
        };
    }
}
