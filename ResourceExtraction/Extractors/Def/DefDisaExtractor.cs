namespace ResourceExtraction.Extractors.Def;

using GameData.Resources.Data;
using ResourceExtraction.Extractors.GameState;
using System.IO;

public class DefDisaExtractor : DefFamilyExtractorBase<DefDisaEntry> {
    protected override int PayloadSize => 7;

    protected override DefDisaEntry ReadPayload(BinaryReader reader) {
        var entry = new DefDisaEntry {
            Field0    = reader.ReadByte(),
            Field1    = reader.ReadByte(),
            Chance    = reader.ReadByte(),
            GlobalKey = reader.ReadUInt16(),
            Field5    = reader.ReadByte(),
            Field6    = reader.ReadByte(),
        };
        entry.OnFire = DefEffect.ForKey(entry.GlobalKey, set: false);
        return entry;
    }
}
