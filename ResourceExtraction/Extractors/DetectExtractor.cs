namespace ResourceExtraction.Extractors;

using GameData.Resources.Config;
using System.IO;

/// <summary>
/// Parses DETECT.DAT — the world-item interaction-detection range table (sibling of
/// FILTER.DAT). The file is a flat run of <see cref="DetectData.LocationCount"/> blocks
/// (block 0 = aboveground, block 1 = underground), each
/// <see cref="DetectData.EntityTypeCount"/> little-endian i32 ranges indexed by entity-type
/// tag. The block count is derived from the stream length. See <see cref="DetectData"/> for
/// the semantics and the IDA references (Load_detect.dat @ ovr190:0x76510).
/// </summary>
public class DetectExtractor : ExtractorBase<DetectData> {
    private const int BlockBytes = DetectData.EntityTypeCount * sizeof(int); // 172

    private static readonly string[] LocationNames = { "Aboveground", "Underground" };

    public override DetectData Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var data = new DetectData(id);

        int blockCount = (int)(resourceStream.Length / BlockBytes);
        for (int b = 0; b < blockCount; b++) {
            var block = new DetectLocationRanges {
                Location = b < LocationNames.Length ? LocationNames[b] : $"Block{b}",
            };
            for (int t = 0; t < DetectData.EntityTypeCount; t++) {
                block.DetectRanges[t] = reader.ReadInt32();
            }
            data.Locations.Add(block);
        }

        return data;
    }
}
