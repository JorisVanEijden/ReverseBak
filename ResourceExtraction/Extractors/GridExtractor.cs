namespace ResourceExtraction.Extractors;

using GameData.Resources.Combat;
using System.IO;

/// <summary>
/// Parses GRID.DAT — the per-zone combat-grid border colour table (one u16 pen index per
/// zone, indexed by <c>zoneNumber - 1</c>). The entry count is derived from the stream
/// length. See <see cref="GridData"/> for the semantics and the IDA references
/// (Load_grid @ seg033:0x2e7ee).
/// </summary>
public class GridExtractor : ExtractorBase<GridData> {
    public override GridData Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var data = new GridData(id);

        int count = (int)(resourceStream.Length / sizeof(ushort));
        for (int zone = 0; zone < count; zone++) {
            data.ZoneBorderPens.Add(reader.ReadUInt16());
        }

        return data;
    }
}
