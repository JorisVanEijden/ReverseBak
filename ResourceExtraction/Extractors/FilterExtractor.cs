namespace ResourceExtraction.Extractors;

using GameData.Resources.Config;
using System.IO;

/// <summary>
/// Parses FILTER.DAT — the world-item draw-distance / culling table. The file is a flat
/// run of <see cref="FilterData.DetailLevelCount"/> blocks, each
/// <see cref="FilterData.EntityTypeCount"/> little-endian i32 draw-distance thresholds
/// indexed by entity-type tag. The block count is derived from the stream length so the
/// whole file is extracted, even though the running game loads only the block matching the
/// current detail-level preference. See <see cref="FilterData"/> for the semantics and the
/// IDA references (Load_filter_dat @ ovr136:0x43f60).
/// </summary>
public class FilterExtractor : ExtractorBase<FilterData> {
    private const int BlockBytes = FilterData.EntityTypeCount * sizeof(int); // 172

    public override FilterData Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var data = new FilterData(id);

        int blockCount = (int)(resourceStream.Length / BlockBytes);
        for (int level = 0; level < blockCount; level++) {
            var block = new DetailLevelFilter { Level = level };
            for (int t = 0; t < FilterData.EntityTypeCount; t++) {
                block.DrawDistances[t] = reader.ReadInt32();
            }
            data.DetailLevels.Add(block);
        }

        return data;
    }
}
